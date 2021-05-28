using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Comsec.SqlPrune.Logging;
using Comsec.SqlPrune.Models;
using Comsec.SqlPrune.Providers;
using Sugar.Extensions;

namespace Comsec.SqlPrune.Commands
{
    /// <summary>
    /// Command to recover (get a copy of) the a backup from a given set.
    /// </summary>
    public class RecoverCommand : BaseFileProviderCommand, ICommand<RecoverCommand.Input>
    {
        private readonly IFileProvider localFileSystemProvider;
        private readonly ILogger logger;

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="fileProviders"></param>
        /// <param name="localFileSystemProvider"></param>
        /// <param name="logger"></param>
        public RecoverCommand(IEnumerable<IFileProvider> fileProviders, IFileProvider localFileSystemProvider, ILogger logger) 
            : base(fileProviders)
        {
            this.localFileSystemProvider = localFileSystemProvider;
            this.logger = logger;
        }

        public class Input
        {
            public Input(string path, string ext, string dbName, DirectoryInfo dest, string date, bool yes)
            {
                Path = path;
                FileExtensions = ext;
                DatabaseName = dbName;
                DestinationPath = dest;
                
                if (string.IsNullOrEmpty(date))
                {
                    Date = null;
                }
                else
                {
                    Date = DateTime.Parse(date);
                }

                NoConfirm = yes;
                
            }

            public string Path { get; set; }

            public string FileExtensions { get; set; }

            public string DatabaseName { get; set; }
            
            public DirectoryInfo DestinationPath { get; set; }

            public DateTime? Date { get; set; }

            public bool NoConfirm { get; set; }
        }

        public async Task Execute(Input input)
        {
            var provider = await GetProvider(input.Path);

            logger.Write("Listing all ");
            logger.Write(ConsoleColor.Cyan, "{0} backups with extensions: {1}", input.DatabaseName, input.FileExtensions);
            logger.Write(" files in ");
            logger.Write(ConsoleColor.Yellow, input.Path);
            logger.WriteLine(" including subfolders...");
            logger.WriteLine();

            var extensionsSearchPatterns = input.FileExtensions
                                                .FromCsv()
                                                .Select(x => x.Replace("*", ""))
                                                .ToArray();

            var allPathsTask = provider.GetFiles(input.Path, input.DatabaseName + "_backup_*")
                                       .GetAwaiter();

            var allPaths = allPathsTask.OutputProgress(logger);

            var paths = allPaths.Where(x => extensionsSearchPatterns.Any(y => x.Key.EndsWith(y)));

            foreach (var keyValuePair in paths)
            {
                logger.WriteLine("{0}\t{1}", keyValuePair.Key, keyValuePair.Value);
            }

            IEnumerable<BakModel> files = ToBakModels(paths);

            logger.WriteLine("Found {0} file(s) out of which {1} have valid file names.", paths.Count(), files.Count());
            logger.WriteLine();

            var groups = files.GroupBy(x => x.DatabaseName);

            if (groups.Count() > 1)
            {
                throw new ApplicationException(
                    "More than one database in the backup set, please \"lengthen\" the -db-name parameter");
            }

            if (!await localFileSystemProvider.IsDirectory(input.DestinationPath.FullName))
            {
                throw new ApplicationException("Destination path is not a local directory");
            }

            if (input.Date.HasValue)
            {
                if (input.Date.Value.Hour == 0 && input.Date.Value.Minute == 0 &&
                    input.Date.Value.Second == 0)
                {
                    files = files.Where(x => x.Created.Date == input.Date.Value.Date);
                }
                else
                {
                    files = files.Where(x => x.Created == input.Date.Value);
                }
            }

            var mostRecentFile = files.OrderByDescending(x => x.Created)
                                      .FirstOrDefault();

            if (mostRecentFile == null)
            {
                throw new ApplicationException("Nothing to recover.");
            }

            var filename = provider.ExtractFilenameFromPath(mostRecentFile.Path);
            var destination = Path.Combine(input.DestinationPath.FullName, filename);

            // Check the file doesn't exist in destination folder
            var destinationFileSize = localFileSystemProvider.GetFileSize(destination).Result;
            if (destinationFileSize > -1)
            {
                if (mostRecentFile.Size == destinationFileSize)
                {
                    logger.WriteLine("{0} already exists.", destination);

                    return;
                }
                
                logger.WriteLine("{0} already exist but is not the same size and will be overwritten.", destination);
            }

            var copy = true;

            if (input.NoConfirm)
            {
                logger.WriteLine("Copying {0} to {1}", mostRecentFile.Path, input.DestinationPath.FullName);
            }
            else
            {
                var prompt = $"Copy {mostRecentFile.Path} to {input.DestinationPath.FullName}?";

                copy = new Confirm(logger).Prompt(prompt, "y");
            }

            if (copy)
            {
                var task = provider.CopyToLocalAsync(mostRecentFile.Path, destination)
                                   .GetAwaiter();

                task.OutputProgress(logger);

                logger.WriteLine("OK");
            }
        }
    }
}

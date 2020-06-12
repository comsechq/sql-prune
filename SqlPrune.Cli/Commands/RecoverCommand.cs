using System;
using System.Collections.Generic;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Comsec.SqlPrune.LightInject;
using Comsec.SqlPrune.Models;
using Comsec.SqlPrune.Providers;
using LightInject;
using Sugar.Extensions;

namespace Comsec.SqlPrune.Commands
{
    /// <summary>
    /// Command to recover (get a copy of) the a backup from a given set.
    /// </summary>
    public class RecoverCommand : BaseFileProviderCommand
    {
        private readonly IFileProvider localFileSystemProvider;

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="fileProviders"></param>
        /// <param name="localFileSystemProvider"></param>
        public RecoverCommand(IEnumerable<IFileProvider> fileProviders, IFileProvider localFileSystemProvider) 
            : base(fileProviders)
        {
            this.localFileSystemProvider = localFileSystemProvider;
        }

        /// <summary>
        /// Defines a sub command, its handler and adds it to the <see cref="parent"/> command.
        /// </summary>
        /// <param name="parent"></param>
        /// <param name="container"></param>
        public static void Configure(Command parent, ServiceContainer container)
        {
            var command = new Command("recover")
                          {
                              new Argument<string>("path",
                                  "The path to a local folder or S3 bucket name"),
                              new Option<string>(new[] {"--ext"},
                                  getDefaultValue: () => "*.bak,*.bak.7z,*.sql,*.sql.gz",
                                  description:
                                  "Overrides the default file extensions (coma separated values can be used)"),
                              new Option<string>("--dbName",
                                  description: "Name of the database to recover")
                              {Required = true},
                              new Option<DirectoryInfo>("--dest",
                                  description: "Folder where to recover the backup file to")
                              {Required = true},
                              new Option<string>("--date",
                                  getDefaultValue: () => "",
                                  description:
                                  "The date (and optionally the time) of the backup (e.g. \"2020-06-09 00:01:02\") to restrict to. If only the date is specified the most recent backup for that day will be recovered."),
                              new Option<bool>(new[] {"--yes", "-y"},
                                  getDefaultValue: () => false,
                                  description: "Recover without confirmation")
                              {Required = false}
                          }.AddAwsSdkCredentialsOptions();

            command.Handler =
                CommandHandler.Create<Input, string, string, string>(async (input, profile, profilesLocation, region) =>
                {
                    container.RegisterOptionalAwsCredentials(profile, profilesLocation)
                             .RegisterOptionalAwsRegion(region);

                    container.Register<RecoverCommand>(f =>
                        new RecoverCommand(f.GetInstance<IEnumerable<IFileProvider>>(),
                            f.GetInstance<IFileProvider>("local")));

                    var instance = container.GetInstance<RecoverCommand>();

                    await instance.Execute(input);
                });

            parent.Add(command);
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

            Console.Write("Listing all ");
            ColorConsole.Write(ConsoleColor.Cyan, "{0} backups with extensions: {1}", input.DatabaseName, input.FileExtensions);
            Console.Write(" files in ");
            ColorConsole.Write(ConsoleColor.Yellow, input.Path);
            Console.WriteLine(" including subfolders...");
            Console.WriteLine();

            var extensionsSearchPatterns = input.FileExtensions
                                                .FromCsv()
                                                .Select(x => x.Replace("*", ""))
                                                .ToArray();

            var allPathsTask = provider.GetFiles(input.Path, input.DatabaseName + "_backup_*")
                                       .GetAwaiter();

            var allPaths = allPathsTask.OutputProgress();

            var paths = allPaths.Where(x => extensionsSearchPatterns.Any(y => x.Key.EndsWith(y)));

            foreach (var keyValuePair in paths)
            {
                Console.WriteLine("{0}\t{1}", keyValuePair.Key, keyValuePair.Value);
            }

            IEnumerable<BakModel> files = ToBakModels(paths);

            Console.WriteLine("Found {0} file(s) out of which {1} have valid file names.", paths.Count(), files.Count());
            Console.WriteLine();

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
                    Console.WriteLine("{0} already exists.", destination);

                    return;
                }
                
                Console.WriteLine("{0} already exist but is not the same size and will be overwritten.", destination);
            }

            var copy = true;

            if (input.NoConfirm)
            {
                Console.WriteLine("Copying {0} to {1}", mostRecentFile.Path, input.DestinationPath.FullName);
            }
            else
            {
                var prompt = $"Copy {mostRecentFile.Path} to {input.DestinationPath.FullName}?";

                copy = Confirm.Prompt(prompt, "y");
            }

            if (copy)
            {
                var task = provider.CopyToLocalAsync(mostRecentFile.Path, destination)
                                   .GetAwaiter();

                task.OutputProgress();

                Console.WriteLine("OK");
            }
        }
    }
}

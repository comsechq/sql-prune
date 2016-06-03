using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using Comsec.SqlPrune.Interfaces.Services.Providers;
using Comsec.SqlPrune.Models;
using Comsec.SqlPrune.Services.Providers;
using Sugar.Command;
using Sugar.Extensions;

namespace Comsec.SqlPrune.Commands
{
    /// <summary>
    /// Command to recover the a backup from a given set.
    /// </summary>
    public class RecoverCommand : BaseFileProviderCommand<RecoverCommand.Options>
    {
        [Flag("recover")]
        public class Options
        {
            /// <summary>
            /// Gets or sets the path (e.g. "e:", "c:\backups" or "s3://bucket/folder").
            /// </summary>
            /// <value>
            /// The path.
            /// </value>
            [Parameter(0, Required = true)]
            public string Path { get; set; }

            /// <summary>
            /// Gets or sets the name of the database.
            /// </summary>
            /// <value>
            /// The name of the database.
            /// </value>
            [Parameter("db-name", Default = null)]
            public string DatabaseName { get; set; }

            /// <summary>
            /// Gets or sets the local destination path to the folder where to copy the file to (e.g. D:\folder).
            /// </summary>
            /// <value>
            /// The destination path.
            /// </value>
            [Parameter("dest", Default = null)]
            public string DestinationPath { get; set; }

            /// <summary>
            /// Gets or sets the date and time (e.g. "2014-02-28 00:01:02") to restrict to.
            /// </summary>
            /// <value>
            /// The date and time of the backup.
            /// </value>
            [Parameter("date-time", Default = "")]
            public DateTime? DateTime { get; set; }

            /// <summary>
            /// Gets or sets the date component only to restrict to (e.g. "2014-02-28").
            /// </summary>
            /// <value>
            /// The day to restore the backup from.
            /// </value>
            [Parameter("date", Default = "")]
            public DateTime? Date { get; set; }

            /// <summary>
            /// Gets or sets the file extensions (values can be comma separated).
            /// </summary>
            /// <value>
            /// The file extensions.
            /// </value>
            [Parameter("fileExtensions", Default = "*.bak")]
            public string FileExtensions { get; set; }

            /// <summary>
            /// Gets or sets a value indicating whether the user will have to [confirm] before any file is deleted.
            /// </summary>
            /// <value>
            ///   <c>true</c> if [no confirm]; otherwise, <c>false</c>.
            /// </value>
            [Flag("no-confirm")]
            public bool NoConfirm { get; set; }
        }

        #region Dependencies

        /// <summary>
        /// Gets or sets the local file system provider.
        /// </summary>
        /// <value>
        /// The local file system provider.
        /// </value>
        public IFileProvider LocalFileSystemProvider { get; set; }

        #endregion

        /// <summary>
        /// Initializes a new instance of the <see cref="RecoverCommand"/> class.
        /// </summary>
        public RecoverCommand()
        {
            LocalFileSystemProvider = new LocalFileSystemProvider();
        }

        /// <summary>
        /// Executes the specified options.
        /// </summary>
        /// <param name="options">The options.</param>
        /// <returns></returns>
        public override int Execute(Options options)
        {
            PruneConsole.OutputVersion();

            IFileProvider provider;

            if (!GetProvider(options.Path, out provider))
            {
                return (int)ExitCode.GeneralError;
            }

            if (string.IsNullOrEmpty(options.DatabaseName))
            {
                Console.WriteLine("Missing -db-bname parameter");

                return (int)ExitCode.GeneralError;
            }
            
            Console.Write("Lising all ");
            ColorConsole.Write(ConsoleColor.Cyan, "{0} backups with extensions: {1}", options.DatabaseName, options.FileExtensions);
            Console.Write(" files in ");
            ColorConsole.Write(ConsoleColor.Yellow, options.Path);
            Console.WriteLine(" including subfolders...");
            Console.WriteLine();

            var extensionsSearchPatterns = options.FileExtensions
                                                  .FromCsv()
                                                  .Select(x => x.Replace("*", ""))
                                                  .ToArray();

            var paths = provider.GetFiles(options.Path, options.DatabaseName + "_backup_*")
                                .Where(x => extensionsSearchPatterns.Any(y => x.Key.EndsWith(y)));

            foreach (var keyValuePair in paths)
            {
                Console.WriteLine("{0}\t{1}", keyValuePair.Key, keyValuePair.Value);
            }

            if (string.IsNullOrEmpty(options.DestinationPath))
            {
                Console.WriteLine("Missing -dest parameter");

                return (int) ExitCode.GeneralError;
            }

            IEnumerable<BakModel> files = ToBakModels(paths);

            Console.WriteLine("Found {0} file(s) out of which {1} have valid file names.", paths.Count(), files.Count());
            Console.WriteLine();

            var groups = files.GroupBy(x => x.DatabaseName);

            if (groups.Count() > 1)
            {
                Console.WriteLine("More than one database in the backup set, please extend the -db-name parameter");

                return (int)ExitCode.GeneralError;
            }

            if (!LocalFileSystemProvider.IsDirectory(options.DestinationPath))
            {
                Console.WriteLine("Destination path is not a local directory");
                
                return (int)ExitCode.GeneralError;
            }

            if (options.DateTime.HasValue)
            {
                files = files.Where(x => x.Created == options.DateTime.Value);
            }
            else if (options.Date.HasValue)
            {
                files = files.Where(x => x.Created.Date == options.Date.Value.Date);
            }

            var mostRecentFile = files.OrderByDescending(x => x.Created)
                                      .FirstOrDefault();

            if (mostRecentFile == null)
            {
                Console.WriteLine("Nothing to recover.");

                return (int)ExitCode.GeneralError;
            }

            var filename = provider.ExtractFilenameFromPath(mostRecentFile.Path);
            var destination = Path.Combine(options.DestinationPath, filename);

            // Check the file doesn't exist in destination folder
            var destinationFileSize = LocalFileSystemProvider.GetFileSize(destination);
            if (destinationFileSize > -1)
            {
                if (mostRecentFile.Size == destinationFileSize)
                {
                    Console.WriteLine("{0} already exists.", destination);
                    
                    return Success();
                }
                
                Console.WriteLine("{0} already exist but is not the same size and will be overwritten.", destination);
            }

            var copy = true;

            if (options.NoConfirm)
            {
                Console.WriteLine("Copying {0} to {1}", mostRecentFile.Path, options.DestinationPath);
            }
            else
            {
                var prompt = string.Format("Copy {0} to {1}?", mostRecentFile.Path, options.DestinationPath);

                copy = Confirm.Prompt(prompt, "y");
            }

            if (copy)
            {
                var task = provider.CopyToLocalAsync(mostRecentFile.Path, destination)
                                   .GetAwaiter();

                var i = 0;

                do
                {
                    i++;
                    Console.Write(".");
                    if (i%10 == 0)
                    {
                        Console.WriteLine();
                    }
                    if (i > 1)
                    {
                        Thread.Sleep(1000);
                    }
                } while (!task.IsCompleted);

                Console.WriteLine("OK");
            }
            
            return Success();
        }
    }
}

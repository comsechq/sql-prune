using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Comsec.SqlPrune.Interfaces.Services;
using Comsec.SqlPrune.Interfaces.Services.Providers;
using Comsec.SqlPrune.Models;
using Comsec.SqlPrune.Services;
using Comsec.SqlPrune.Services.Providers;
using Sugar.Command;

namespace Comsec.SqlPrune.Commands
{
    /// <summary>
    /// Prunes a given location from its .bak files.
    /// </summary>
    public class PruneCommand : BoundCommand<PruneCommand.Options>
    {
        public class Options
        {
            /// <summary>
            /// Gets or sets the path (e.g. "e:" or "c:\backups").
            /// </summary>
            /// <value>
            /// The path.
            /// </value>
            [Parameter(0, Required = true)]
            public string Path { get; set; }

            /// <summary>
            /// Gets or sets a value indicating whether to delete files or just run a simulation.
            /// </summary>
            /// <value>
            ///   <c>true</c> if delete; otherwise, <c>false</c>.
            /// </value>
            [Flag("delete")]
            public bool Delete { get; set; }

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
        /// Gets or sets the file service.
        /// </summary>
        /// <value>
        /// The file service.
        /// </value>
        public IFileProvider[] FileProviders { get; set; }

        /// <summary>
        /// Gets or sets the prune service.
        /// </summary>
        /// <value>
        /// The prune service.
        /// </value>
        public IPruneService PruneService { get; set; }

        #endregion

        /// <summary>
        /// Initializes a new instance of the <see cref="PruneCommand" /> class.
        /// </summary>
        public PruneCommand()
        {
            FileProviders = new IFileProvider[]
                            {
                                new S3Provider(),
                                new LocalFileSystemProvider()
                            };

            PruneService = new PruneService();
        }

        /// <summary>
        /// Executes the command and restores the given directory onto the SQL server
        /// </summary>
        /// <param name="options">The options.</param>
        public override int Execute(Options options)
        {
            PruneConsole.OutputVersion();

            var provider = FileProviders.FirstOrDefault(p => p.ShouldRun(options.Path));

            if (provider == null)
            {
                Console.WriteLine("Unrecognised path. You must provide a path to a local folder or to an Amazon S3 bucket.");
                Console.WriteLine(@"Example: x:\path\to\folder or s3://bucket-name/optionally/with/path/folder");

                return (int) ExitCode.GeneralError;
            }

            if (string.IsNullOrEmpty(options.Path) || options.Path.StartsWith("-") || !provider.IsDirectory(options.Path))
            {
                Console.WriteLine("Invalid path: You must provide a path to an existing local folder or drive.");

                return (int) ExitCode.GeneralError;
            }

            Console.WriteLine("Listing all .bak files in folder {0} including subfolders...", options.Path);
            Console.WriteLine();

            var paths = provider.GetFiles(options.Path, "*.bak");

            if (paths == null)
            {
                Console.WriteLine("No bak files found in folder or subfolders.");

                return (int) ExitCode.GeneralError;
            }

            var files = new List<BakModel>(paths.Count);

            foreach (var pathAndSize in paths)
            {
                BakModel model;

                if (!BakFilenameExtractor.ValidateFilenameAndExtract(pathAndSize.Key, out model)) continue;

                model.Size = pathAndSize.Value;

                files.Add(model);
            }

            Console.WriteLine("Found {0} file(s) out of which {1} have valid file names.", paths.Count, files.Count);
            Console.WriteLine();

            if (options.Delete)
            {
                if (options.NoConfirm)
                {
                    ColorConsole.Write(ConsoleColor.Red, "DEFCON 1: ");
                    Console.WriteLine("All prunable files will be deleted.");
                }
                else
                {
                    ColorConsole.Write(ConsoleColor.Yellow, "DEFCON 3: ");
                    Console.WriteLine("Files will be deleted, but you'll have to confirm each deletion.");
                }
            }
            else
            {
                ColorConsole.Write(ConsoleColor.Green, "DEFCON 5: ");
                Console.WriteLine("Simulation only, no files will be deleted.");
            }
            Console.WriteLine();

            var backupSets = files.GroupBy(x => x.DatabaseName);

            long totalBytes = 0;
            long totalKept = 0;
            long totalPruned = 0;

            foreach (var databaseBakupSet in backupSets)
            {
                PruneService.FlagPrunableBackupsInSet(databaseBakupSet);

                ColorConsole.Write(ConsoleColor.White, " {0}:", databaseBakupSet.Key);
                Console.WriteLine();
                
                ColorConsole.Write(ConsoleColor.DarkGray, " Created         Status\tBytes\t\tPath");
                Console.WriteLine();

                foreach (var model in databaseBakupSet)
                {
                    Console.Write(" {0} ", model.Created.ToString("yyyyMMddTHHmmss", CultureInfo.InvariantCulture));

                    if (!model.Prunable.HasValue)
                    {
                        Console.Write(model.Status);
                    }
                    else if (model.Prunable.Value)
                    {
                        ColorConsole.Write(ConsoleColor.DarkRed, model.Status);
                    }
                    else
                    {
                        ColorConsole.Write(ConsoleColor.DarkGreen, model.Status);
                    }
                    Console.Write("\t{0,15:N0}\t", model.Size);

                    Console.Write(model.Path);
                        
                    Console.WriteLine();

                    if (model.Prunable.HasValue && model.Prunable.Value && options.Delete)
                    {
                        var prompt = string.Format("Delete {0}?", model.Path);

                        var delete = options.NoConfirm || Confirm.Prompt(prompt, "y");

                        if (delete)
                        {
                            provider.Delete(model.Path);
                            ColorConsole.Write(ConsoleColor.Red, "Deleted ");
                        }
                        else
                        {
                            ColorConsole.Write(ConsoleColor.DarkGreen, "Skipped ");
                        }
                        Console.WriteLine(model.Path);
                    }

                    totalBytes += model.Size;

                    if (model.Prunable.HasValue)
                    {
                        if (model.Prunable.Value)
                        {
                            totalPruned += model.Size;
                        }
                        else
                        {
                            totalKept += model.Size;
                        }

                    }
                }

                Console.WriteLine();
            }

            // Size Summary
            if (totalBytes > 0)
            {
                Console.WriteLine();

                ColorConsole.Write(ConsoleColor.DarkGreen, "                 Kept");
                Console.WriteLine(": \t{0,15:N0}", totalKept);

                ColorConsole.Write(ConsoleColor.DarkRed, "               Pruned");
                Console.WriteLine(": \t{0,15:N0}", totalPruned);

                Console.WriteLine("                Total:\t{0,15:N0}", totalBytes);
            }

            return (int) ExitCode.Success;
        }
    }
}

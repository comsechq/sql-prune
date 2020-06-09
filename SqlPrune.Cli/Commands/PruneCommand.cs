using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Comsec.SqlPrune.Providers;
using Comsec.SqlPrune.Services;
using Sugar.Command;
using Sugar.Command.Binder;
using Sugar.Extensions;

namespace Comsec.SqlPrune.Commands
{
    /// <summary>
    /// Prunes a given location from its .bak files.
    /// </summary>
    public class PruneCommand : BaseFileProviderCommand<PruneCommand.Options>
    {
        [Flag("prune")]
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
            /// Gets or sets the file extensions (values can be comma separated).
            /// </summary>
            /// <value>
            /// The file extensions.
            /// </value>
            [Parameter("file-extensions", Default = "*.bak,*.bak.7z,*.sql,*.sql.gz")]
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

        private readonly IPruneService pruneService;

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="fileProviders"></param>
        /// <param name="pruneService"></param>
        public PruneCommand(IEnumerable<IFileProvider> fileProviders, IPruneService pruneService) 
            : base(fileProviders)
        {
            this.pruneService = pruneService;
        }

        /// <summary>
        /// Executes the command and restores the given directory onto the SQL server
        /// </summary>
        /// <param name="options">The options.</param>
        public override int Execute(Options options)
        {
            PruneConsole.OutputVersion();

            var provider = GetProvider(options.Path).Result;

            if (provider == null)
            {
                return (int) ExitCode.GeneralError;
            }
            
            Console.Write("Listing all ");
            ColorConsole.Write(ConsoleColor.Cyan, options.FileExtensions);
            Console.Write(" files in ");
            ColorConsole.Write(ConsoleColor.Yellow, options.Path);
            Console.WriteLine(" including subfolders...");
            Console.WriteLine();

            var endingWith = options.FileExtensions
                                    .FromCsv()
                                    .Select(x => x.Trim())
                                    .ToArray();

            var paths = provider.GetFiles(options.Path, endingWith)
                                .Result;

            if (paths == null)
            {
                Console.WriteLine("No bak files found in folder or subfolders.");

                return (int) ExitCode.GeneralError;
            }

            var files = ToBakModels(paths);

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

            foreach (var dbBackupSet in backupSets)
            {
                pruneService.FlagPrunableBackupsInSet(dbBackupSet);

                ColorConsole.Write(ConsoleColor.White, " {0}:", dbBackupSet.Key);
                Console.WriteLine();
                
                ColorConsole.Write(ConsoleColor.DarkGray, " Created         Status\tBytes\t\tPath");
                Console.WriteLine();

                foreach (var model in dbBackupSet)
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
                    
                    if (model.Prunable.HasValue && model.Prunable.Value && options.Delete)
                    {
                        var prompt = $"{Environment.NewLine}Delete {model.Path}?";

                        var delete = options.NoConfirm || Confirm.Prompt(prompt, "y");

                        if (delete)
                        {
                            provider.Delete(model.Path);
                            ColorConsole.Write(ConsoleColor.Red, " Deleted");
                        }
                        else
                        {
                            ColorConsole.Write(ConsoleColor.DarkGreen, " Skipped");
                        }

                        if (!options.NoConfirm)
                        {
                            Console.WriteLine(" {0}", model.Path);
                        }
                    }

                    Console.WriteLine();

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
                ColorConsole.Write(ConsoleColor.DarkGreen, "              Kept");
                Console.WriteLine(": {0,19:N0} ({1})", totalKept, totalKept.ToSizeWithSuffix());

                ColorConsole.Write(ConsoleColor.DarkRed, "            Pruned");
                Console.WriteLine(": {0,19:N0} ({1})", totalPruned, totalPruned.ToSizeWithSuffix());

                Console.WriteLine("             Total: {0,19:N0} ({1})", totalBytes, totalBytes.ToSizeWithSuffix());
            }

            return (int) ExitCode.Success;
        }
    }
}

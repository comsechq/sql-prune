using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using Comsec.SqlPrune.Interfaces;
using Comsec.SqlPrune.Interfaces.Services;
using Comsec.SqlPrune.Models;
using Comsec.SqlPrune.Services;
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
            [Parameter(0, Required = true)]
            public string Path { get; set; }

            /// <summary>
            /// Gets or sets a value indicating whether this command should use a verbose output or not.
            /// </summary>
            /// <value>
            ///   <c>true</c> if verbose; otherwise, <c>false</c>.
            /// </value>
            [Flag("verbose")]
            public bool Verbose { get; set; }

            /// <summary>
            /// Gets or sets the destination directory.
            /// </summary>
            /// <value>
            /// The db file path.
            /// </value>
            [Flag("delete")]
            public bool Delete { get; set; }
        }

        #region Dependencies

        /// <summary>
        /// Gets or sets the file service.
        /// </summary>
        /// <value>
        /// The file service.
        /// </value>
        public IFileService FileService { get; set; }

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
            FileService = new FileService();
            PruneService = new PruneService();
        }

        /// <summary>
        /// Executes the command and restores the given directory onto the SQL server
        /// </summary>
        /// <param name="options">The options.</param>
        public override int Execute(Options options)
        {
            PruneConsole.OutputVersion();

            if (string.IsNullOrEmpty(options.Path) || options.Path.StartsWith("-") || !FileService.IsDirectory(options.Path))
            {
                Console.WriteLine("Invalid path: You must provide a path to a folder.");

                return (int) ExitCode.GeneralError;
            }

            Console.WriteLine("Listing all .bak files in folder {0} including subfolders...", options.Path);
            Console.WriteLine();

            var paths = FileService.GetFiles(options.Path, "*.bak", SearchOption.AllDirectories);

            if (paths == null)
            {
                Console.WriteLine("No bak files found in folder or subfolders.");

                return (int) ExitCode.GeneralError;
            }

            var files = new List<BakModel>(paths.Length);

            foreach (var path in paths)
            {
                BakModel model;

                if (!BakFilenameExtractor.ValidateFilenameAndExtract(path, out model)) continue;

                files.Add(model);
            }

            Console.WriteLine("Found {0} file(s) out of which {1} have valid file names.", paths.Length, files.Count);
            Console.WriteLine();

            var backupSets = files.GroupBy(x => x.DatabaseName);

            foreach (var databaseBakupSet in backupSets)
            {
                PruneService.FlagPrunableBackupsInSet(databaseBakupSet, DateTime.Now);

                if (options.Verbose)
                {
                    ColorConsole.Write(ConsoleColor.White, " {0}:", databaseBakupSet.Key);
                    Console.WriteLine();
                    ColorConsole.Write(ConsoleColor.DarkGray, " Created\t\tStatus\t\tPath");
                    Console.WriteLine();
                    foreach (var model in databaseBakupSet)
                    {
                        Console.Write(" {0}\t", model.Created.ToString("yyyyMMddTHHmmss", CultureInfo.InvariantCulture));

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
                        Console.Write("\t\t");

                        Console.Write(model.Path);
                        
                        Console.WriteLine();
                    }
                    Console.WriteLine();
                }
            }

            return (int) ExitCode.Success;
        }
    }
}

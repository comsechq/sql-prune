using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using Comsec.SqlPrune.Interfaces;
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

        #endregion

        /// <summary>
        /// Initializes a new instance of the <see cref="PruneCommand" /> class.
        /// </summary>
        public PruneCommand()
        {
            FileService = new FileService();
        }

        /// <summary>
        /// Executes the command and restores the given directory onto the SQL server
        /// </summary>
        /// <param name="options">The options.</param>
        public override int Execute(Options options)
        {
            PruneConsole.OutputVersion(Console.ForegroundColor);

            if (!FileService.IsDirectory(options.Path))
            {
                Console.WriteLine("Invalid path: You must provide a path to a folder.");

                return (int) ExitCode.GeneralError;
            }

            Console.WriteLine("Listing all .bak files in folder {0} including subfolders...", options.Path);
            Console.WriteLine();

            var paths = FileService.GetFiles(options.Path, "*.bak", SearchOption.AllDirectories);

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

            if (options.Verbose)
            {
                foreach (var databaseBakupSet in backupSets)
                {
                    Console.WriteLine("Backup set for {0}:", databaseBakupSet.Key);
                    Console.WriteLine(" Created\t\tPath");
                    foreach (var model in databaseBakupSet)
                    {
                        Console.WriteLine(" {0}\t{1}:", model.Created.ToString("yyyyMMddTHHmmss", CultureInfo.InvariantCulture), model.Path);
                    }
                    Console.WriteLine();
                }
            }

            return (int) ExitCode.Success;
        }
    }
}

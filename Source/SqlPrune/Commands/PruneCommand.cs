using System;
using System.IO;
using Comsec.SqlPrune.Interfaces;
using Comsec.SqlPrune.Services;
using Sugar.Command;

namespace Comsec.SqlPrune.Commands
{
    /// <summary>
    /// Prunes a given location from its .bak files.
    /// </summary>
    public class PruneCommand : BoundCommand<PruneCommand.Options>
    {
        [Flag("path")]
        public class Options
        {
            [Parameter("path", Required = true)]
            public string Path { get; set; }

            /// <summary>
            /// Gets or sets the destination directory.
            /// </summary>
            /// <value>
            /// The db file path.
            /// </value>
            [Parameter("delete", Default = "false", Required = false)]
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
            if (!FileService.IsDirectory(options.Path))
            {
                Console.WriteLine("Invalid path: You must provide a path to a folder.");

                return (int) ExitCode.GeneralError;
            }

            return (int) ExitCode.Success;
        }
    }
}

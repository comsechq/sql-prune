using System;
using System.Collections.Generic;
using System.Linq;
using Comsec.SqlPrune.Interfaces.Services.Providers;
using Comsec.SqlPrune.Models;
using Comsec.SqlPrune.Services.Providers;
using Sugar.Command;

namespace Comsec.SqlPrune.Commands
{
    /// <summary>
    /// Base class for commands that depending on file providers.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public abstract class BaseFileProviderCommand<T> : BoundCommand<T> where T : class, new()
    {
        #region Dependencies

        /// <summary>
        /// Gets or sets the file service.
        /// </summary>
        /// <value>
        /// The file service.
        /// </value>
        public IFileProvider[] FileProviders { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="BaseFileProviderCommand{T}"/> class.
        /// </summary>
        protected BaseFileProviderCommand()
        {
            FileProviders = new IFileProvider[]
                            {
                                new S3Provider(),
                                new LocalFileSystemProvider()
                            };
        }

        #endregion

        /// <summary>
        /// Gets the provider.
        /// </summary>
        /// <param name="path">The path.</param>
        /// <param name="provider">The selected provider.</param>
        /// <returns>True if a provider for the specified <see cref="path"/> was found.</returns>
        protected bool GetProvider(string path, out IFileProvider provider)
        {
            var success = true;

            PruneConsole.OutputVersion();

            provider = FileProviders.FirstOrDefault(p => p.ShouldRun(path));

            if (provider == null)
            {
                Console.WriteLine("Unrecognised path. You must provide a path to a local folder or to an Amazon S3 bucket.");
                Console.WriteLine(@"Example: x:\path\to\folder or s3://bucket-name/optionally/with/path/folder");

                success = false;
            }

            if (string.IsNullOrEmpty(path) || path.StartsWith("-") || !provider.IsDirectory(path))
            {
                Console.WriteLine("Invalid path: You must provide a path to an existing local folder or drive.");

                success = false;
            }

            return success;
        }

        /// <summary>
        /// Maps the specified paths to a list of backup models.
        /// </summary>
        /// <param name="paths">The paths.</param>
        /// <returns></returns>
        protected IList<BakModel> ToBakModels(IEnumerable<KeyValuePair<string, long>> paths)
        {
            var files = new List<BakModel>();

            foreach (var pathAndSize in paths)
            {
                BakModel model;

                if (!BakFilenameExtractor.ValidateFilenameAndExtract(pathAndSize.Key, out model)) continue;

                model.Size = pathAndSize.Value;

                files.Add(model);
            }

            return files;
        }
    }
}

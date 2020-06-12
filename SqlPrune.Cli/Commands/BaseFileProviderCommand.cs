using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Comsec.SqlPrune.Models;
using Comsec.SqlPrune.Providers;

namespace Comsec.SqlPrune.Commands
{
    /// <summary>
    /// Base class for commands depending on file providers.
    /// </summary>
    public abstract class BaseFileProviderCommand
    {
        private readonly IEnumerable<IFileProvider> fileProviders;

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="fileProviders"></param>
        protected BaseFileProviderCommand(IEnumerable<IFileProvider> fileProviders)
        {
            this.fileProviders = fileProviders;
        }

        /// <summary>
        /// Gets the provider.
        /// </summary>
        /// <param name="path">The path.</param>
        /// <returns>Null when no provider for the specified <see cref="path"/> was found.</returns>
        protected async Task<IFileProvider> GetProvider(string path)
        {
            var provider = fileProviders.FirstOrDefault(p => p.ShouldRun(path));

            if (provider == null)
            {
                throw new ApplicationException(@"Unrecognised path. You must provide a path to a local folder (or an Amazon S3 bucket).
Example: ""x:\path\to\folder"" or ""s3://bucket-name/optionally/with/path/folder""");
            }
            else
            {
                var isDirectory = await provider.IsDirectory(path);

                if (string.IsNullOrEmpty(path) || path.StartsWith("-") || !isDirectory)
                {
                    throw new ApplicationException(
                        $"Invalid path: \"{path}\". You must provide a path to an existing folder");
                }
            }
            
            if (provider == null)
            {
                throw new ApplicationException("Could not determine file provider");
            }

            return provider;
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

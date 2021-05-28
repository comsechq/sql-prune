using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Comsec.SqlPrune.Extensions;
using Comsec.SqlPrune.Logging;
using Sugar.Extensions;

namespace Comsec.SqlPrune.Providers
{
    /// <summary>
    /// Wrapper service for access to <see cref="System.IO.File"/> and <see cref="System.IO.Directory"/>
    /// </summary>
    public class LocalFileSystemProvider : IFileProvider
    {
        private readonly ILogger logger;

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="logger"></param>
        public LocalFileSystemProvider(ILogger logger)
        {
            this.logger = logger;
        }

        /// <summary>
        /// Method called by the command to determine which <see cref="IFileProvider" /> implementation should run.
        /// </summary>
        /// <param name="path">The path.</param>
        /// <returns></returns>
        public bool ShouldRun(string path)
        {
            return !string.IsNullOrEmpty(path) &&
                   !(path.StartsWith(@"\\") || path.Contains("://"));
        }

        /// <summary>
        /// Extracts the filename from path.
        /// </summary>
        /// <param name="path">The path.</param>
        /// <returns></returns>
        public string ExtractFilenameFromPath(string path)
        {
            return path.SubstringAfterLastChar(@"\");
        }

        /// <summary>
        /// Determines whether the specified path is a directory.
        /// </summary>
        /// <param name="path">The path.</param>
        /// <returns></returns>
        public Task<bool> IsDirectory(string path)
        {
            try
            {
                var pathAttributes = File.GetAttributes(path);

                return Task.FromResult((pathAttributes & FileAttributes.Directory) == FileAttributes.Directory);
            }
            catch(DirectoryNotFoundException e)
            {
                logger.WriteLine(e.Message);
            }
            catch(FileNotFoundException e)
            {
                logger.WriteLine(e.Message);
            }

            return Task.FromResult(false);
        }

        /// <summary>
        /// Gets the size of the file.
        /// </summary>
        /// <param name="path">The path.</param>
        /// <returns></returns>
        public Task<long> GetFileSize(string path)
        {
            try
            {
                var info = new FileInfo(path);

                return Task.FromResult(info.Length);
            }
            catch (FileNotFoundException)
            {
                return Task.FromResult((long) -1);
            }
        }

        /// <summary>
        /// Returns the names of files (including their paths) that match the specified search pattern in the specified directory.
        /// </summary>
        /// <param name="dirPath">The directory to search.</param>
        /// <param name="searchPattern">The search patter (e.g. "*.txt").</param>
        /// <returns>
        /// A dictionary listing each file found and its size (in bytes).
        /// </returns>
        /// <remarks>
        /// System Files and Folders will be ignored
        /// </remarks>
        public Task<IDictionary<string, long>> GetFiles(string dirPath, params string[] searchPattern)
        {
            var info = new DirectoryInfo(dirPath);

            var matchingFiles = WalkDirectory(info, searchPattern);

            IDictionary<string, long> result = new Dictionary<string, long>(matchingFiles.Count);

            foreach (var filename in matchingFiles)
            {
                var fileInfo = new FileInfo(filename);

                result.Add(filename, fileInfo.Length);
            }

            return Task.FromResult(result);
        }

        /// <summary>
        /// Recursive method to walks the directory including any subdirectories and return all files found matching the given <see cref="searchPattern"/>.
        /// </summary>
        /// <param name="root">The root.</param>
        /// <param name="searchPatterns">The search patterns (e.g. "*.bak").</param>
        /// <returns></returns>
        /// <remarks>
        /// Inspired from code example on http://msdn.microsoft.com/en-us/library/bb513869.aspx
        /// </remarks>
        private IList<string> WalkDirectory(DirectoryInfo root, params string[] searchPatterns)
        {
            var result = new List<string>();

            if (root.Name != "$RECYCLE.BIN")
            {
                try
                {
                    var files = root.EnumerateFiles()
                                    .MatchOnAny(x => x.Name, searchPatterns);

                    result.AddRange(files.Select(x => x.Directory + (x.DirectoryName.EndsWith(@"\") ? null : @"\") + x.Name));
                }
                catch (UnauthorizedAccessException e)
                {
                    logger.WriteLine(e.Message);
                }
                catch (DirectoryNotFoundException e)
                {
                    logger.WriteLine(e.Message);
                }
            }

            try
            {
                var subDirs = root.GetDirectories();
                foreach (var dirInfo in subDirs)
                {
                    var filesInSubDirectory = WalkDirectory(dirInfo, searchPatterns);

                    result.AddRange(filesInSubDirectory);
                }
            }
            catch (UnauthorizedAccessException e)
            {
                logger.WriteLine(e.Message);
            }
            catch (DirectoryNotFoundException e)
            {
                logger.WriteLine(e.Message);
            }
            
            return result;
        }
        
        /// <summary>
        /// Deletes the specified file.
        /// </summary>
        /// <param name="path">The path.</param>
        public Task Delete(string path)
        {
            File.Delete(path);

            return Task.CompletedTask;
        }

        /// <summary>
        /// Copies to local asynchronously.
        /// </summary>
        /// <param name="path">The path.</param>
        /// <param name="destinationPath">The destination path.</param>
        /// <returns></returns>
        public Task CopyToLocalAsync(string path, string destinationPath)
        {
            File.Copy(path, destinationPath);

            return Task.CompletedTask;
        }
    }
}

using System.IO;
using Comsec.SqlPrune.Interfaces;

namespace Comsec.SqlPrune.Services
{
    /// <summary>
    /// Wrapper service for access to <see cref="System.IO.File"/>.
    /// </summary>
    public class FileService : IFileService
    {
        /// <summary>
        /// Determines whether the specified path is a directory.
        /// </summary>
        /// <param name="path">The path.</param>
        /// <returns></returns>
        public bool IsDirectory(string path)
        {
            var pathAttributes = File.GetAttributes(path);

            return (pathAttributes & FileAttributes.Directory) != FileAttributes.Directory;
        }
    }
}

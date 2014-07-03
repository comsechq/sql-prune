using System.Collections.Generic;

namespace Comsec.SqlPrune.Interfaces.Services
{
    /// <summary>
    /// Interface to wrap call to <see cref="System.IO.File" />
    /// </summary>
    public interface IFileService
    {
        /// <summary>
        /// Determines whether the specified path is a directory.
        /// </summary>
        /// <param name="path">The path.</param>
        /// <returns></returns>
        bool IsDirectory(string path);

        /// <summary>
        /// Returns the names of files (including their paths) that match the specified search pattern in the specified directory.
        /// </summary>
        /// <param name="dirPath">The directory to search.</param>
        /// <param name="searchPattern">The search patter (e.g. "*.txt").</param>
        /// <returns>
        /// A list of files.
        /// </returns>
        /// <remarks>
        /// System Files and Folders will be ignored
        /// </remarks>
        IList<string> GetFiles(string dirPath, string searchPattern);

        /// <summary>
        /// Deletes the specified file.
        /// </summary>
        /// <param name="path">The path.</param>
        void Delete(string path);
    }
}

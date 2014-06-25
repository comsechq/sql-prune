using System.IO;

namespace Comsec.SqlPrune.Interfaces
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
        /// Returns the names of files (including their paths) that match the specified 
        /// search pattern in the specified directory, using a value to determine whether
        /// to search subdirectories.
        /// </summary>
        /// <param name="dirPath">The directory to search.</param>
        /// <param name="searchPattern">
        /// The search string to match against the names of files in path. The parameter
        /// cannot end in two periods ("..") or contain two periods ("..") followed by
        /// System.IO.Path.DirectorySeparatorChar or System.IO.Path.AltDirectorySeparatorChar,
        /// nor can it contain any of the characters in System.IO.Path.InvalidPathChars.</param>
        /// <param name="option">
        /// One of the enumeration values that specifies whether the search operation
        /// should include all subdirectories or only the current directory.</param>
        /// <returns>
        /// An array of the full names (including paths) for the files in the specified
        /// directory that match the specified search pattern and option.
        /// </returns>
        string[] GetFiles(string dirPath, string searchPattern, SearchOption option);
    }
}

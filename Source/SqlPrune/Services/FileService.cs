using System;
using System.IO;
using Comsec.SqlPrune.Interfaces;

namespace Comsec.SqlPrune.Services
{
    /// <summary>
    /// Wrapper service for access to <see cref="System.IO.File"/> and <see cref="System.IO.Directory"/>
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

            return (pathAttributes & FileAttributes.Directory) == FileAttributes.Directory;
        }

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
        /// <remarks>System Files and Folders will be ignored</remarks>
        public string[] GetFiles(string dirPath, string searchPattern, SearchOption option)
        {
            string[] result = null;

            // TODO: Do the reccuring search manually to silently ignore the system files
            // http://stackoverflow.com/questions/172544/ignore-folders-files-when-directory-getfiles-is-denied-access/172575#172575

            try
            {
                result = Directory.GetFiles(dirPath, searchPattern, option);
            }
            catch(UnauthorizedAccessException ex)
            {
                Console.WriteLine("Unauthorized access to: " + ex.Message);
            }

            return result;
        }
    }
}

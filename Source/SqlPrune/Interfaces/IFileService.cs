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
    }
}

namespace Comsec.SqlPrune.Logging
{
    /// <summary>
    /// Interface to log a line to the console.
    /// </summary>
    public interface ILogger
    {
        ILogger Write(string format, params object[] parameters);

        ILogger WriteLine(string format = null, params object[] parameters);
    }
}

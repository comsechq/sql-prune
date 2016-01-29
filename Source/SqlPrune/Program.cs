namespace Comsec.SqlPrune
{
    /// <summary>
    /// Entry point for the command line tool.
    /// </summary>
    public class Program
    {
        static int Main(string[] args)
        {
            var console = new PruneConsole();
            
            return console.Run(args);
        }
    }
}

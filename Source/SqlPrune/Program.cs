namespace Comsec.SqlPrune
{
    /// <summary>
    /// Entry point for the command line tool.
    /// </summary>
    public class Program
    {
        static void Main(string[] args)
        {
            var console = new PruneConsole();
            
            console.Run(args);
        }
    }
}

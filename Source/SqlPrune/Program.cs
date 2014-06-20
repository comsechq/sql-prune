using Comsec.SqlPrune.Commands;

namespace Comsec.SqlPrune
{
    public class Program
    {
        static void Main(string[] args)
        {
            var console = new PruneConsole();
            console.Commands.Add(new PruneCommand());

            console.Run(args);
        }
    }
}

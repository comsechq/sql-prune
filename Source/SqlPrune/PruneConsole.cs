using System;
using System.Diagnostics;
using System.Reflection;
using Sugar.Command;

namespace Comsec.SqlPrune
{
    /// <summary>
    /// Main Command Console
    /// </summary>
    public class PruneConsole : BaseCommandConsole
    {
        public static void OutputVersion()
        {
            var assembly = Assembly.GetExecutingAssembly();
            var fvi = FileVersionInfo.GetVersionInfo(assembly.Location);
            var version = fvi.FileVersion;

            Console.WriteLine();
            ColorConsole.Write(ConsoleColor.White, "SQL Pruning Utility - v{0}", version);
            Console.WriteLine(" - Comsec Solutions Ltd - http://comsechq.com");
            Console.WriteLine();
        }

        /// <summary>
        /// Displays the help message
        /// </summary>
        public override int Default()
        {
            OutputVersion();

            Console.WriteLine("A simple utility to to prune MS-SQL backup files from a given folder.");
            Console.WriteLine();
            Console.Write("Get more information or contribute on github: ");
            
            ColorConsole.Write(ConsoleColor.White, "https://github.com/comsechq/sql-prune");
            Console.WriteLine();
            Console.WriteLine();
            Console.WriteLine("Usage:");
            Console.WriteLine();
            Console.WriteLine("    sqlprune.exe [pathToFolder] [-delete]");
            Console.WriteLine();
            Console.WriteLine(" pathToFolder: The path to your local folder containting .bak files (e.g. \"c:\\sql-backups\")");
            Console.WriteLine("       delete: Unless this flag is present files will not be deleted");
            Console.WriteLine();

            ColorConsole.Write(ConsoleColor.Red, "WARNING: ");
            Console.WriteLine("This program is designed to delete files from your computer. The authors do not accept liability for any errors or data-loss which could arise as a result of running this executable.");

            return (int) ExitCode.GeneralError;
        }
    }
}

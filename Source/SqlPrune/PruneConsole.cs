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
        public static void OutputVersion(ConsoleColor previousForegroundColor)
        {
            var assembly = Assembly.GetExecutingAssembly();
            var fvi = FileVersionInfo.GetVersionInfo(assembly.Location);
            var version = fvi.FileVersion;

            Console.WriteLine();
            Console.ForegroundColor = ConsoleColor.White;
            Console.Write("SQL Pruning Utility - v" + version);
            Console.ForegroundColor = previousForegroundColor;
            Console.WriteLine(" - Comsec Solutions Ltd - http://comsechq.com");
            Console.WriteLine();
        }

        /// <summary>
        /// Displays the help message
        /// </summary>
        public override int Default()
        {
            var previousColor = Console.ForegroundColor;

            OutputVersion(previousColor);

            Console.WriteLine("A simple utility to to prune MS-SQL backup files from a given folder.");
            Console.WriteLine();
            Console.Write("Get more information or contribute on github: ");
            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine("https://github.com/comsechq/sql-prune");
            Console.ForegroundColor = previousColor;
            Console.WriteLine();
            Console.WriteLine("Usage:");
            Console.WriteLine();
            Console.WriteLine("    sqlprune.exe [pathToFolder] [-delete]");
            Console.WriteLine();
            Console.WriteLine(" pathToFolder: The path to your local folder containting .bak files (e.g. \"c:\\sql-backups\")");
            Console.WriteLine("       delete: Unless this flag is present files will not be deleted");
            
            return (int) ExitCode.GeneralError;
        }
    }
}

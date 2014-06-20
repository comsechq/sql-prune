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
        /// <summary>
        /// Displays the help message
        /// </summary>
        public override int Default()
        {
            var assembly = Assembly.GetExecutingAssembly();
            var fvi = FileVersionInfo.GetVersionInfo(assembly.Location);
            var version = fvi.FileVersion;

            var previousColour = Console.ForegroundColor;
            
            Console.WriteLine();
            Console.ForegroundColor = ConsoleColor.White;
            Console.Write("SQL Pruning Utility - v" + version);
            Console.ForegroundColor = previousColour;
            Console.WriteLine(" - Comsec Solutions Ltd - http://comsechq.com");
            Console.WriteLine();
            Console.WriteLine("A simple utility to to prune MS-SQL backup files from a given folder.");
            Console.WriteLine();
            Console.Write("Get more information or contribute on github: ");
            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine("https://github.com/comsechq/sql-prune");
            Console.ForegroundColor = previousColour;
            Console.WriteLine();
            Console.WriteLine("Usage:");
            Console.WriteLine();
            Console.WriteLine("    sqlprune.exe -path [pathToFolder] [-delete]");
            Console.WriteLine();
            Console.WriteLine("   path: The path to your local folder containting .bak files (e.g. \"c:\\sql-backups\")");
            Console.WriteLine(" delete: Unless this flag is present files will not be deleted");
            
            return (int) ExitCode.GeneralError;
        }
    }
}

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

            ColorConsole.Write(ConsoleColor.Red, "WARNING: ");
            Console.WriteLine("This program is designed to delete files. The authors do not accept liability for any errors or data-loss which could arise as a result of running this executable.");
            Console.WriteLine();
        }

        /// <summary>
        /// Displays the help message
        /// </summary>
        public override int Default()
        {
            OutputVersion();

            Console.WriteLine("A simple utility to to prune MS-SQL backup files from a given folder or Amazon S3 bucket.");
            Console.WriteLine();
            Console.Write("Get more information or contribute on github: ");
            
            ColorConsole.Write(ConsoleColor.White, "https://github.com/comsechq/sql-prune");
            Console.WriteLine();
            Console.WriteLine();
            Console.WriteLine("Usage:");
            Console.WriteLine();
            Console.WriteLine(" sqlprune.exe [path] -prune [-delete] [-no-confirm]");
            Console.WriteLine();
            Console.WriteLine("       path: The path to a local folder or an S3 bucket containting .bak files (e.g. \"c:\\sql-backups\" or \"s3://bucket-name/backups\")");
            Console.WriteLine("     -prune: The flag to activate the 'prune' mode");
            Console.WriteLine("    -delete: Unless this flag is present files will not be deleted");
            Console.WriteLine(" -no-confim: You will have to confirm before any file is deleted unless this flag is present");
            Console.WriteLine("   -recover: If this flag is present the command");
            Console.WriteLine();
            Console.WriteLine(" sqlprune.exe [path] -recover -db-name -dest [-date] [-no-confirm]");
            Console.WriteLine();
            Console.WriteLine("       path: The path to a local folder or an S3 bucket containting .bak files (e.g. \"c:\\sql-backups\" or \"s3://bucket-name/backups\")");
            Console.WriteLine("   -recover: The flag to activate the 'recovery' mode");
            Console.WriteLine("   -db-name: The name of the database to recover");
            Console.WriteLine("      -dest: The path to a local folder where to copy the file to");
            Console.WriteLine("      -date: Specifies which date to retrieve (when not provided the most recent backup in the set will be used)");
            Console.WriteLine(" -no-confim: You will have to confirm before any file is recovered unless this flag is present");
            Console.WriteLine();

            return (int) ExitCode.GeneralError;
        }
    }
}

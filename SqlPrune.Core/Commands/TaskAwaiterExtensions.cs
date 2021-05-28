using System;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Amazon.S3.Model;
using Comsec.SqlPrune.Logging;

namespace Comsec.SqlPrune.Commands
{
    public static class TaskAwaiterExtensions
    {
        private static string animation = "░▒▓";

        private static int GetTop()
        {
            var top = -1;

            try
            {
                top = Console.CursorTop;
            }
            catch(IOException ex)
            {
                if (ex.Message.Contains("The handle is invalid"))
                {
                    // Console attached to the process (probably unit testing)
                }
                else
                {
                    throw ex;
                }
            }

            return top;
        }

        /// <summary>
        /// Outputs ▒ chars to the console until a task completes.
        /// </summary>
        /// <typeparam name="TResult"></typeparam>
        /// <param name="taskToAwait"></param>
        /// <param name="logger"></param>
        /// <returns></returns>
        public static TResult OutputProgress<TResult>(this TaskAwaiter<TResult> taskToAwait, ILogger logger) 
        {
            var i = 0;

            if (logger is ConsoleLogger consoleLogger)
            {
                var top = GetTop();

                do
                {
                    Thread.Sleep(250);

                    if (top > -1)
                    {
                        Console.SetCursorPosition(0, Console.CursorTop);
                        var animationIndex = i % animation.Length;
                        var block = animation.Substring(animationIndex, 1);
                        Console.Write($"{block} ({(i + 1) / 4} second(s) elapsed...");
                    }

                    i++;
                } while (!taskToAwait.IsCompleted);

                Console.WriteLine(Environment.NewLine);
            }
            else
            {
                do
                {
                    i++;
                    Thread.Sleep(100);
                    // No output
                } while (!taskToAwait.IsCompleted);

                logger.WriteLine($"{(i + 1) / 10} second(s) elapsed...");
            }

            return taskToAwait.GetResult();
        }

        /// <summary>
        /// Outputs ▒ chars to the console until a task completes.
        /// </summary>
        /// <param name="taskToAwait"></param>
        /// <param name="logger"></param>
        /// <returns></returns>
        public static void OutputProgress(this TaskAwaiter taskToAwait, ILogger logger)
        {
            var i = 0;

            if (logger is ConsoleLogger consoleLogger)
            {
                var top = GetTop();

                do
                {
                    Thread.Sleep(250);

                    if (top > -1)
                    {
                        Console.SetCursorPosition(0, Console.CursorTop);
                        var animationIndex = i % animation.Length;
                        var block = animation.Substring(animationIndex, 1);
                        Console.Write($"{block} ({(i + 1) / 4} second(s) elapsed...");
                    }
                
                    i++;
                } while (!taskToAwait.IsCompleted);

                Console.WriteLine(Environment.NewLine);
            }
            else
            {
                do
                {
                    i++;
                    Thread.Sleep(100);
                } while (!taskToAwait.IsCompleted);

                logger.WriteLine($"{(i + 1) / 10} second(s) elapsed...");
            }
        }
    }
}
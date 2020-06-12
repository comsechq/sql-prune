using System;
using System.Runtime.CompilerServices;
using System.Threading;

namespace Comsec.SqlPrune.Commands
{
    public static class TaskAwaiterExtensions
    {
        private static string animation = "░▒▓";

        /// <summary>
        /// Outputs ▒ chars to the console until a task completes.
        /// </summary>
        /// <typeparam name="TResult"></typeparam>
        /// <param name="taskToAwait"></param>
        /// <returns></returns>
        public static TResult OutputProgress<TResult>(this TaskAwaiter<TResult> taskToAwait) 
        {
            var i = 0;

            do
            {
                Thread.Sleep(1000);
                Console.SetCursorPosition(0, Console.CursorTop);
                var animationIndex = i % animation.Length;
                var block = animation.Substring(animationIndex, 1);
                Console.Write($"{block} ({i + 1} second(s) elapsed...");
                i++;
            } while (!taskToAwait.IsCompleted);

            Console.WriteLine(Environment.NewLine);

            return taskToAwait.GetResult();
        }

        /// <summary>
        /// Outputs ▒ chars to the console until a task completes.
        /// </summary>
        /// <param name="taskToAwait"></param>
        /// <returns></returns>
        public static void OutputProgress(this TaskAwaiter taskToAwait) 
        {
            var i = 0;

            do
            {
                Thread.Sleep(1000);
                Console.SetCursorPosition(0, Console.CursorTop);
                var animationIndex = i % animation.Length;
                var block = animation.Substring(animationIndex, 1);
                Console.Write($"{block} ({i + 1} second(s) elapsed...");
                i++;
            } while (!taskToAwait.IsCompleted);

            Console.WriteLine(Environment.NewLine);
        }
    }
}
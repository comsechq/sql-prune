using System;

namespace Comsec.SqlPrune.Commands
{
    /// <summary>
    /// Helper class to interact with the user.
    /// </summary>
    public static class Confirm
    {
        /// <summary>
        /// Prompts the specified question.
        /// </summary>
        /// <param name="question">The question.</param>
        /// <param name="expectedAnswer">The expected answer.</param>
        /// <returns>True if the user types the expected answer, false otherwise.</returns>
        public static bool Prompt(string question, string expectedAnswer)
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine();
            Console.WriteLine(question);
            ColorConsole.Write(ConsoleColor.DarkGray, "To continue please type '");
            ColorConsole.Write(ConsoleColor.White, expectedAnswer);
            ColorConsole.Write(ConsoleColor.DarkGray, "': ");

            var check = Console.ReadLine();

            return check == expectedAnswer;
        }
    }
}
using System;
using Comsec.SqlPrune.Logging;

namespace Comsec.SqlPrune.Commands
{
    /// <summary>
    /// Helper class to interact with the user.
    /// </summary>
    public class Confirm
    {
        private readonly ILogger logger;

        public Confirm(ILogger logger)
        {
            this.logger = logger;
        }

        /// <summary>
        /// Prompts the specified question.
        /// </summary>
        /// <param name="question">The question.</param>
        /// <param name="expectedAnswer">The expected answer.</param>
        /// <returns>True if the user types the expected answer, false otherwise.</returns>
        public bool Prompt(string question, string expectedAnswer)
        {
            logger.Write(ConsoleColor.Yellow, question + Environment.NewLine)
                  .Write(ConsoleColor.DarkGray, "To continue please type '")
                  .Write(ConsoleColor.White, expectedAnswer)
                  .Write(ConsoleColor.DarkGray, "': ");
            
            var check = Console.ReadLine();

            return check == expectedAnswer;
        }
    }
}
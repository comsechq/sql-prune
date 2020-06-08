using System;

namespace Comsec.SqlPrune
{
    /// <summary>
    /// Helper class to output something to the console
    /// </summary>
    public static class ColorConsole
    {
        /// <summary>
        /// Writes to the console using the the specified color.
        /// </summary>
        /// <param name="color">The color.</param>
        /// <param name="format">The format.</param>
        /// <param name="parameters">The parameters.</param>
        public static void Write(ConsoleColor color, string format, params object[] parameters)
        {
            Console.ForegroundColor = color;
            Console.Write(format, parameters);
            Console.ResetColor();
        }

    }
}

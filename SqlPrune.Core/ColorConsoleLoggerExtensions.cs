using System;
using Comsec.SqlPrune.Logging;

namespace Comsec.SqlPrune
{
    /// <summary>
    /// Helper class to output something to the console
    /// </summary>
    public static class ColorConsoleLoggerExtensions
    {
        /// <summary>
        /// Writes to the console using the the specified color (or not if the <see cref="logger"/> does not support it).
        /// </summary>
        /// <param name="logger">The logger.</param>
        /// <param name="color">The color.</param>
        /// <param name="format">The value to format.</param>
        /// <param name="parameters">The parameters (optional).</param>
        public static ILogger Write(this ILogger logger, ConsoleColor color, string format, params object[] parameters)
        {
            var consoleLogger = logger as ConsoleLogger;

            if (consoleLogger != null)
            {
                consoleLogger.ForegroundColor = color;
            }

            logger.Write(format, parameters);

            consoleLogger?.ResetColor();

            return logger;
        }

    }
}

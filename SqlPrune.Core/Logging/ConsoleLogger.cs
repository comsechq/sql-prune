namespace Comsec.SqlPrune.Logging
{
    /// <summary>
    /// Logs lines to the system console.
    /// </summary>
    public class ConsoleLogger : ILogger
    {
        public System.ConsoleColor ForegroundColor
        {
            get => System.Console.ForegroundColor;
            set => System.Console.ForegroundColor = value;
        }

        public System.ConsoleColor BackgroundColor
        {
            get => System.Console.BackgroundColor;
            set => System.Console.ForegroundColor = value;
        }

        public void ResetColor()
        {
            System.Console.ResetColor();
        }

        public ILogger Write(string format, params object[] parameters)
        {
            if (format == null)
            {
                format = string.Empty;
            }

            System.Console.Write(format, parameters);

            return this;
        }

        public ILogger WriteLine(string format, params object[] parameters)
        {
            if (format == null)
            {
                format = string.Empty;
            }

            System.Console.WriteLine(format, parameters);

            return this;
        }
    }
}
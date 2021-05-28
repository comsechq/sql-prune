using Amazon.Lambda.Core;
using Comsec.SqlPrune.Logging;

namespace SqlPrune.Lambda.Logger
{
    public class LambdaLogger : ILogger
    {
        private readonly ILambdaLogger logger;

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="logger"></param>
        public LambdaLogger(ILambdaLogger logger)
        {
            this.logger = logger;
        }

        public ILogger Write(string format, params object[] parameters)
        {
            format ??= string.Empty;

            logger.Log(string.Format(format, parameters));


            return this;
        }

        /// <summary>
        /// Logs a line.
        /// </summary>
        /// <returns></returns>
        public ILogger WriteLine(string format, params object[] parameters)
        {
            format ??= string.Empty;

            logger.LogLine(string.Format(format, parameters));

            return this;
        }
    }
}

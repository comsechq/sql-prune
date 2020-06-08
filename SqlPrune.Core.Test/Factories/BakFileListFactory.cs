using System;
using System.Collections.Generic;
using System.Text;

namespace Comsec.SqlPrune.Factories
{
    /// <summary>
    /// Helper class that generate a simulated listing of MS SQL database backup .bak files.
    /// </summary>
    public class BakFileListFactory
    {
        private static readonly Random Generator = new Random();

        public List<string> DatabaseNames { get; set; }

        public BakFileListFactory()
        {
            DatabaseNames = new List<string>();
        }

        /// <summary>
        /// Withes the database.
        /// </summary>
        /// <param name="databaseName">Name of the database.</param>
        /// <returns></returns>
        public BakFileListFactory WithDatabases(string databaseName)
        {
            return WithDatabases(new[] {databaseName});
        }

        /// <summary>
        /// Withes the databases.
        /// </summary>
        /// <param name="databaseNames">The database names.</param>
        /// <returns></returns>
        public BakFileListFactory WithDatabases(IEnumerable<string> databaseNames)
        {
            DatabaseNames.AddRange(databaseNames);

            return this;
        }

        /// <summary>
        /// Creates the string of X number of digits.
        /// </summary>
        /// <param name="length">The length.</param>
        /// <returns></returns>
        public static string CreateDigitString(int length)
        {
            var builder = new StringBuilder();
            while (builder.Length < length)
            {
                builder.Append(Generator.Next(10).ToString());
            }
            return builder.ToString();
        }

        /// <summary>
        /// Creates the specified from.
        /// </summary>
        /// <param name="from">The date we should start simulating backups from.</param>
        /// <param name="until">The date we should stop simulating backups until.</param>
        /// <returns>
        /// A generated list of files and their size.
        /// </returns>
        public IDictionary<string, long> Create(DateTime from, DateTime until)
        {
            return Create(from, until - from);
        }

        /// <summary>
        /// Creates the specified from.
        /// </summary>
        /// <param name="from">From.</param>
        /// <param name="duration">The duration.</param>
        /// <returns>
        /// A generated list of files and their size.
        /// </returns>
        public IDictionary<string, long> Create(DateTime from, TimeSpan duration)
        {
            var days = duration.Days;

            return Create(from, days);
        }

        /// <summary>
        /// Creates a list of file names, one per day for each <see cref="DatabaseNames" />.
        /// </summary>
        /// <param name="from">From.</param>
        /// <param name="numberOfDays">The number of days.</param>
        /// <returns>
        /// A generated list of files and their size.
        /// </returns>
        public IDictionary<string, long> Create(DateTime from, int numberOfDays)
        {
            Dictionary<string, long> result = null;

            // Helps simulating a BAK file name: dbname1_backup_2014_03_29_010003_3911004.bak
            const string filenameFormat = "{0}_backup_{1}_{2}_{3}.bak";

            if (numberOfDays > 0)
            {
                result = new Dictionary<string, long>(numberOfDays * DatabaseNames.Count);

                for (var i = 0; i < numberOfDays; i++)
                {
                    var currentDate = from.AddDays(i);

                    var date = currentDate.ToString("yyyy_MM_dd");
                    var time = currentDate.ToString("HHmmss");
                    
                    foreach (var databaseName in DatabaseNames)
                    {
                        var random = CreateDigitString(7);
                        
                        var filename = string.Format(filenameFormat, databaseName, date, time, random);

                        result.Add(filename, Convert.ToInt64(random));
                    }
                }
            }

            return result;
        }
    }
}

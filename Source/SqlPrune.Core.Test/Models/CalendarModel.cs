using System.Collections.Generic;
using System.Runtime.Serialization;

namespace Comsec.SqlPrune.Models
{
    /// <summary>
    /// Model used to generate Calendar.json, used by the calendar visualiser (Calendar.html) 
    /// to visualised the output of <see cref="Services.PruneServiceTest"/>.
    /// </summary>
    [DataContract]
    public class CalendarModel
    {
        /// <summary>
        /// Gets or sets the values.
        /// </summary>
        /// <value>
        /// The values.
        /// </value>
        [DataMember(Name = "values")]
        public IEnumerable<DateModel> Values { get; set; }

        /// <summary>
        /// Gets or sets the start year.
        /// </summary>
        /// <value>
        /// The start year.
        /// </value>
        [DataMember(Name = "startYear")]
        public int StartYear { get; set; }

        /// <summary>
        /// Gets or sets the end year.
        /// </summary>
        /// <value>
        /// The end year.
        /// </value>
        [DataMember(Name = "endYear")]
        public int EndYear { get; set; }

        /// <summary>
        /// Gets or sets the minimum value.
        /// </summary>
        /// <value>
        /// The minimum value.
        /// </value>
        [DataMember(Name = "minValue")]
        public int MinValue { get; set; }

        /// <summary>
        /// Gets or sets the maximum value.
        /// </summary>
        /// <value>
        /// The maximum value.
        /// </value>
        [DataMember(Name = "maxValue")]
        public int MaxValue { get; set; }
    }
}

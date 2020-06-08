using System;
using System.Runtime.Serialization;

namespace Comsec.SqlPrune.Models
{
    /// <summary>
    /// Model reprensenting how many occurences of something happened on a given date
    /// </summary>
    [DataContract]
    public class DateModel
    {
        /// <summary>
        /// Gets or sets the date.
        /// </summary>
        /// <value>
        /// The date.
        /// </value>
        [DataMember(Name = "date")]
        public DateTime Date { get; set; }

        /// <summary>
        /// Gets or sets the count.
        /// </summary>
        /// <value>
        /// The count.
        /// </value>
        [DataMember(Name = "count")]
        public int Count { get; set; }
    }
}
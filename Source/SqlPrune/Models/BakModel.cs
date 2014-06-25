using System;

namespace Comsec.SqlPrune.Models
{
    /// <summary>
    /// Model representing a bak file
    /// </summary>
    public class BakModel
    {
        /// <summary>
        /// Gets or sets the path.
        /// </summary>
        /// <value>
        /// The path.
        /// </value>
        public string Path { get; set; }

        /// <summary>
        /// Gets or sets the filename.
        /// </summary>
        /// <value>
        /// The filename.
        /// </value>
        public string DatabaseName { get; set; }

        /// <summary>
        /// Gets or sets when the backup was created.
        /// </summary>
        /// <value>
        /// The backup date and time.
        /// </value>
        public DateTime Created { get; set; }

        /// <summary>
        /// Gets or sets the can be pruned.
        /// </summary>
        /// <value>
        /// The can be pruned.
        /// </value>
        /// <remarks>
        /// This property has no value when it hasn't been assereted that this backup can be prunned, 
        /// with regard to a given backup set. <see cref="Services.PruneService"/>
        /// </remarks>
        public bool? Prunable { get; set; }
    }
}

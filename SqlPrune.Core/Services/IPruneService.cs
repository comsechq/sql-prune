using System.Collections.Generic;
using Comsec.SqlPrune.Models;

namespace Comsec.SqlPrune.Services
{
    /// <summary>
    /// Interface for the pruning service (business logic that decides whether or not to prune a backup from a set).
    /// </summary>
    public interface IPruneService
    {
        /// <summary>
        /// Sets prunable backups in set.
        /// </summary>
        /// <param name="set">The set of backups for a given database.</param>
        void FlagPrunableBackupsInSet(IEnumerable<BakModel> set);
    }
}

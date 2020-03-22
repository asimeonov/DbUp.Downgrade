using System.Collections.Generic;
using System.Linq;
using DbUp.Engine;

namespace DbUp.Downgrade.Helpers
{
    /// <summary>
    /// Default matching for scripts. Comparing scriptToExecute.Name to downgradeScript.Name.
    /// </summary>
    public class DefaultDowngradeScriptFinder : IDowngradeScriptFinder
    {
        /// <summary>
        /// Default matching for scripts. Comparing scriptToExecute.Name to downgradeScript.Name
        /// </summary>
        public SqlScript GetCorrespondingDowngradeScript(SqlScript scriptToExecute, List<SqlScript> downgradeScripts)
        {
            return downgradeScripts.SingleOrDefault(downgradeScript => downgradeScript.Name.Equals(scriptToExecute.Name));
        }
    }
}

using System.Collections.Generic;
using DbUp.Engine;

namespace DbUp.Downgrade.Helpers
{
    public interface IDowngradeScriptFinder
    {
        SqlScript GetCorrespondingDowngradeScript(SqlScript scriptToExecute, List<SqlScript> downgradeScripts);
    }
}
using DbUp.Builder;
using DbUp.Engine;
using DbUp.Engine.Output;
using DbUp.Engine.Transactions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace DbUp.Rollback
{
    public class RollbackEnabledUpgradeEngine
    {
        public UpgradeEngine UpgradeEngine { get; private set; }

        private RollbackEnabledTableJournal _journal;
        private List<IScriptProvider> _scriptProviders;
        private IConnectionManager _connectionManager;
        private IUpgradeLog _log;

        public RollbackEnabledUpgradeEngine(UpgradeEngineBuilder builder)
        {
            builder.Configure(c =>
            {
                _journal = c.Journal as RollbackEnabledTableJournal;
                _scriptProviders = c.ScriptProviders;
                _connectionManager = c.ConnectionManager;
                _log = c.Log;
            });

            UpgradeEngine = builder.Build();
        }

        public DatabaseUpgradeResult PerformUpgrade()
        {
            List<SqlScript> downgradeScripts = new List<SqlScript>();

            try
            {
                var configurationTransactionMode = _connectionManager.TransactionMode;
                _connectionManager.TransactionMode = TransactionMode.SingleTransaction;
                using (var opration = _connectionManager.OperationStarting(_log, new List<SqlScript>()))
                {
                    var allScripts = _scriptProviders.SelectMany(scriptProvider => scriptProvider.GetScripts(_connectionManager));

                    var executedScripts = _journal.GetExecutedScriptsInReverseOrder();

                    foreach (var executedScript in executedScripts)
                    {
                        if (!allScripts.Any(s => s.Name.Equals(executedScript)))
                        {
                            string rollbackScript = _journal.GetRollbackScript(executedScript);

                            _journal.RevertScript(executedScript, rollbackScript);
                        }
                    }
                }
                _connectionManager.TransactionMode = configurationTransactionMode;
            }
            catch (Exception e)
            {
                return new DatabaseUpgradeResult(downgradeScripts, false, e);
            }

            return UpgradeEngine.PerformUpgrade();
        }
    }
}

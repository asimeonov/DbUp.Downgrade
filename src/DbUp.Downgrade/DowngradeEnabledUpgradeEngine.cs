using DbUp.Builder;
using DbUp.Engine;
using DbUp.Engine.Output;
using DbUp.Engine.Transactions;
using System;
using System.Collections.Generic;
using System.Linq;

namespace DbUp.Downgrade
{
    public class DowngradeEnabledUpgradeEngine
    {
        public UpgradeEngine UpgradeEngine { get; private set; }

        private DowngradeEnabledTableJournal _journal;
        private List<IScriptProvider> _scriptProviders;
        private IConnectionManager _connectionManager;
        private IUpgradeLog _log;
        private readonly bool _autoDowngradeEnabled;

        public DowngradeEnabledUpgradeEngine(UpgradeEngineBuilder builder, bool autoDowngradeEnabled)
        {
            _autoDowngradeEnabled = autoDowngradeEnabled;

            builder.Configure(c =>
            {
                _journal = c.Journal as DowngradeEnabledTableJournal;
                _scriptProviders = c.ScriptProviders;
                _connectionManager = c.ConnectionManager;
                _log = c.Log;
            });

            UpgradeEngine = builder.Build();
        }

        public DatabaseUpgradeResult PerformDowngrade()
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
                            string downgradeScript = _journal.GetDowngradeScript(executedScript);

                            _journal.RevertScript(executedScript, downgradeScript);
                        }
                    }
                }
                _connectionManager.TransactionMode = configurationTransactionMode;
            }
            catch (Exception ex)
            {
                return new DatabaseUpgradeResult(downgradeScripts, false, ex);
            }

            return new DatabaseUpgradeResult(downgradeScripts, true, null);
        }

        public DatabaseUpgradeResult PerformUpgrade()
        {
            if (_autoDowngradeEnabled)
            {
                var downgradeResult = PerformDowngrade();

                if (!downgradeResult.Successful)
                {
                    return downgradeResult;
                }
            }

            return UpgradeEngine.PerformUpgrade();
        }
    }
}

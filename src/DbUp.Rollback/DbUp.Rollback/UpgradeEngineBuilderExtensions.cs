using DbUp.Builder;
using DbUp.Engine;
using DbUp.ScriptProviders;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace DbUp.Rollback
{
    public static class UpgradeEngineBuilderExtensions
    {
        public static UpgradeEngineBuilder WithScriptsAndRollbackScriptsEmbeddedInAssembly(this UpgradeEngineBuilder builder,
            Assembly assembly,
            RollbackScriptsSettings rollbackScriptsSettings,
            string schema = null)
        {
            builder.Configure(c => c.ScriptProviders.Add(new EmbeddedScriptProvider(assembly, rollbackScriptsSettings.ScriptsFilter)));
            builder.Configure(c => c.Journal = new TableWithRollbackJournal(() => c.ConnectionManager, () => c.Log, schema, "SchemaVersions",
                new EmbeddedScriptProvider(assembly, rollbackScriptsSettings.RollbackScriptsFilter)));

            return builder;
        }

        public static RollbackEnabledUpgradeEngine BuildWithRollbackEnabled(this UpgradeEngineBuilder builder)
        {
            return new RollbackEnabledUpgradeEngine(builder);
        }
    }
}

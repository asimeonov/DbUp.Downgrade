using DbUp.Builder;
using DbUp.ScriptProviders;
using System.Reflection;

namespace DbUp.Rollback
{
    public static class UpgradeEngineBuilderExtensions
    {
        public static UpgradeEngineBuilder WithScriptsAndRollbackScriptsEmbeddedInAssembly(this UpgradeEngineBuilder builder,
            Assembly assembly,
            RollbackScriptsSettings rollbackScriptsSettings,
            string schema = null, 
            string table = "SchemaVersions")
        {
            builder.Configure(c => c.ScriptProviders.Add(new EmbeddedScriptProvider(assembly, rollbackScriptsSettings.ScriptsFilter)));
            builder.Configure(c => c.Journal = new SqlRollbackEnabledTableJournal(() => c.ConnectionManager, () => c.Log, schema, table,
                new EmbeddedScriptProvider(assembly, rollbackScriptsSettings.RollbackScriptsFilter)));

            return builder;
        }

        public static RollbackEnabledUpgradeEngine BuildWithRollbackEnabled(this UpgradeEngineBuilder builder)
        {
            return new RollbackEnabledUpgradeEngine(builder);
        }
    }
}

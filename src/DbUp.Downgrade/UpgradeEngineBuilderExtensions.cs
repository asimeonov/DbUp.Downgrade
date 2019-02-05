using DbUp.Builder;
using DbUp.ScriptProviders;
using System;
using System.Reflection;

namespace DbUp.Downgrade
{
    public static class UpgradeEngineBuilderExtensions
    {
        public static UpgradeEngineBuilder WithScriptsAndDowngradeScriptsEmbeddedInAssembly(this UpgradeEngineBuilder builder,
            Assembly assembly,
            DowngradeScriptsSettings downgradeScriptsSettings,
            string schema = null,
            string table = "SchemaVersions")
        {
            builder.Configure(c => c.ScriptProviders.Add(new EmbeddedScriptProvider(assembly, downgradeScriptsSettings.ScriptsFilter)));
            builder.Configure(c => c.Journal = new SqlDowngradeEnabledTableJournal(() => c.ConnectionManager, () => c.Log, schema, table,
                new EmbeddedScriptProvider(assembly, downgradeScriptsSettings.DowngradeScriptsFilter)));

            return builder;
        }

        public static DowngradeEnabledUpgradeEngine BuildWithDowngrade(this UpgradeEngineBuilder builder, bool autoDowngradeEnabled)
        {
            return new DowngradeEnabledUpgradeEngine(builder, autoDowngradeEnabled);
        }
    }
}

using DbUp.Builder;
using DbUp.Engine;
using DbUp.Engine.Output;
using DbUp.Engine.Transactions;
using DbUp.ScriptProviders;
using System;
using System.Reflection;

namespace DbUp.Downgrade
{
    public static class UpgradeEngineBuilderExtensions
    {
        public static UpgradeEngineBuilder WithScriptsAndDowngradeScriptsEmbeddedInAssembly<TDowngradeEnabledTableJournal>(this UpgradeEngineBuilder builder,
            Assembly assembly,
            DowngradeScriptsSettings downgradeScriptsSettings,
            string schema = null,
            string table = null) where TDowngradeEnabledTableJournal : DowngradeEnabledTableJournal
        {
            builder.WithScriptsEmbeddedInAssembly(assembly, downgradeScriptsSettings.ScriptsFilter);
            builder.WithDowngradeTableProvider<TDowngradeEnabledTableJournal>(
                new EmbeddedScriptProvider(assembly, downgradeScriptsSettings.DowngradeScriptsFilter),
                schema, 
                table);

            return builder;
        }

        public static UpgradeEngineBuilder WithDowngradeTableProvider<TDowngradeEnabledTableJournal>(this UpgradeEngineBuilder builder,
            IScriptProvider downgradeScriptProvider,
            string schema = null,
            string table = null) where TDowngradeEnabledTableJournal : DowngradeEnabledTableJournal
        {
            builder.Configure(c =>
            {
                Func<IConnectionManager> connectionManager = () => c.ConnectionManager;
                Func<IUpgradeLog> log = () => c.Log;
                c.Journal = (TDowngradeEnabledTableJournal)Activator.CreateInstance(typeof(TDowngradeEnabledTableJournal),
                  connectionManager, log, schema, table, downgradeScriptProvider);
            });

            return builder;
        }

        public static DowngradeEnabledUpgradeEngine BuildWithDowngrade(this UpgradeEngineBuilder builder, bool autoDowngradeEnabled)
        {
            return new DowngradeEnabledUpgradeEngine(builder, autoDowngradeEnabled);
        }
    }
}

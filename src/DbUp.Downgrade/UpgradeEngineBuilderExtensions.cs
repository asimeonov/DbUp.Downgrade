using DbUp.Builder;
using DbUp.Downgrade.Helpers;
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
                new EmbeddedDowngradeScriptFinder(downgradeScriptsSettings),
                schema, 
                table);

            return builder;
        }

        /// <summary>
        /// Creates UpgradeEngineBuilder with WithDowngradeTableProvider.
        /// </summary>
        /// <typeparam name="TDowngradeEnabledTableJournal">Table inhered from DowngradeEnabledTableJournal</typeparam>
        /// <param name="builder">Static extention to UpgradeEngineBuilder</param>
        /// <param name="downgradeScriptProvider">Pre-defined downgrade scripts provider from DbUp.IScriptProvider</param>
        /// <param name="downgradeScriptFinder">IDowngradeScriptFinder implementation that can match scripts with downgradeScript. Matching by name is provided by default implementation <see cref="DefaultDowngradeScriptFinder"/></param>
        /// <param name="schema">Schema name</param>
        /// <param name="table">Table Name where execuded scripts will be stored default: "SchemaVersions".</param>
        /// <returns></returns>
        public static UpgradeEngineBuilder WithDowngradeTableProvider<TDowngradeEnabledTableJournal>(this UpgradeEngineBuilder builder,
            IScriptProvider downgradeScriptProvider,
            IDowngradeScriptFinder downgradeScriptFinder,
            string schema = null,
            string table = null) where TDowngradeEnabledTableJournal : DowngradeEnabledTableJournal
        {
            builder.Configure(c =>
            {
                Func<IConnectionManager> connectionManager = () => c.ConnectionManager;
                Func<IUpgradeLog> log = () => c.Log;
                c.Journal = (TDowngradeEnabledTableJournal)Activator.CreateInstance(typeof(TDowngradeEnabledTableJournal),
                  connectionManager, log, schema, table, downgradeScriptProvider, downgradeScriptFinder);
            });

            return builder;
        }

        public static DowngradeEnabledUpgradeEngine BuildWithDowngrade(this UpgradeEngineBuilder builder, bool autoDowngradeEnabled)
        {
            return new DowngradeEnabledUpgradeEngine(builder, autoDowngradeEnabled);
        }
    }
}

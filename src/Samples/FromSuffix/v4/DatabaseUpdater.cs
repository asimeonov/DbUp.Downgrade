using DbUp;
using DbUp.Downgrade;
using DbUp.Engine;
using System.Reflection;

namespace SampleScripts
{
    public static class DatabaseUpdater
    {
        public static void UpdateWithDowngradeEnabled(string connectionString, Action<DatabaseUpgradeResult> DisplayResult)
        {
            DowngradeScriptsSettings settings = DowngradeScriptsSettings.FromSuffix();

            var upgradeEngineBuilder = DeployChanges.To
                .SqlDatabase(connectionString)
                .WithScriptsAndDowngradeScriptsEmbeddedInAssembly<SqlDowngradeEnabledTableJournal>(Assembly.GetExecutingAssembly(), settings)
                .LogToConsole();

            var upgrader = upgradeEngineBuilder.BuildWithDowngrade(true);

            DatabaseUpgradeResult result = upgrader.PerformUpgrade();
            DisplayResult(result);
        }
    }
}
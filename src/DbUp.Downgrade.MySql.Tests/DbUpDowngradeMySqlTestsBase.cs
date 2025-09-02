using DbUp.Downgrade.Helpers;
using DbUp.Engine;
using DbUp.ScriptProviders;
using MySqlConnector;

namespace DbUp.Downgrade.MySql.Tests
{
    public abstract class DbUpDowngradeMySqlTestsBase
    {
        public readonly string _connectionString;

        protected DbUpDowngradeMySqlTestsBase(string connectionString)
        {
            _connectionString = connectionString;
        }

        [Fact]
        public void StaticScriptProvider_SuccessfullyStoresDowngradeScripts()
        {
            var upgradeScriptProvider = new StaticScriptProvider(new List<SqlScript>() { new SqlScript("NameOfYourScript", @"CREATE TABLE `Values` (
                `Id` INT NOT NULL,
                `Value1` INT NOT NULL,
                `Value2` INT NULL,
                PRIMARY KEY (`Id`)
            ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;") });

            var downgradeScriptProvider = new StaticScriptProvider(new List<SqlScript>() { new SqlScript("NameOfYourScript", "DROP TABLE IF EXISTS `Values`;") });

            var upgradeEngineBuilder = DeployChanges.To
                .MySqlDatabase(_connectionString)
                .WithScripts(upgradeScriptProvider)
                .WithDowngradeTableProvider<MySqlDowngradeEnabledTableJournal>(downgradeScriptProvider, new DefaultDowngradeScriptFinder())
                .LogToConsole();

            var result = upgradeEngineBuilder.BuildWithDowngrade(true).PerformUpgrade();

            //Assert
            Assert.True(result.Successful);

            Dictionary<string, string> executedScriptsAndDowngradeScripts = GetExecutedScriptsFromDatabase(_connectionString);
            var upgradeScripts = upgradeScriptProvider.GetScripts(null);
            var downgradeScripts = downgradeScriptProvider.GetScripts(null);

            Assert.Equal(executedScriptsAndDowngradeScripts.Count, upgradeScripts.Count());

            foreach (var storedDowngradeScript in executedScriptsAndDowngradeScripts.Values.Where(v => !string.IsNullOrEmpty(v)))
            {
                Assert.Contains(downgradeScripts, script => script.Contents.Equals(storedDowngradeScript));
            }
        }

        private Dictionary<string, string> GetExecutedScriptsFromDatabase(string connectionString)
        {
            Dictionary<string, string> executedScriptsAndDowngradeScripts = new Dictionary<string, string>();
            using var conn = new MySqlConnection(connectionString);

            conn.Open();
            using var command = conn.CreateCommand();
            command.CommandText = "SELECT ScriptName, DowngradeScript FROM schemaversions;";

            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                executedScriptsAndDowngradeScripts.Add((string)reader["ScriptName"], reader["DowngradeScript"] is DBNull ? null : reader["DowngradeScript"].ToString());
            }
            reader.Close();
            conn.Close();
            conn.Dispose();

            return executedScriptsAndDowngradeScripts;
        }
    }
}
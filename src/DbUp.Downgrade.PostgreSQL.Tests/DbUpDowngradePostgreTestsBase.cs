using DbUp.Downgrade.Helpers;
using DbUp.Engine;
using DbUp.ScriptProviders;
using Npgsql;

namespace DbUp.Downgrade.PostgreSQL.Tests
{
    public abstract class DbUpDowngradePostgreTestsBase
    {
        public readonly string _connectionString;

        protected DbUpDowngradePostgreTestsBase(string connectionString)
        {
            _connectionString = connectionString;
        }

        [Fact]
        public void StaticScriptProvider_SuccessfullyStoresDowngradeScripts()
        {
            var upgradeScriptProvider = new StaticScriptProvider(new List<SqlScript>() { new SqlScript("NameOfYourScript", @"CREATE TABLE Values (
                Id INTEGER NOT NULL,
                Value1 INTEGER NOT NULL,
                Value2 INTEGER,
                CONSTRAINT PK_Values PRIMARY KEY (Id)
            );") });

            var downgradeScriptProvider = new StaticScriptProvider(new List<SqlScript>() { new SqlScript("NameOfYourScript", "DROP TABLE values;") });

            var upgradeEngineBuilder = DeployChanges.To
                .PostgresqlDatabase(_connectionString)
                .WithScripts(upgradeScriptProvider)
                .WithDowngradeTableProvider<PostgresDowngradeEnabledTableJournal>(downgradeScriptProvider, new DefaultDowngradeScriptFinder())
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
            using var dataSource = NpgsqlDataSource.Create(connectionString);

            using var command = dataSource.CreateCommand("SELECT scriptname, downgradescript FROM schemaversions;");
            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                executedScriptsAndDowngradeScripts.Add((string)reader["ScriptName"], reader["DowngradeScript"] is DBNull ? null : reader["DowngradeScript"].ToString());
            }
            reader.Close();

            return executedScriptsAndDowngradeScripts;
        }
    }
}
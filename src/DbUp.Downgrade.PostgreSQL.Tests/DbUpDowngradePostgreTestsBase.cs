using System.Reflection;
using DbUp.Builder;
using DbUp.Downgrade.Helpers;
using DbUp.Downgrade.Shared.Tests;
using DbUp.Engine;
using DbUp.ScriptProviders;
using Microsoft.Data.SqlClient;
using Npgsql;

namespace DbUp.Downgrade.PostgreSQL.Tests
{
    public abstract class DbUpDowngradePostgreTestsBase : DbUpDowngradeSharedFixtures, IDisposable
    {
        public readonly string _connectionString;

        public DbUpDowngradePostgreTestsBase(string connectionString)
        {
            _connectionString = connectionString;

            EnsureDatabase.For.PostgresqlDatabase(_connectionString);
        }

        public void Dispose()
        {
            var dataSource = NpgsqlDataSource.Create(_connectionString);

            using var command = dataSource.CreateCommand(@"
                DROP TABLE IF EXISTS ""Entry"" CASCADE;
                DROP TABLE IF EXISTS ""Feed"" CASCADE;
                DROP TABLE IF EXISTS ""Setting"" CASCADE;
                DROP TABLE IF EXISTS ""Redirect"" CASCADE;
                DROP TABLE IF EXISTS ""values"" CASCADE;
                DROP TABLE IF EXISTS ""schemaversions"";
            ");

            command.ExecuteNonQuery();
        }

        public override string CreateValuesTableSql => @"
            CREATE TABLE values (
                id INT NOT NULL,
                value1 INT NOT NULL,
                value2 INT NULL,
                CONSTRAINT pk_values PRIMARY KEY (id)
            );";

        public override string DropValuesTableSql => "DROP TABLE IF EXISTS values;";

        public override string CreatePersonsTableSql => @"
            CREATE TABLE Persons (
                PersonID INT,
                LastName VARCHAR(255),
                FirstName VARCHAR(255)
            );";


        public override Dictionary<string, string> GetExecutedScriptsFromDatabase()
        {
            var executedScriptsAndDowngradeScripts = new Dictionary<string, string>();

            var dataSource = NpgsqlDataSource.Create(_connectionString);

            using var command = dataSource.CreateCommand($"SELECT scriptname, downgradescript FROM {PostgresDowngradeEnabledTableJournal.DefaultTable}");

            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                executedScriptsAndDowngradeScripts.Add(
                    reader["scriptname"].ToString(),
                    reader["downgradescript"] is DBNull ? null : reader["downgradescript"].ToString()
                );
            }

            return executedScriptsAndDowngradeScripts;
        }

        public override void AssertColumnExists(string columnName)
        {
            //using (var connection = new NpgsqlConnection(_connectionString))
            //{
            //    var command = new NpgsqlCommand($@"
            //        SELECT COLUMN_NAME
            //        FROM INFORMATION_SCHEMA.COLUMNS
            //        WHERE TABLE_NAME = '{PostgresDowngradeEnabledTableJournal.DefaultTable}'
            //          AND COLUMN_NAME = '{columnName}'
            //          AND TABLE_SCHEMA = '{PostgreSqlTestContainerBase.DatabaseName}'", connection);

            //    try
            //    {
            //        connection.Open();
            //        var result = command.ExecuteScalar();
            //        Assert.NotNull(result);
            //    }
            //    finally
            //    {
            //        connection.Close();
            //    }
            //}
        }

        public override void AssertTableNoLongerExists(string tableName)
        {
            using (var connection = new NpgsqlConnection(_connectionString))
            {
                var command = new NpgsqlCommand($@"
                    SELECT 1
                    FROM INFORMATION_SCHEMA.TABLES
                    WHERE TABLE_NAME = '{tableName}'
                      AND TABLE_SCHEMA = 'public'", connection);

                try
                {
                    connection.Open();
                    var result = command.ExecuteScalar();
                    Assert.Null(result);
                }
                finally
                {
                    connection.Close();
                }
            }
        }

        public override UpgradeEngineBuilder GetUpgradeEngineBuilder(DowngradeScriptsSettings settings)
        {
            return DeployChanges.To
                .PostgresqlDatabase(_connectionString)
                .WithScriptsAndDowngradeScriptsEmbeddedInAssembly<PostgresDowngradeEnabledTableJournal>(Assembly.GetExecutingAssembly(), settings)
                .LogToConsole();
        }

        public override UpgradeEngineBuilder GetUpgradeEngineBuilder(IScriptProvider upgradeScriptProvider, IScriptProvider downgradeScriptProvider, IDowngradeScriptFinder downgradeScriptFinder)
        {
            return DeployChanges.To
                .PostgresqlDatabase(_connectionString)
                .WithScripts(upgradeScriptProvider)
                .WithDowngradeTableProvider<PostgresDowngradeEnabledTableJournal>(downgradeScriptProvider, new DefaultDowngradeScriptFinder())
                .LogToConsole();
        }

        public override Assembly ExecuttingAssembly => Assembly.GetExecutingAssembly();
    }
}
using System.Reflection;
using DbUp.Builder;
using DbUp.Downgrade.Helpers;
using DbUp.Downgrade.Shared.Tests;
using DbUp.Engine;
using MySqlConnector;

namespace DbUp.Downgrade.MySql.Tests
{
    public abstract class DbUpDowngradeMySqlTestsBase : DbUpDowngradeSharedFixtures, IDisposable
    {
        public readonly string _connectionString;

        public DbUpDowngradeMySqlTestsBase(string connectionString)
        {
            _connectionString = connectionString;
        }

        public void Dispose()
        {
            using var connection = new MySqlConnection(_connectionString);

            using var command = new MySqlCommand(@"
                DROP TABLE IF EXISTS `Entry`;
                DROP TABLE IF EXISTS `Feed`;
                DROP TABLE IF EXISTS `Setting`;
                DROP TABLE IF EXISTS `Redirect`;
                DROP TABLE IF EXISTS `Values`;
                DROP TABLE IF EXISTS `schemaversions`;", connection);

            try
            {
                connection.Open();
                command.ExecuteNonQuery();
            }
            finally
            {
                connection.Close();
            }
        }

        public override string CreateValuesTableSql => @"
            CREATE TABLE `Values` (
                `Id` INT NOT NULL,
                `Value1` INT NOT NULL,
                `Value2` INT NULL,
                PRIMARY KEY (`Id`)
            ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;";

        public override string DropValuesTableSql => "DROP TABLE IF EXISTS `Values`;";

        public override string CreatePersonsTableSql => @"
            CREATE TABLE `Persons` (
                `PersonID` INT,
                `LastName` VARCHAR(255),
                `FirstName` VARCHAR(255)
            );";

        public override Dictionary<string, string> GetExecutedScriptsFromDatabase()
        {
            var executedScriptsAndDowngradeScripts = new Dictionary<string, string>();

            using (var connection = new MySqlConnection(_connectionString))
            {
                var command = new MySqlCommand($"SELECT `scriptname`, `downgradescript` FROM {MySqlDowngradeEnabledTableJournal.DefaultTable}", connection);

                try
                {
                    connection.Open();
                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            executedScriptsAndDowngradeScripts.Add(
                                reader["scriptname"].ToString(),
                                reader["downgradescript"] is DBNull ? null : reader["downgradescript"].ToString()
                            );
                        }
                    }
                }
                finally
                {
                    connection.Close();
                }
            }

            return executedScriptsAndDowngradeScripts;
        }

        public override void AssertColumnExists(string columnName)
        {
            using (var connection = new MySqlConnection(_connectionString))
            {
                var command = new MySqlCommand($@"
                    SELECT 1 
                    FROM INFORMATION_SCHEMA.COLUMNS 
                    WHERE TABLE_NAME = '{MySqlDowngradeEnabledTableJournal.DefaultTable}' 
                        AND COLUMN_NAME = '{columnName}'
                        AND TABLE_SCHEMA = DATABASE()", connection);

                try
                {
                    connection.Open();
                    var result = command.ExecuteScalar();
                    Assert.NotNull(result);
                }
                finally
                {
                    connection.Close();
                }
            }
        }

        public override void AssertTableNoLongerExists(string tableName)
        {
            using (var connection = new MySqlConnection(_connectionString))
            {
                var command = new MySqlCommand($@"
                    SELECT 1 
                    FROM INFORMATION_SCHEMA.TABLES 
                    WHERE TABLE_NAME = '{tableName}' 
                        AND TABLE_SCHEMA = DATABASE()", connection);

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
                .MySqlDatabase(_connectionString)
                .WithScriptsAndDowngradeScriptsEmbeddedInAssembly<MySqlDowngradeEnabledTableJournal>(Assembly.GetExecutingAssembly(), settings)
                .LogToConsole();
        }

        public override UpgradeEngineBuilder GetUpgradeEngineBuilder(IScriptProvider upgradeScriptProvider, IScriptProvider downgradeScriptProvider, IDowngradeScriptFinder downgradeScriptFinder)
        {
            return DeployChanges.To
                .MySqlDatabase(_connectionString)
                .WithScripts(upgradeScriptProvider)
                .WithDowngradeTableProvider<MySqlDowngradeEnabledTableJournal>(downgradeScriptProvider, new DefaultDowngradeScriptFinder())
                .LogToConsole();
        }

        public override Assembly ExecuttingAssembly => Assembly.GetExecutingAssembly();
    }
}
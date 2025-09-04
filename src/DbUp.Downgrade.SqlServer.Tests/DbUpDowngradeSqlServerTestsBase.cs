using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using DbUp.Builder;
using DbUp.Downgrade.Helpers;
using DbUp.Downgrade.Shared.Tests;
using DbUp.Engine;
using DbUp.ScriptProviders;
using Microsoft.Data.SqlClient;
using Xunit;

namespace DbUp.Downgrade.SqlServer.Tests
{
    public abstract class DbUpDowngradeSqlServerTestsBase : DbUpDowngradeSharedFixtures, IDisposable
    {
        public readonly string _connectionString;

        public DbUpDowngradeSqlServerTestsBase(string connectionString)
        {
            _connectionString = connectionString;

            EnsureDatabase.For.SqlDatabase(_connectionString);
        }

        public void Dispose()
        {
            DropDatabase.For.SqlDatabase(_connectionString);
        }

        public override string CreateValuesTableSql => @"CREATE TABLE [dbo].[Values](
                     [Id] [int] NOT NULL,
                     [Value1] [int] NOT NULL,
                     [Value2] [int] NULL,
                     CONSTRAINT [PK_Values] PRIMARY KEY CLUSTERED 
                    (
                     [Id] ASC
                    )WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
                    ) ON [PRIMARY]";

        public override string DropValuesTableSql => "DROP TABLE [dbo].[Values]";

        public override string CreatePersonsTableSql => "CREATE TABLE Persons(PersonID int, LastName varchar(255), FirstName varchar(255));";

        public override Dictionary<string, string> GetExecutedScriptsFromDatabase()
        {
            Dictionary<string, string> executedScriptsAndDowngradeScripts = new Dictionary<string, string>();

            using (var connection = new SqlConnection(_connectionString))
            {
                SqlCommand command = new SqlCommand("SELECT [ScriptName] ,[DowngradeScript] FROM SchemaVersions", connection);

                try
                {
                    connection.Open();
                    using (SqlDataReader reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            executedScriptsAndDowngradeScripts.Add((string)reader["ScriptName"], reader["DowngradeScript"] is DBNull ? null : reader["DowngradeScript"].ToString());
                        }
                        reader.Close();
                    }
                }
                catch
                {
                    throw;
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
            using (var connection = new SqlConnection(_connectionString))
            {
                SqlCommand command = new SqlCommand($"SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'SchemaVersions' AND COLUMN_NAME = '{columnName}'", connection);

                try
                {
                    connection.Open();
                    var executeScalar = command.ExecuteScalar();
                    Assert.NotNull(executeScalar); // If 1 the DowngradeScript cloumn persists in the DB
                }
                catch
                {
                    throw;
                }
                finally
                {
                    connection.Close();
                }
            }
        }

        public override void AssertTableNoLongerExists(string tableName)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                SqlCommand command = new SqlCommand($"select 1 from INFORMATION_SCHEMA.TABLES where TABLE_NAME = '{tableName}'", connection);

                try
                {
                    connection.Open();
                    var executeScalar = command.ExecuteScalar();
                    Assert.Null(executeScalar); // If null table no longer exists
                }
                catch
                {
                    throw;
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
                .SqlDatabase(_connectionString)
                .WithScriptsAndDowngradeScriptsEmbeddedInAssembly<SqlDowngradeEnabledTableJournal>(Assembly.GetExecutingAssembly(), settings)
                .LogToConsole();
        }

        public override UpgradeEngineBuilder GetUpgradeEngineBuilder(IScriptProvider upgradeScriptProvider, IScriptProvider downgradeScriptProvider, IDowngradeScriptFinder downgradeScriptFinder)
        {
            return DeployChanges.To
                .SqlDatabase(_connectionString)
                .WithScripts(upgradeScriptProvider)
                .WithDowngradeTableProvider<SqlDowngradeEnabledTableJournal>(downgradeScriptProvider, new DefaultDowngradeScriptFinder())
                .LogToConsole();
        }

        public override Assembly ExecuttingAssembly => Assembly.GetExecutingAssembly();
    }
}
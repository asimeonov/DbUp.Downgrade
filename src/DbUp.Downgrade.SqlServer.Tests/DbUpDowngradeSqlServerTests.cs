using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Reflection;
using DbUp.Downgrade.Helpers;
using DbUp.Engine;
using DbUp.ScriptProviders;
using NUnit.Framework;

namespace DbUp.Downgrade.SqlServer.Tests
{
    public class DbUpDowngradeSqlServerTests
    {
        string connectionString = $"Data Source=.\\SqlExpress;Initial Catalog=DbUpDowngradeTests;Integrated Security=True;Pooling=False";

        [SetUp]
        public void Setup()
        {
            EnsureDatabase.For.SqlDatabase(connectionString);
        }

        [Test]
        public void WithScriptsAndDowngradeScriptsEmbeddedInAssembly_RunsFromSuffix_SuccessfullyStoresDowngradeScripts()
        {
            // Setup
            string suffix = "_downgrade";

            Func<string, bool> scriptsFilter = s => s.EndsWith(".sql", StringComparison.OrdinalIgnoreCase) && !s.EndsWith($"{suffix}.sql", StringComparison.OrdinalIgnoreCase) && s.Contains("SuffixUpAndDownScripts");
            Func<string, bool> downgradeScriptsFilter = s => s.EndsWith($"{suffix}.sql", StringComparison.OrdinalIgnoreCase) && s.Contains("SuffixUpAndDownScripts");

            DowngradeScriptsSettings settings = new DowngradeScriptsSettings(scriptsFilter, downgradeScriptsFilter, 
                new KeyValuePair<DowngradeScriptsSettingsMode, string>(DowngradeScriptsSettingsMode.Suffix, suffix));

            var upgradeEngineBuilder = DeployChanges.To
                .SqlDatabase(connectionString)
                .WithScriptsAndDowngradeScriptsEmbeddedInAssembly<SqlDowngradeEnabledTableJournal>(Assembly.GetExecutingAssembly(), settings)
                .LogToNowhere();

            // Act
            var result = upgradeEngineBuilder.BuildWithDowngrade(true).PerformUpgrade();

            //Assert
            Assert.AreEqual(true, result.Successful);

            Dictionary<string, string> executedScriptsAndDowngradeScripts = GetExecutedScriptsFromDatabase(connectionString);
            var upgradeScripts = new EmbeddedScriptProvider(Assembly.GetExecutingAssembly(), settings.ScriptsFilter).GetScripts(null);
            var downgradeScripts = new EmbeddedScriptProvider(Assembly.GetExecutingAssembly(), settings.DowngradeScriptsFilter).GetScripts(null);

            Assert.AreEqual(executedScriptsAndDowngradeScripts.Count, upgradeScripts.Count());

            foreach (var storedDowngradeScript in executedScriptsAndDowngradeScripts.Values.Where(v => !string.IsNullOrEmpty(v)))
            {
                Assert.IsTrue(downgradeScripts.Any(script => script.Contents.Equals(storedDowngradeScript)));
            }
        }

        [Test]
        public void WithScriptsAndDowngradeScriptsEmbeddedInAssembly_RunsFromFolder_SuccessfullyStoresDowngradeScripts()
        {
            // Setup
            string folderName = "DowngradeScripts";

            Func<string, bool> scriptsFilter = s => s.EndsWith(".sql", StringComparison.OrdinalIgnoreCase) && !s.Contains($".{folderName}.") && s.Contains("FolderUpAndDownScrips");
            Func<string, bool> downgradeScriptsFilter = s => s.Contains($".{folderName}.") && s.Contains("FolderUpAndDownScrips");

            DowngradeScriptsSettings settings = new DowngradeScriptsSettings(scriptsFilter, downgradeScriptsFilter,
                new KeyValuePair<DowngradeScriptsSettingsMode, string>(DowngradeScriptsSettingsMode.Folder, folderName));

            var upgradeEngineBuilder = DeployChanges.To
                .SqlDatabase(connectionString)
                .WithScriptsAndDowngradeScriptsEmbeddedInAssembly<SqlDowngradeEnabledTableJournal>(Assembly.GetExecutingAssembly(), settings)
                .LogToNowhere();

            // Act
            var result = upgradeEngineBuilder.BuildWithDowngrade(true).PerformUpgrade();

            //Assert
            Assert.AreEqual(true, result.Successful);

            Dictionary<string, string> executedScriptsAndDowngradeScripts = GetExecutedScriptsFromDatabase(connectionString);
            var upgradeScripts = new EmbeddedScriptProvider(Assembly.GetExecutingAssembly(), settings.ScriptsFilter).GetScripts(null);
            var downgradeScripts = new EmbeddedScriptProvider(Assembly.GetExecutingAssembly(), settings.DowngradeScriptsFilter).GetScripts(null);

            Assert.AreEqual(executedScriptsAndDowngradeScripts.Count, upgradeScripts.Count());

            foreach (var storedDowngradeScript in executedScriptsAndDowngradeScripts.Values.Where(v => !string.IsNullOrEmpty(v)))
            {
                Assert.IsTrue(downgradeScripts.Any(script => script.Contents.Equals(storedDowngradeScript)));
            }
        }

        [Test]
        public void FileSystemScriptProvider_SuccessfullyStoresDowngradeScripts()
        {
            var upgradeScriptProvider = new FileSystemScriptProvider("FileSystemScripts\\Up", new FileSystemScriptOptions() { IncludeSubDirectories = true });
            var downgradeScriptProvider = new FileSystemScriptProvider("FileSystemScripts\\Down", new FileSystemScriptOptions() { IncludeSubDirectories = true });

            var upgradeEngineBuilder = DeployChanges.To
                .SqlDatabase(connectionString)
                .WithScripts(upgradeScriptProvider)
                .WithDowngradeTableProvider<SqlDowngradeEnabledTableJournal>(downgradeScriptProvider, new DefaultDowngradeScriptFinder())
                .LogToNowhere();

            var result = upgradeEngineBuilder.BuildWithDowngrade(true).PerformUpgrade();

            //Assert
            Assert.AreEqual(true, result.Successful);

            Dictionary<string, string> executedScriptsAndDowngradeScripts = GetExecutedScriptsFromDatabase(connectionString);
            var upgradeScripts = upgradeScriptProvider.GetScripts(null);
            var downgradeScripts = downgradeScriptProvider.GetScripts(null);

            Assert.AreEqual(executedScriptsAndDowngradeScripts.Count, upgradeScripts.Count());

            foreach (var storedDowngradeScript in executedScriptsAndDowngradeScripts.Values.Where(v => !string.IsNullOrEmpty(v)))
            {
                Assert.IsTrue(downgradeScripts.Any(script => script.Contents.Equals(storedDowngradeScript)));
            }
        }

        [Test]
        public void StaticScriptProvider_SuccessfullyStoresDowngradeScripts()
        {
            var upgradeScriptProvider = new StaticScriptProvider(new List<SqlScript>() { new SqlScript("NameOfYourScript", @"CREATE TABLE [dbo].[Values](
                     [Id] [int] NOT NULL,
                     [Value1] [int] NOT NULL,
                     [Value2] [int] NULL,
                     CONSTRAINT [PK_Values] PRIMARY KEY CLUSTERED 
                    (
                     [Id] ASC
                    )WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
                    ) ON [PRIMARY]") });

            var downgradeScriptProvider = new StaticScriptProvider(new List<SqlScript>() { new SqlScript("NameOfYourScript", "DROP TABLE [dbo].[Values]") });

            var upgradeEngineBuilder = DeployChanges.To
                .SqlDatabase(connectionString)
                .WithScripts(upgradeScriptProvider)
                .WithDowngradeTableProvider<SqlDowngradeEnabledTableJournal>(downgradeScriptProvider, new DefaultDowngradeScriptFinder())
                .LogToNowhere();

            var result = upgradeEngineBuilder.BuildWithDowngrade(true).PerformUpgrade();

            //Assert
            Assert.AreEqual(true, result.Successful);

            Dictionary<string, string> executedScriptsAndDowngradeScripts = GetExecutedScriptsFromDatabase(connectionString);
            var upgradeScripts = upgradeScriptProvider.GetScripts(null);
            var downgradeScripts = downgradeScriptProvider.GetScripts(null);

            Assert.AreEqual(executedScriptsAndDowngradeScripts.Count, upgradeScripts.Count());

            foreach (var storedDowngradeScript in executedScriptsAndDowngradeScripts.Values.Where(v => !string.IsNullOrEmpty(v)))
            {
                Assert.IsTrue(downgradeScripts.Any(script => script.Contents.Equals(storedDowngradeScript)));
            }
        }

        [Test]
        public void WithScriptsAndDowngradeScriptsEmbeddedInAssembly_RunsFromSuffix_SuccessfullyRevertIfOlderVersionIsDeployed()
        {
            // Setup
            string suffix = "_downgrade";

            Func<string, bool> scriptsFilter = s => s.EndsWith(".sql", StringComparison.OrdinalIgnoreCase) && !s.EndsWith($"{suffix}.sql", StringComparison.OrdinalIgnoreCase) && s.Contains("SuffixUpAndDownScripts");
            Func<string, bool> downgradeScriptsFilter = s => s.EndsWith($"{suffix}.sql", StringComparison.OrdinalIgnoreCase) && s.Contains("SuffixUpAndDownScripts");

            DowngradeScriptsSettings settings = new DowngradeScriptsSettings(scriptsFilter, downgradeScriptsFilter,
                new KeyValuePair<DowngradeScriptsSettingsMode, string>(DowngradeScriptsSettingsMode.Suffix, suffix));

            var upgradeEngineBuilder = DeployChanges.To
                .SqlDatabase(connectionString)
                .WithScriptsAndDowngradeScriptsEmbeddedInAssembly<SqlDowngradeEnabledTableJournal>(Assembly.GetExecutingAssembly(), settings)
                .LogToNowhere();

            // Act
            var result = upgradeEngineBuilder.BuildWithDowngrade(true).PerformUpgrade();

            //Assert
            Assert.AreEqual(true, result.Successful);

            // Perform revert of latest script
            scriptsFilter = s => s.EndsWith(".sql", StringComparison.OrdinalIgnoreCase) && !s.EndsWith($"{suffix}.sql", StringComparison.OrdinalIgnoreCase) 
                && s.Contains("SuffixUpAndDownScripts") && !s.Contains("Script0004 - Redirects");
            downgradeScriptsFilter = s => s.EndsWith($"{suffix}.sql", StringComparison.OrdinalIgnoreCase) && s.Contains("SuffixUpAndDownScripts");

            settings = new DowngradeScriptsSettings(scriptsFilter, downgradeScriptsFilter,
                new KeyValuePair<DowngradeScriptsSettingsMode, string>(DowngradeScriptsSettingsMode.Suffix, suffix));

            upgradeEngineBuilder = DeployChanges.To
                .SqlDatabase(connectionString)
                .WithScriptsAndDowngradeScriptsEmbeddedInAssembly<SqlDowngradeEnabledTableJournal>(Assembly.GetExecutingAssembly(), settings)
                .LogToNowhere();

            // Act
            result = upgradeEngineBuilder.BuildWithDowngrade(true).PerformUpgrade();

            //Assert
            Assert.AreEqual(true, result.Successful);

            Dictionary<string, string> executedScriptsAndDowngradeScripts = GetExecutedScriptsFromDatabase(connectionString);
            var upgradeScripts = new EmbeddedScriptProvider(Assembly.GetExecutingAssembly(), settings.ScriptsFilter).GetScripts(null);

            Assert.AreEqual(executedScriptsAndDowngradeScripts.Count, upgradeScripts.Count());

            using (var connection = new SqlConnection(connectionString))
            {
                SqlCommand command = new SqlCommand("select 1 from INFORMATION_SCHEMA.TABLES where TABLE_NAME = 'Redirect'", connection);

                try
                {
                    connection.Open();
                    var executeScalar = command.ExecuteScalar();
                    Assert.IsNull(executeScalar); // If null table no longer exists
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

        [Test]
        public void ExistingProjects_DowngradeScriptColumnDontExists_AddsDowngradeScriptColumn()
        {
            var upgradeEngineBuilder = DeployChanges.To
                .SqlDatabase(connectionString)
                .WithScript(new SqlScript("CreatePersonsTable", "CREATE TABLE Persons(PersonID int, LastName varchar(255), FirstName varchar(255));"))
                .LogToNowhere();

            upgradeEngineBuilder.Build().PerformUpgrade();

            var upgradeScriptProvider = new StaticScriptProvider(new List<SqlScript>() { new SqlScript("NameOfYourScript", @"CREATE TABLE [dbo].[Values](
                     [Id] [int] NOT NULL,
                     [Value1] [int] NOT NULL,
                     [Value2] [int] NULL,
                     CONSTRAINT [PK_Values] PRIMARY KEY CLUSTERED 
                    (
                     [Id] ASC
                    )WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
                    ) ON [PRIMARY]") });

            var downgradeScriptProvider = new StaticScriptProvider(new List<SqlScript>() { new SqlScript("NameOfYourScript", "DROP TABLE [dbo].[Values]") });

            var downgradeEngineBuilder = DeployChanges.To
                .SqlDatabase(connectionString)
                .WithScripts(upgradeScriptProvider)
                .WithDowngradeTableProvider<SqlDowngradeEnabledTableJournal>(downgradeScriptProvider, new DefaultDowngradeScriptFinder())
                .LogToNowhere();

            var result = downgradeEngineBuilder.BuildWithDowngrade(true).PerformUpgrade();

            //Assert
            Assert.AreEqual(true, result.Successful);

            using (var connection = new SqlConnection(connectionString))
            {
                SqlCommand command = new SqlCommand("SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'SchemaVersions' AND COLUMN_NAME = 'DowngradeScript'", connection);

                try
                {
                    connection.Open();
                    var executeScalar = command.ExecuteScalar();
                    Assert.IsNotNull(executeScalar); // If 1 the DowngradeScript cloumn persists in the DB
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

        private Dictionary<string, string> GetExecutedScriptsFromDatabase(string connectionString)
        {
            Dictionary<string, string> executedScriptsAndDowngradeScripts = new Dictionary<string, string>();

            using (var connection = new SqlConnection(connectionString))
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

        [TearDown]
        public void TearDown()
        {
            DropDatabase.For.SqlDatabase(connectionString);
        }
    }
}
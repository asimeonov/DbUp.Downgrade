using System.Reflection;
using DbUp.Builder;
using DbUp.Downgrade.Helpers;
using DbUp.Engine;
using DbUp.ScriptProviders;

namespace DbUp.Downgrade.Shared.Tests
{
    public abstract class DbUpDowngradeSharedFixtures
    {
        public abstract UpgradeEngineBuilder GetUpgradeEngineBuilder(DowngradeScriptsSettings settings);
        public abstract UpgradeEngineBuilder GetUpgradeEngineBuilder(IScriptProvider upgradeScriptProvider, IScriptProvider downgradeScriptProvider1, IDowngradeScriptFinder downgradeScriptFinder);
        public abstract Dictionary<string, string> GetExecutedScriptsFromDatabase();
        public abstract void AssertColumnExists(string columnName);
        public abstract void AssertTableNoLongerExists(string tableName);
        public abstract string CreateValuesTableSql { get; }
        public abstract string CreatePersonsTableSql { get; }
        public abstract string DropValuesTableSql { get; }
        public abstract Assembly ExecuttingAssembly { get; }

        [Fact]
        public void WithScriptsAndDowngradeScriptsEmbeddedInAssembly_RunsFromSuffix_SuccessfullyStoresDowngradeScripts()
        {
            // Setup
            string suffix = "_downgrade";

            Func<string, bool> scriptsFilter = s => s.EndsWith(".sql", StringComparison.OrdinalIgnoreCase) && !s.EndsWith($"{suffix}.sql", StringComparison.OrdinalIgnoreCase) && s.Contains("SuffixUpAndDownScripts");
            Func<string, bool> downgradeScriptsFilter = s => s.EndsWith($"{suffix}.sql", StringComparison.OrdinalIgnoreCase) && s.Contains("SuffixUpAndDownScripts");

            DowngradeScriptsSettings settings = new DowngradeScriptsSettings(scriptsFilter, downgradeScriptsFilter,
                new KeyValuePair<DowngradeScriptsSettingsMode, string>(DowngradeScriptsSettingsMode.Suffix, suffix));

            var upgradeEngineBuilder = GetUpgradeEngineBuilder(settings);

            // Act
            var result = upgradeEngineBuilder.BuildWithDowngrade(true).PerformUpgrade();

            //Assert
            Assert.True(result.Successful, result.Error?.Message);

            Dictionary<string, string> executedScriptsAndDowngradeScripts = GetExecutedScriptsFromDatabase();
            var upgradeScripts = new EmbeddedScriptProvider(ExecuttingAssembly, settings.ScriptsFilter).GetScripts(null);
            var downgradeScripts = new EmbeddedScriptProvider(ExecuttingAssembly, settings.DowngradeScriptsFilter).GetScripts(null);

            Assert.Equal(executedScriptsAndDowngradeScripts.Count, upgradeScripts.Count());

            foreach (var storedDowngradeScript in executedScriptsAndDowngradeScripts.Values.Where(v => !string.IsNullOrEmpty(v)))
            {
                Assert.Contains(downgradeScripts, script => script.Contents.Equals(storedDowngradeScript));
            }
        }

        [Fact]
        public void WithScriptsAndDowngradeScriptsEmbeddedInAssembly_RunsFromFolder_SuccessfullyStoresDowngradeScripts()
        {
            // Setup
            string folderName = "DowngradeScripts";

            Func<string, bool> scriptsFilter = s => s.EndsWith(".sql", StringComparison.OrdinalIgnoreCase) && !s.Contains($".{folderName}.") && s.Contains("FolderUpAndDownScrips");
            Func<string, bool> downgradeScriptsFilter = s => s.Contains($".{folderName}.") && s.Contains("FolderUpAndDownScrips");

            DowngradeScriptsSettings settings = new DowngradeScriptsSettings(scriptsFilter, downgradeScriptsFilter,
                new KeyValuePair<DowngradeScriptsSettingsMode, string>(DowngradeScriptsSettingsMode.Folder, folderName));

            var upgradeEngineBuilder = GetUpgradeEngineBuilder(settings);

            // Act
            var result = upgradeEngineBuilder.BuildWithDowngrade(true).PerformUpgrade();

            //Assert
            Assert.True(result.Successful, result.Error?.Message);

            Dictionary<string, string> executedScriptsAndDowngradeScripts = GetExecutedScriptsFromDatabase();
            var upgradeScripts = new EmbeddedScriptProvider(ExecuttingAssembly, settings.ScriptsFilter).GetScripts(null);
            var downgradeScripts = new EmbeddedScriptProvider(ExecuttingAssembly, settings.DowngradeScriptsFilter).GetScripts(null);

            Assert.Equal(executedScriptsAndDowngradeScripts.Count, upgradeScripts.Count());

            foreach (var storedDowngradeScript in executedScriptsAndDowngradeScripts.Values.Where(v => !string.IsNullOrEmpty(v)))
            {
                Assert.Contains(downgradeScripts, script => script.Contents.Equals(storedDowngradeScript));
            }
        }

        [Fact]
        public void FileSystemScriptProvider_SuccessfullyStoresDowngradeScripts()
        {
            var upgradeScriptProvider = new FileSystemScriptProvider($"FileSystemScripts{Path.DirectorySeparatorChar}Up", new FileSystemScriptOptions() { IncludeSubDirectories = true });
            var downgradeScriptProvider = new FileSystemScriptProvider($"FileSystemScripts{Path.DirectorySeparatorChar}Down", new FileSystemScriptOptions() { IncludeSubDirectories = true });

            var upgradeEngineBuilder = GetUpgradeEngineBuilder(upgradeScriptProvider, downgradeScriptProvider, new DefaultDowngradeScriptFinder());

            var result = upgradeEngineBuilder.BuildWithDowngrade(true).PerformUpgrade();

            //Assert
            Assert.True(result.Successful, result.Error?.Message);

            Dictionary<string, string> executedScriptsAndDowngradeScripts = GetExecutedScriptsFromDatabase();
            var upgradeScripts = upgradeScriptProvider.GetScripts(null);
            var downgradeScripts = downgradeScriptProvider.GetScripts(null);

            Assert.Equal(executedScriptsAndDowngradeScripts.Count, upgradeScripts.Count());

            foreach (var storedDowngradeScript in executedScriptsAndDowngradeScripts.Values.Where(v => !string.IsNullOrEmpty(v)))
            {
                Assert.Contains(downgradeScripts, script => script.Contents.Equals(storedDowngradeScript));
            }
        }

        [Fact]
        public void StaticScriptProvider_SuccessfullyStoresDowngradeScripts()
        {
            var upgradeScriptProvider = new StaticScriptProvider(new List<SqlScript>() { new SqlScript("NameOfYourScript", CreateValuesTableSql) });

            var downgradeScriptProvider = new StaticScriptProvider(new List<SqlScript>() { new SqlScript("NameOfYourScript", DropValuesTableSql) });

            var upgradeEngineBuilder = GetUpgradeEngineBuilder(upgradeScriptProvider, downgradeScriptProvider, new DefaultDowngradeScriptFinder());

            var result = upgradeEngineBuilder.BuildWithDowngrade(true).PerformUpgrade();

            //Assert
            Assert.True(result.Successful, result.Error?.Message);

            Dictionary<string, string> executedScriptsAndDowngradeScripts = GetExecutedScriptsFromDatabase();
            var upgradeScripts = upgradeScriptProvider.GetScripts(null);
            var downgradeScripts = downgradeScriptProvider.GetScripts(null);

            Assert.Equal(executedScriptsAndDowngradeScripts.Count, upgradeScripts.Count());

            foreach (var storedDowngradeScript in executedScriptsAndDowngradeScripts.Values.Where(v => !string.IsNullOrEmpty(v)))
            {
                Assert.Contains(downgradeScripts, script => script.Contents.Equals(storedDowngradeScript));
            }
        }

        [Fact]
        public void WithStaticScriptProvider_SuccessfullyRevertPassedScriptByName()
        {
            // Arrange
            var upgradeScriptProvider = new StaticScriptProvider(new List<SqlScript>() { new SqlScript("NameOfYourScript", CreateValuesTableSql) });

            var downgradeScriptProvider = new StaticScriptProvider(new List<SqlScript>() { new SqlScript("NameOfYourScript", DropValuesTableSql) });

            var downgradeEnabledUpgradeEngine = GetUpgradeEngineBuilder(upgradeScriptProvider, downgradeScriptProvider, new DefaultDowngradeScriptFinder()).BuildWithDowngrade(false);

            // Act
            // Assert
            var result = downgradeEnabledUpgradeEngine.PerformUpgrade();

            Assert.True(result.Successful, result.Error?.Message);

            result = downgradeEnabledUpgradeEngine.PerformDowngradeForScripts(["NameOfYourScript"]);

            Assert.True(result.Successful, result.Error?.Message);

            AssertTableNoLongerExists("values");
        }

        [Fact]
        public void WithScriptsAndDowngradeScriptsEmbeddedInAssembly_RunsFromSuffix_SuccessfullyRevertIfOlderVersionIsDeployed()
        {
            // Arrange
            string suffix = "_downgrade";

            Func<string, bool> scriptsFilter = s => s.EndsWith(".sql", StringComparison.OrdinalIgnoreCase) && !s.EndsWith($"{suffix}.sql", StringComparison.OrdinalIgnoreCase) && s.Contains("SuffixUpAndDownScripts");
            Func<string, bool> downgradeScriptsFilter = s => s.EndsWith($"{suffix}.sql", StringComparison.OrdinalIgnoreCase) && s.Contains("SuffixUpAndDownScripts");

            DowngradeScriptsSettings settings = new DowngradeScriptsSettings(scriptsFilter, downgradeScriptsFilter,
                new KeyValuePair<DowngradeScriptsSettingsMode, string>(DowngradeScriptsSettingsMode.Suffix, suffix));

            var upgradeEngineBuilder = GetUpgradeEngineBuilder(settings);

            // Act
            var result = upgradeEngineBuilder.BuildWithDowngrade(true).PerformUpgrade();

            //Assert
            Assert.True(result.Successful, result.Error?.Message);

            // Perform revert of latest script
            scriptsFilter = s => s.EndsWith(".sql", StringComparison.OrdinalIgnoreCase) && !s.EndsWith($"{suffix}.sql", StringComparison.OrdinalIgnoreCase)
                && s.Contains("SuffixUpAndDownScripts") && !s.Contains("Script0004 - Redirects");
            downgradeScriptsFilter = s => s.EndsWith($"{suffix}.sql", StringComparison.OrdinalIgnoreCase) && s.Contains("SuffixUpAndDownScripts");

            settings = new DowngradeScriptsSettings(scriptsFilter, downgradeScriptsFilter,
                new KeyValuePair<DowngradeScriptsSettingsMode, string>(DowngradeScriptsSettingsMode.Suffix, suffix));

            upgradeEngineBuilder = GetUpgradeEngineBuilder(settings);

            // Act
            result = upgradeEngineBuilder.BuildWithDowngrade(true).PerformUpgrade();

            //Assert
            Assert.True(result.Successful, result.Error?.Message);

            Dictionary<string, string> executedScriptsAndDowngradeScripts = GetExecutedScriptsFromDatabase();
            var upgradeScripts = new EmbeddedScriptProvider(ExecuttingAssembly, settings.ScriptsFilter).GetScripts(null);

            Assert.Equal(executedScriptsAndDowngradeScripts.Count, upgradeScripts.Count());
            
            AssertTableNoLongerExists("redirect");
        }

        [Fact]
        public void ExistingProjects_DowngradeScriptColumnDontExists_AddsDowngradeScriptColumn()
        {
            var upgradeScriptProvider = new StaticScriptProvider(new List<SqlScript>() { new SqlScript("NameOfYourScript", CreateValuesTableSql) });

            var downgradeScriptProvider = new StaticScriptProvider(new List<SqlScript>() { new SqlScript("NameOfYourScript", DropValuesTableSql) });

            var upgradeEngineBuilder = GetUpgradeEngineBuilder(upgradeScriptProvider, downgradeScriptProvider, new DefaultDowngradeScriptFinder())
                .WithScript(new SqlScript("CreatePersonsTable", CreatePersonsTableSql));

            upgradeEngineBuilder.Build().PerformUpgrade();

            var downgradeEngineBuilder = GetUpgradeEngineBuilder(upgradeScriptProvider,downgradeScriptProvider, new DefaultDowngradeScriptFinder());

            var result = downgradeEngineBuilder.BuildWithDowngrade(true).PerformUpgrade();

            //Assert
            Assert.True(result.Successful, result.Error?.Message);

            AssertColumnExists("downgradescript");
        }
    }
}

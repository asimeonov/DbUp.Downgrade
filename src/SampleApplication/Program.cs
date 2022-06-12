using DbUp;
using DbUp.Downgrade;
using DbUp.Downgrade.Helpers;
using DbUp.Engine;
using DbUp.ScriptProviders;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace SampleApplication
{
    public static class Program
    {
        const string SampleApplicationAssemblyPath = "SampleApplication\\bin\\Debug\\net6.0\\SampleApplication.dll";
        const string SampleWithSuffixV4AssemblyPath = "Samples\\FromSufix\\v4\\bin\\Debug\\net6.0\\SampleScripts.dll";
        const string SampleWithSuffixV5AssemblyPath = "Samples\\FromSufix\\v5\\bin\\Debug\\net6.0\\SampleScripts.dll";
        const string SampleWithFolderV4AssemblyPath = "Samples\\FromFolder\\v4\\bin\\Debug\\net6.0\\SampleScripts.dll";
        const string SampleWithFolderV5AssemblyPath = "Samples\\FromFolder\\v5\\bin\\Debug\\net6.0\\SampleScripts.dll";

        public static void Main(string[] args)
        {
            string instanceName = @".";

            var connectionString = $"Data Source={instanceName};Initial Catalog=SampleApplication;Integrated Security=True;Pooling=False";

            FromSufixSampleDeplyingTwoVersionsAndRevertingSecondDeployOfTheSameApplication(connectionString);

            FromFolderSampleDeplyingTwoVersionsAndRevertingSecondDeployOfTheSameApplication(connectionString);

            ManualRevertOfScriptFromCode(connectionString);

            DropDatabase.For.SqlDatabase(connectionString);

            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine();
            Console.WriteLine("Press any key to continue");
            Console.ReadKey();
        }

        private static void ManualRevertOfScriptFromCode(string connectionString)
        {
            DropDatabase.For.SqlDatabase(connectionString);

            EnsureDatabase.For.SqlDatabase(connectionString);

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
                .LogToConsole();

            var upgrader = upgradeEngineBuilder.BuildWithDowngrade(true);

            var result = upgrader.PerformUpgrade();
            Display(result);

            result = upgrader.PerformDowngradeForScripts(new[] { "NameOfYourScript" });
            Display(result);
        }

        private static void FromFolderSampleDeplyingTwoVersionsAndRevertingSecondDeployOfTheSameApplication(string connectionString)
        {
            DropDatabase.For.SqlDatabase(connectionString);

            EnsureDatabase.For.SqlDatabase(connectionString);

            // Update the DB up to version 004 of the scripts
            UpdateDatabaseFromSampleAssembly(SampleWithFolderV4AssemblyPath, connectionString);
            // Now update to version with scripts up to 005. Theoretically this is same assembly with just another script added
            UpdateDatabaseFromSampleAssembly(SampleWithFolderV5AssemblyPath, connectionString);
            // Opps something goes wrong with the application when we deploy 005, revert to assembly with version 004
            UpdateDatabaseFromSampleAssembly(SampleWithFolderV4AssemblyPath, connectionString);
        }

        private static void FromSufixSampleDeplyingTwoVersionsAndRevertingSecondDeployOfTheSameApplication(string connectionString)
        {
            DropDatabase.For.SqlDatabase(connectionString);

            EnsureDatabase.For.SqlDatabase(connectionString);

            // Update the DB up to version 004 of the scripts
            UpdateDatabaseFromSampleAssembly(SampleWithSuffixV4AssemblyPath, connectionString);
            // Now update to version with scripts up to 005. Theoretically this is same assembly with just another script added
            UpdateDatabaseFromSampleAssembly(SampleWithSuffixV5AssemblyPath, connectionString);
            // Opps something goes wrong with the application when we deploy 005, revert to assembly with version 004
            UpdateDatabaseFromSampleAssembly(SampleWithSuffixV4AssemblyPath, connectionString);
        }

        private static void UpdateDatabaseFromSampleAssembly(string sampleAssemblyPath, string connectionString)
        {
            Assembly assembly = Assembly.LoadFile(Assembly.GetExecutingAssembly().Location.Replace(SampleApplicationAssemblyPath, sampleAssemblyPath));

            Type myType = assembly.GetType("SampleScripts.DatabaseUpdater");

            myType.GetMethod("UpdateWithDowngradeEnabled").Invoke(null, new object[] { connectionString, Display });
        }

        private static void Display(DatabaseUpgradeResult result)
        {
            if (result.Successful)
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("Success!");
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine(result.Error);
                Console.WriteLine("Failed!");
            }
        }
    }
}

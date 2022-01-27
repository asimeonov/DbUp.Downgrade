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
        public static void Main(string[] args)
        {
            string instanceName = @".\SqlExpress";
            // Uncomment the following line to run against sql local db instance.

            var connectionString = $"Data Source={instanceName};Initial Catalog=SampleApplication;Integrated Security=True;Pooling=False";

            //DropDatabase.For.SqlDatabase(connectionString);

            EnsureDatabase.For.SqlDatabase(connectionString);

            DowngradeScriptsSettings settings = DowngradeScriptsSettings.FromSuffix();

            var upgradeEngineBuilder = DeployChanges.To
                .SqlDatabase(connectionString)
                .WithScriptsAndDowngradeScriptsEmbeddedInAssembly<SqlDowngradeEnabledTableJournal>(Assembly.GetExecutingAssembly(), settings)
                .LogToConsole();

            //var upgradeScriptProvider = new StaticScriptProvider(new List<SqlScript>() { new SqlScript("NameOfYourScript", @"CREATE TABLE [dbo].[Values](
            //         [Id] [int] NOT NULL,
            //         [Value1] [int] NOT NULL,
            //         [Value2] [int] NULL,
            //         CONSTRAINT [PK_Values] PRIMARY KEY CLUSTERED 
            //        (
            //         [Id] ASC
            //        )WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
            //        ) ON [PRIMARY]") });

            //var downgradeScriptProvider = new StaticScriptProvider(new List<SqlScript>() { new SqlScript("NameOfYourScript", "DROP TABLE [dbo].[Values]") });

            //var upgradeEngineBuilder = DeployChanges.To
            //    .SqlDatabase(connectionString)
            //    .WithScripts(upgradeScriptProvider)
            //    .WithDowngradeTableProvider<SqlDowngradeEnabledTableJournal>(downgradeScriptProvider, new DefaultDowngradeScriptFinder())
            //    .LogToConsole();

            var upgrader = upgradeEngineBuilder.BuildWithDowngrade(true);

            var result = upgrader.PerformUpgrade();
            Display(result);

            result = upgrader.PerformDowngradeForScripts(new[] { "SampleApplication.Scripts.Script0005 - Redirects add time to travel.sql" });
            Display(result);

            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine();
            Console.WriteLine("Press any key to continue");
            Console.ReadKey();
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

using DbUp;
using DbUp.Downgrade;
using System;
using System.Reflection;

namespace SampleApplication
{
    class Program
    {
        static void Main(string[] args)
        {
            string instanceName = @"(local)\SqlExpress";
            // Uncomment the following line to run against sql local db instance.
            instanceName = "localhost";

            var connectionString = $"Data Source={instanceName};Initial Catalog=SampleApplication;Integrated Security=True;Pooling=False";

            //DropDatabase.For.SqlDatabase(connectionString);

            EnsureDatabase.For.SqlDatabase(connectionString);

            var upgradeEngineBuilder = DeployChanges.To
                .SqlDatabase(connectionString)
                .WithScriptsAndDowngradeScriptsEmbeddedInAssembly(Assembly.GetExecutingAssembly(), DowngradeScriptsSettings.FromFolder())
                .LogToConsole();

            var upgrader = upgradeEngineBuilder.BuildWithDowngrade(true);

            var result = upgrader.PerformUpgrade();

            // Display the result
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

            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine();
            Console.WriteLine("Press any key to continue");
            Console.ReadKey();
        }
    }
}

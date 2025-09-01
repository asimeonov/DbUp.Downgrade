using System.Threading.Tasks;
using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;
using Microsoft.Data.SqlClient;
using Testcontainers.MsSql;
using Xunit;

namespace DbUp.Downgrade.SqlServer.Tests
{
    public class SqlServerDatabaseFixture : IAsyncLifetime
    {
        private readonly IDatabaseContainer _sqlContainer;
        public string ConnectionString => _sqlContainer.GetConnectionString();

        public SqlServerDatabaseFixture()
        {
            _sqlContainer = new MsSqlBuilder()
                .WithName("sqlserver-test-container")
                .WithImage("mcr.microsoft.com/mssql/server:2022-latest")
                .WithPassword("Your_password123")
                .WithWaitStrategy(Wait.ForUnixContainer().UntilDatabaseIsAvailable(SqlClientFactory.Instance))
                .Build();
        }

        public async Task InitializeAsync()
        {
            await _sqlContainer.StartAsync();
        }

        public async Task DisposeAsync()
        {
            await _sqlContainer.StopAsync();
            await _sqlContainer.DisposeAsync();
        }
    }
}

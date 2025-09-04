using System.Threading.Tasks;
using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;
using Microsoft.Data.SqlClient;
using Testcontainers.MsSql;
using Xunit;

namespace DbUp.Downgrade.SqlServer.Tests
{
    public class SqlServerTestContainerBase : IAsyncLifetime
    {
        private readonly IDatabaseContainer _sqlContainer;
        public string ConnectionString => _sqlContainer.GetConnectionString().Replace("master", "DbUpDowngradeTests");

        public SqlServerTestContainerBase(string containerImage)
        {
            _sqlContainer = new MsSqlBuilder()
                .WithImage(containerImage)
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

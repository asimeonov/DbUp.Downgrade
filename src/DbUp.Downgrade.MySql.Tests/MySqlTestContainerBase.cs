using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;
using MySqlConnector;
using Testcontainers.MySql;

namespace DbUp.Downgrade.MySql.Tests
{
    public class MySqlTestContainerBase : IAsyncLifetime
    {
        private readonly IDatabaseContainer _sqlContainer;
        public string ConnectionString => _sqlContainer.GetConnectionString();

        public MySqlTestContainerBase(string containerImage)
        {
            _sqlContainer = new MySqlBuilder()
                .WithImage(containerImage)
                .WithDatabase("DbUpDowngradeTests")
                .WithUsername("mysql")
                .WithPassword("Your_password123")
                .WithWaitStrategy(Wait.ForUnixContainer().UntilDatabaseIsAvailable(MySqlConnectorFactory.Instance))
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

using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;
using Npgsql;
using Testcontainers.PostgreSql;

namespace DbUp.Downgrade.PostgreSQL.Tests
{
    public class PostgreSqlTestContainerBase : IAsyncLifetime
    {
        private readonly IDatabaseContainer _sqlContainer;
        public string ConnectionString => _sqlContainer.GetConnectionString();

        public PostgreSqlTestContainerBase(string containerImage)
        {
            _sqlContainer = new PostgreSqlBuilder()
                .WithImage(containerImage)
                .WithDatabase("DbUpDowngradeTests")
                .WithUsername("postgres")
                .WithPassword("Your_password123")
                .WithWaitStrategy(Wait.ForUnixContainer().UntilDatabaseIsAvailable(NpgsqlFactory.Instance))
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

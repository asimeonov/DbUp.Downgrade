using Xunit;

namespace DbUp.Downgrade.SqlServer.Tests
{
    public class SqlServer2025Tests : DbUpDowngradeSqlServerTestsBase, IClassFixture<SqlServer2025DatabaseFixture>
    {
        public SqlServer2025Tests(SqlServer2025DatabaseFixture sqlServerDatabaseFixture) : base(sqlServerDatabaseFixture.ConnectionString)
        {
        }
    }
}
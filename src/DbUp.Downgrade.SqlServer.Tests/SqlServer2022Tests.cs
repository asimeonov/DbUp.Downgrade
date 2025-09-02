using Xunit;

namespace DbUp.Downgrade.SqlServer.Tests
{
    public class SqlServer2022Tests : DbUpDowngradeSqlServerTestsBase, IClassFixture<SqlServer2022DatabaseFixture>
    {
        public SqlServer2022Tests(SqlServer2022DatabaseFixture sqlServerDatabaseFixture) : base(sqlServerDatabaseFixture.ConnectionString)
        {
        }
    }
}
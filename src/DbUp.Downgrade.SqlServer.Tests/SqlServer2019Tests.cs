using Xunit;

namespace DbUp.Downgrade.SqlServer.Tests
{
    public class SqlServer2019Tests : DbUpDowngradeSqlServerTestsBase, IClassFixture<SqlServer2019DatabaseFixture>
    {
        public SqlServer2019Tests(SqlServer2019DatabaseFixture databaseFixture) : base(databaseFixture.ConnectionString)
        {
        }
    }
}

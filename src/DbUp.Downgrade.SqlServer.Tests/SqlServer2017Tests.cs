using Xunit;

namespace DbUp.Downgrade.SqlServer.Tests
{
    public class SqlServer2017Tests : DbUpDowngradeSqlServerTestsBase, IClassFixture<SqlServer2017DatabaseFixture>
    {
        public SqlServer2017Tests(SqlServer2017DatabaseFixture sqlServer2017DatabaseFixture) : base(sqlServer2017DatabaseFixture.ConnectionString)
        {
        }
    }
}

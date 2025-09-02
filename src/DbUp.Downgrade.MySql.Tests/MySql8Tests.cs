namespace DbUp.Downgrade.MySql.Tests
{
    public class MySql8Tests : DbUpDowngradeMySqlTestsBase, IClassFixture<MySql8DatabaseFixture>
    {
        public MySql8Tests(MySql8DatabaseFixture fixture) : base(fixture.ConnectionString)
        {
        }
    }
}

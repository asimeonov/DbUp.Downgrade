namespace DbUp.Downgrade.PostgreSQL.Tests
{
    public class Postgre14Tests : DbUpDowngradePostgreTestsBase, IClassFixture<Postgre14DatabaseFixture>
    {
        public Postgre14Tests(Postgre14DatabaseFixture fixture) : base(fixture.ConnectionString)
        {
        }
    }
}

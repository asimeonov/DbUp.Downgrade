namespace DbUp.Downgrade.PostgreSQL.Tests
{
    public class Postgre15Tests : DbUpDowngradePostgreTestsBase, IClassFixture<Postgre15DatabaseFixture>
    {
        public Postgre15Tests(Postgre15DatabaseFixture fixture) : base(fixture.ConnectionString)
        {
        }
    }
}

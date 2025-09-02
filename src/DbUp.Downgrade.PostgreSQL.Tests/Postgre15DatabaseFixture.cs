namespace DbUp.Downgrade.PostgreSQL.Tests
{
    public class Postgre15DatabaseFixture : PostgreSqlTestContainerBase
    {
        public Postgre15DatabaseFixture() : base("postgres:15")
        {
        }
    }
}

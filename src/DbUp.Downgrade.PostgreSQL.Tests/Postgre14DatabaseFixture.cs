namespace DbUp.Downgrade.PostgreSQL.Tests
{
    public class Postgre14DatabaseFixture : PostgreSqlTestContainerBase
    {
        public Postgre14DatabaseFixture() : base("postgres:14")
        {
        }
    }
}

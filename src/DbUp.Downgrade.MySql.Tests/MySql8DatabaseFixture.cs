namespace DbUp.Downgrade.MySql.Tests
{
    public class MySql8DatabaseFixture : MySqlTestContainerBase
    {
        public MySql8DatabaseFixture() : base("mysql:8.0")
        {
        }
    }
}

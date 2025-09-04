namespace DbUp.Downgrade.SqlServer.Tests
{
    public class SqlServer2019DatabaseFixture : SqlServerTestContainerBase
    {
        public SqlServer2019DatabaseFixture() : base("mcr.microsoft.com/mssql/server:2019-latest")
        {
        }
    }
}
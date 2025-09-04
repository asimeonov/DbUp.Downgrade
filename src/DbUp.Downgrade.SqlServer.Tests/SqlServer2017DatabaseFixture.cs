namespace DbUp.Downgrade.SqlServer.Tests
{
    public class SqlServer2017DatabaseFixture : SqlServerTestContainerBase
    {
        public SqlServer2017DatabaseFixture() : base("mcr.microsoft.com/mssql/server:2017-latest")
        {
        }
    }
}
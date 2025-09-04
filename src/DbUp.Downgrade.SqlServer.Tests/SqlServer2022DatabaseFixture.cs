namespace DbUp.Downgrade.SqlServer.Tests
{
    public class SqlServer2022DatabaseFixture : SqlServerTestContainerBase
    {
        public SqlServer2022DatabaseFixture() : base("mcr.microsoft.com/mssql/server:2022-latest")
        {
        }
    }
}

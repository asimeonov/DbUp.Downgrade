namespace DbUp.Downgrade.SqlServer.Tests
{
    public class SqlServer2025DatabaseFixture : SqlServerTestContainerBase
    {
        public SqlServer2025DatabaseFixture() : base("mcr.microsoft.com/mssql/server:2025-latest")
        {
        }
    }
}

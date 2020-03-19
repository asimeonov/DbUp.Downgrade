using DbUp.Downgrade.Helpers;
using DbUp.Engine;
using DbUp.Engine.Output;
using DbUp.Engine.Transactions;
using DbUp.SqlServer;
using System;

namespace DbUp.Downgrade
{
    public class SqlDowngradeEnabledTableJournal : DowngradeEnabledTableJournal
    {
        public const string DefaultTable = "SchemaVersions";

        public SqlDowngradeEnabledTableJournal(
            Func<IConnectionManager> connectionManager,
            Func<IUpgradeLog> logger,
            string schema,
            string table,
            IScriptProvider downgradeScriptsProvider,
            IDowngradeScriptFinder downgradeScriptFinder)
            : base(connectionManager, logger, new SqlServerObjectParser(), schema, table ?? DefaultTable, downgradeScriptsProvider, downgradeScriptFinder)
        {

        }

        protected override string CreateSchemaTableWithDowngradeSql(string quotedPrimaryKeyName)
        {
            return
                $@"create table {FqSchemaTableName} (
                    [Id] int identity(1,1) not null constraint {quotedPrimaryKeyName} primary key,
                    [ScriptName] nvarchar(255) not null,
                    [Applied] datetime not null,
                    [DowngradeScript] nvarchar(MAX) null)";
        }

        protected override string GetInsertJournalEntrySql(string scriptName, string applied, string downgradeScript)
        {
            return $"insert into {FqSchemaTableName} (ScriptName, Applied, DowngradeScript) values ({scriptName}, {applied}, {downgradeScript})";
        }

        protected override string GetJournalEntriesSql()
        {
            return $"select [ScriptName] from {FqSchemaTableName} order by [ScriptName]";
        }

        protected override string GetExecutedScriptsInReverseOrderSql()
        {
            return $"select [ScriptName] from {FqSchemaTableName} order by [Applied] desc";
        }

        protected override string GetDowngradeScriptSql(string scriptName)
        {
            return $"select [DowngradeScript] from {FqSchemaTableName} where [ScriptName] = '{scriptName}'";
        }

        protected override string DeleteScriptFromJournalSql(string scriptName)
        {
            return $"delete from {FqSchemaTableName} where [ScriptName] = '{scriptName}'";
        }

        protected override string AddDowngradeScriptColumnSql()
        {
            return $"alter table {FqSchemaTableName} add [DowngradeScript] nvarchar(MAX)";
        }

        protected override string DoesDowngradeColumnExistSql()
        {
            return $"SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = '{UnquotedSchemaTableName}' AND COLUMN_NAME = 'DowngradeScript'";
        }
    }
}

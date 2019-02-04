using DbUp.Engine;
using DbUp.Engine.Output;
using DbUp.Engine.Transactions;
using DbUp.SqlServer;
using System;
using System.Collections.Generic;
using System.Text;

namespace DbUp.Rollback
{
    public class SqlRollbackEnabledTableJournal : RollbackEnabledTableJournal
    {
        public SqlRollbackEnabledTableJournal(
            Func<IConnectionManager> connectionManager,
            Func<IUpgradeLog> logger,
            string schema,
            string table,
            IScriptProvider rollbackScriptsProvider)
            : base(connectionManager, logger, new SqlServerObjectParser(), schema, table, rollbackScriptsProvider)
        {

        }

        protected override string CreateSchemaTableWithRollbackSql(string quotedPrimaryKeyName)
        {
            return
                $@"create table {FqSchemaTableName} (
                    [Id] int identity(1,1) not null constraint {quotedPrimaryKeyName} primary key,
                    [ScriptName] nvarchar(255) not null,
                    [Applied] datetime not null,
                    [RollbackScript] nvarchar(MAX) null)";
        }

        protected override string GetInsertJournalEntrySql(string scriptName, string applied, string rollbackScript)
        {
            return $"insert into {FqSchemaTableName} (ScriptName, Applied, RollbackScript) values ({scriptName}, {applied}, {rollbackScript})";
        }

        protected override string GetJournalEntriesSql()
        {
            return $"select [ScriptName] from {FqSchemaTableName} order by [ScriptName]";
        }

        protected override string GetExecutedScriptsInReverseOrderSql()
        {
            return $"select [ScriptName] from {FqSchemaTableName} order by [Applied] desc";
        }

        protected override string GetRollbackScriptSql(string scriptName)
        {
            return $"select [RollbackScript] from {FqSchemaTableName} where [ScriptName] = '{scriptName}'";
        }

        protected override string DeleteScriptFromJournalSql(string scriptName)
        {
            return $"delete from {FqSchemaTableName} where [ScriptName] = '{scriptName}'";
        }

        protected override string AddRollbackScriptColumnSql()
        {
            return $"alter table {FqSchemaTableName} add [RollbackScript] nvarchar(MAX)";
        }

        protected override string DoesRollbackColumnExistSql()
        {
            return $"SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = '{UnquotedSchemaTableName}' AND COLUMN_NAME = 'RollbackScript'";
        }
    }
}

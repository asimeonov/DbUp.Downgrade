using DbUp.Engine;
using DbUp.Engine.Output;
using DbUp.Engine.Transactions;
using DbUp.Postgresql;
using System;

namespace DbUp.Downgrade
{
    public class PostgresDowngradeEnabledTableJournal : DowngradeEnabledTableJournal
    {
        public const string DefaultTable = "schemaversions";

        public PostgresDowngradeEnabledTableJournal(
            Func<IConnectionManager> connectionManager,
            Func<IUpgradeLog> logger,
            string schema,
            string table,
            IScriptProvider downgradeScriptsProvider)
            : base(connectionManager, logger, new PostgresqlObjectParser(), schema, table ?? DefaultTable, downgradeScriptsProvider)
        {

        }

        protected override string CreateSchemaTableWithDowngradeSql(string quotedPrimaryKeyName)
        {
            return
                $@"CREATE TABLE {FqSchemaTableName}
                (
                    schemaversionsid serial NOT NULL,
                    scriptname character varying(255) NOT NULL,
                    applied timestamp without time zone NOT NULL,
                    downgradescript text,
                    CONSTRAINT {quotedPrimaryKeyName} PRIMARY KEY (schemaversionsid)
                )";
        }

        protected override string GetInsertJournalEntrySql(string scriptName, string applied, string downgradeScript)
        {
            return $"insert into {FqSchemaTableName} (ScriptName, Applied, DowngradeScript) values ({scriptName}, {applied}, {downgradeScript})";
        }

        protected override string GetJournalEntriesSql()
        {
            return $"select ScriptName from {FqSchemaTableName} order by ScriptName";
        }

        protected override string GetExecutedScriptsInReverseOrderSql()
        {
            return $"select ScriptName from {FqSchemaTableName} order by Applied desc";
        }

        protected override string GetDowngradeScriptSql(string scriptName)
        {
            return $"select downgradescript from {FqSchemaTableName} where scriptname = '{scriptName}'";
        }

        protected override string DeleteScriptFromJournalSql(string scriptName)
        {
            return $"delete from {FqSchemaTableName} where scriptname = '{scriptName}'";
        }

        protected override string AddDowngradeScriptColumnSql()
        {
            return $"alter table {FqSchemaTableName} add downgradescript text";
        }

        protected override string DoesDowngradeColumnExistSql()
        {
            return $"SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = '{UnquotedSchemaTableName}' AND COLUMN_NAME = 'downgradescript'";
        }
    }
}

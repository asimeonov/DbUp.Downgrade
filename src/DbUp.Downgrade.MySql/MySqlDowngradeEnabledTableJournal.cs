using DbUp.Downgrade.Helpers;
using DbUp.Engine;
using DbUp.Engine.Output;
using DbUp.Engine.Transactions;
using DbUp.MySql;
using System;

namespace DbUp.Downgrade
{
    public class MySqlDowngradeEnabledTableJournal : DowngradeEnabledTableJournal
    {
        public const string DefaultTable = "schemaversions";

        public MySqlDowngradeEnabledTableJournal(
            Func<IConnectionManager> connectionManager,
            Func<IUpgradeLog> logger,
            string schema,
            string table,
            IScriptProvider downgradeScriptsProvider,
            IDowngradeScriptFinder downgradeScriptFinder)
            : base(connectionManager, logger, new MySqlObjectParser(), schema, table ?? DefaultTable, downgradeScriptsProvider, downgradeScriptFinder)
        {

        }

        protected override string CreateSchemaTableWithDowngradeSql(string quotedPrimaryKeyName)
        {
            return
                $@"CREATE TABLE {FqSchemaTableName} (
                    `schemaversionid` INT NOT NULL AUTO_INCREMENT,
                    `scriptname` VARCHAR(255) NOT NULL,
                    `applied` TIMESTAMP NOT NULL,
                    `downgradescript` TEXT DEFAULT NULL,
                    PRIMARY KEY (`schemaversionid`)
                );";
        }

        protected override string GetInsertJournalEntrySql(string scriptName, string applied, string downgradeScript)
        {
            if (downgradeScript == null)
            {
                return $"INSERT INTO {FqSchemaTableName} (`scriptname`, `applied`) VALUES ({scriptName}, {applied})";
            }

            return $"INSERT INTO {FqSchemaTableName} (`scriptname`, `applied`, `downgradescript`) VALUES ({scriptName}, {applied}, {downgradeScript})";
        }

        protected override string GetJournalEntriesSql()
        {
            return $"SELECT `scriptname` FROM {FqSchemaTableName} ORDER BY `scriptname`";
        }

        protected override string GetExecutedScriptsInReverseOrderSql()
        {
            return $"SELECT `scriptname` FROM {FqSchemaTableName} ORDER BY `applied` DESC";
        }

        protected override string GetDowngradeScriptSql(string scriptName)
        {
            return $"SELECT `downgradescript` FROM {FqSchemaTableName} WHERE `scriptname` = '{scriptName}'";
        }

        protected override string DeleteScriptFromJournalSql(string scriptName)
        {
            return $"DELETE FROM {FqSchemaTableName} WHERE `scriptname` = '{scriptName}'";
        }

        protected override string AddDowngradeScriptColumnSql()
        {
            return $"ALTER TABLE {FqSchemaTableName} ADD `downgradescript` TEXT DEFAULT NULL";
        }

        protected override string DoesDowngradeColumnExistSql()
        {
            return $"SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = '{UnquotedSchemaTableName}' AND COLUMN_NAME = 'downgradescript'";
        }
    }
}

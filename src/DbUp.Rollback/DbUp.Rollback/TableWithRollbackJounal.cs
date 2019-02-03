using DbUp.Engine;
using DbUp.Engine.Output;
using DbUp.Engine.Transactions;
using DbUp.ScriptProviders;
using DbUp.SqlServer;
using DbUp.Support;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;

namespace DbUp.Rollback
{
    public class TableWithRollbackJournal : TableJournal
    {
        private readonly List<SqlScript> _rollbackScripts;

        public TableWithRollbackJournal(
            Func<IConnectionManager> connectionManager,
            Func<IUpgradeLog> logger,
            string schema,
            string table,
            EmbeddedScriptProvider rollbackScriptsProvider)
            : base(connectionManager, logger, new SqlServerObjectParser(), schema, table)
        {
            _rollbackScripts = rollbackScriptsProvider.GetScripts(connectionManager()).ToList();
        }

        protected override string CreateSchemaTableSql(string quotedPrimaryKeyName)
        {
            return
                $@"create table {FqSchemaTableName} (
                    [Id] int identity(1,1) not null constraint {quotedPrimaryKeyName} primary key,
                    [ScriptName] nvarchar(255) not null,
                    [Applied] datetime not null,
                    [RollbackScript] nvarchar(MAX) null)";
        }

        protected override string GetInsertJournalEntrySql(string scriptName, string applied)
        {
            throw new NotSupportedException();
        }

        protected string GetInsertJournalEntrySql(string scriptName, string applied, string rollbackScript)
        {
            return $"insert into {FqSchemaTableName} (ScriptName, Applied, RollbackScript) values ({scriptName}, {applied}, {rollbackScript})";
        }

        protected override string GetJournalEntriesSql()
        {
            return $"select [ScriptName] from {FqSchemaTableName} order by [ScriptName]";
        }

        protected new IDbCommand GetInsertScriptCommand(Func<IDbCommand> dbCommandFactory, SqlScript script)
        {
            var command = dbCommandFactory();

            var scriptNameParam = command.CreateParameter();
            scriptNameParam.ParameterName = "scriptName";
            scriptNameParam.Value = script.Name;
            command.Parameters.Add(scriptNameParam);

            var appliedParam = command.CreateParameter();
            appliedParam.ParameterName = "applied";
            appliedParam.Value = DateTime.Now;
            command.Parameters.Add(appliedParam);

            var rollbackScriptParam = command.CreateParameter();
            rollbackScriptParam.ParameterName = "rollbackScript";

            string[] executingStringParts = script.Name.Replace(".sql", string.Empty).Split('.');
            var correspondingRollbackScript = _rollbackScripts.SingleOrDefault(s => s.Name.Contains(executingStringParts.Last()));

            if (correspondingRollbackScript != null)
            {
                rollbackScriptParam.Value = correspondingRollbackScript.Contents;
            }
            else
            {
                rollbackScriptParam.Value = DBNull.Value;
            }

            command.Parameters.Add(rollbackScriptParam);

            command.CommandText = GetInsertJournalEntrySql("@scriptName", "@applied", "@rollbackScript");
            command.CommandType = CommandType.Text;
            return command;
        }

        public override void StoreExecutedScript(SqlScript script, Func<IDbCommand> dbCommandFactory)
        {
            EnsureTableExistsAndIsLatestVersion(dbCommandFactory);
            using (var command = GetInsertScriptCommand(dbCommandFactory, script))
            {
                command.ExecuteNonQuery();
            }
        }

        public virtual string GetRollbackScript(string scriptName)
        {
            Log().WriteInformation("Newer (unrecognized) script '{0}' found, searching for corresponding revert script.", scriptName);
            string sqlCommand = $"select [RollbackScript] from {FqSchemaTableName} where [ScriptName] = '{scriptName}'";

            return ConnectionManager().ExecuteCommandsWithManagedConnection(dbCommandFactory =>
            {
                using (var command = dbCommandFactory())
                {
                    command.CommandText = sqlCommand;
                    command.CommandType = CommandType.Text;

                    return (string)command.ExecuteScalar();
                }
            });
        }

        public virtual void RevertScript(string scriptName, string rollbackScript, string appVersion)
        {
            if (string.IsNullOrWhiteSpace(rollbackScript))
            {
                Log().WriteWarning("Script '{0}' does not have a revert script.", scriptName);
                return;
            }

            Log().WriteInformation("Script '{0}' was not recognized in version {1} and will be reverted.", scriptName, appVersion);

            try
            {
                ConnectionManager().ExecuteCommandsWithManagedConnection(dbCommandFactory =>
                {
                    using (var command = dbCommandFactory())
                    {
                        command.CommandText = rollbackScript;
                        command.CommandType = CommandType.Text;

                        command.ExecuteNonQuery();

                        command.CommandText = $"delete from {FqSchemaTableName} where [ScriptName] = '{scriptName}'";

                        command.ExecuteNonQuery();
                    }
                });
            }
            catch (Exception ex)
            {
                Log().WriteError("Script '{0}' wasn't reverted, Exception was thrown:\r\n{1}", scriptName, ex.ToString());
                throw;
            }
        }

        public override void EnsureTableExistsAndIsLatestVersion(Func<IDbCommand> dbCommandFactory)
        {
            if (!DoesTableExist(dbCommandFactory))
            {
                base.EnsureTableExistsAndIsLatestVersion(dbCommandFactory);
            }
            else
            {
                if (!IsTableInLatestVersion(dbCommandFactory))
                {
                    using (var command = dbCommandFactory())
                    {
                        command.CommandText = $"alter table {FqSchemaTableName} add [RollbackScript] nvarchar(MAX)";
                        command.CommandType = CommandType.Text;

                        command.ExecuteNonQuery();
                    }
                }
            }
        }

        protected virtual bool IsTableInLatestVersion(Func<IDbCommand> dbCommandFactory)
        {
            using (var command = dbCommandFactory())
            {
                command.CommandText = DoesRollbackColumnExistSql();
                command.CommandType = CommandType.Text;
                var executeScalar = command.ExecuteScalar();
                if (executeScalar == null)
                    return false;
                if (executeScalar is long)
                    return (long)executeScalar == 1;
                if (executeScalar is decimal)
                    return (decimal)executeScalar == 1;
                return (int)executeScalar == 1;
            }
        }

        protected virtual string DoesRollbackColumnExistSql()
        {
            return $"SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = '{UnquotedSchemaTableName}' AND COLUMN_NAME = 'RollbackScript'";
        }
    }
}

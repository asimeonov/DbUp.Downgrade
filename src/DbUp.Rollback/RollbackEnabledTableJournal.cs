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
    public abstract class RollbackEnabledTableJournal : TableJournal
    {
        private readonly List<SqlScript> _rollbackScripts;

        public RollbackEnabledTableJournal(
            Func<IConnectionManager> connectionManager,
            Func<IUpgradeLog> logger,
            ISqlObjectParser sqlObjectParser,
            string schema,
            string table,
            IScriptProvider rollbackScriptsProvider)
            : base(connectionManager, logger, sqlObjectParser, schema, table)
        {
            _rollbackScripts = rollbackScriptsProvider.GetScripts(connectionManager()).ToList();
        }

        protected abstract string CreateSchemaTableWithRollbackSql(string quotedPrimaryKeyName);

        protected override string CreateSchemaTableSql(string quotedPrimaryKeyName)
        {
            return CreateSchemaTableWithRollbackSql(quotedPrimaryKeyName);
        }

        protected abstract string GetInsertJournalEntrySql(string scriptName, string applied, string rollbackScript);

        protected override string GetInsertJournalEntrySql(string scriptName, string applied)
        {
            return GetInsertJournalEntrySql(scriptName, applied, null);
        }

        protected abstract string GetExecutedScriptsInReverseOrderSql();

        public string[] GetExecutedScriptsInReverseOrder()
        {
            return ConnectionManager().ExecuteCommandsWithManagedConnection(dbCommandFactory =>
            {
                if (DoesTableExist(dbCommandFactory))
                {
                    Log().WriteInformation("Fetching list of already executed scripts ordered by Date desc.");

                    var scripts = new List<string>();

                    using (var command = dbCommandFactory())
                    {
                        command.CommandText = GetExecutedScriptsInReverseOrderSql();
                        command.CommandType = CommandType.Text;
                        using (var reader = command.ExecuteReader())
                        {
                            while (reader.Read())
                                scripts.Add((string)reader[0]);
                        }
                    }

                    return scripts.ToArray();
                }
                else
                {
                    Log().WriteInformation("Journal table does not exist");
                    return new string[0];
                }
            });
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

        protected abstract string GetRollbackScriptSql(string scriptName);

        public virtual string GetRollbackScript(string scriptName)
        {
            Log().WriteInformation("Newer (unrecognized) script '{0}' found, searching for corresponding revert script.", scriptName);

            return ConnectionManager().ExecuteCommandsWithManagedConnection(dbCommandFactory =>
            {
                using (var command = dbCommandFactory())
                {
                    command.CommandText = GetRollbackScriptSql(scriptName);
                    command.CommandType = CommandType.Text;

                    return (string)command.ExecuteScalar();
                }
            });
        }

        protected abstract string DeleteScriptFromJournalSql(string scriptName);

        public virtual void RevertScript(string scriptName, string rollbackScript)
        {
            if (string.IsNullOrWhiteSpace(rollbackScript))
            {
                Log().WriteWarning("Script '{0}' does not have a revert script.", scriptName);
                return;
            }

            Log().WriteInformation("Script '{0}' was not recognized in cuurent version and will be reverted.", scriptName);

            try
            {
                ConnectionManager().ExecuteCommandsWithManagedConnection(dbCommandFactory =>
                {
                    using (var command = dbCommandFactory())
                    {
                        command.CommandText = rollbackScript;
                        command.CommandType = CommandType.Text;

                        command.ExecuteNonQuery();

                        command.CommandText = DeleteScriptFromJournalSql(scriptName);

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

        protected abstract string AddRollbackScriptColumnSql();

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
                        command.CommandText = AddRollbackScriptColumnSql();
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

        protected abstract string DoesRollbackColumnExistSql();
    }
}

using DbUp.Downgrade.Helpers;
using DbUp.Engine;
using DbUp.Engine.Output;
using DbUp.Engine.Transactions;
using DbUp.Support;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;

namespace DbUp.Downgrade
{
    public abstract class DowngradeEnabledTableJournal : TableJournal
    {
        private readonly List<SqlScript> _downgradeScripts;
        bool journalIsInLatestVersion;
        private readonly IDowngradeScriptFinder _downgradeScriptFinder;

        public DowngradeEnabledTableJournal(
            Func<IConnectionManager> connectionManager,
            Func<IUpgradeLog> logger,
            ISqlObjectParser sqlObjectParser,
            string schema,
            string table,
            IScriptProvider downgradeScriptsProvider,
            IDowngradeScriptFinder downgradeScriptFinder)
            : base(connectionManager, logger, sqlObjectParser, schema, table)
        {
            _downgradeScripts = downgradeScriptsProvider.GetScripts(connectionManager()).ToList();
            journalIsInLatestVersion = false;
            _downgradeScriptFinder = downgradeScriptFinder;
        }

        protected abstract string CreateSchemaTableWithDowngradeSql(string quotedPrimaryKeyName);

        protected override string CreateSchemaTableSql(string quotedPrimaryKeyName)
        {
            return CreateSchemaTableWithDowngradeSql(quotedPrimaryKeyName);
        }

        protected abstract string GetInsertJournalEntrySql(string scriptName, string applied, string downgradeScript);

        protected override string GetInsertJournalEntrySql(string scriptName, string applied)
        {
            return GetInsertJournalEntrySql(scriptName, applied, null);
        }

        protected abstract string GetExecutedScriptsInReverseOrderSql();

        public string[] GetExecutedScriptsInReverseOrder()
        {
            return ConnectionManager().ExecuteCommandsWithManagedConnection(dbCommandFactory =>
            {
                EnsureTableExistsAndIsLatestVersion(dbCommandFactory);

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

            var downgradeScriptParam = command.CreateParameter();
            downgradeScriptParam.ParameterName = "downgradeScript";

            var correspondingDowngradeScript = _downgradeScriptFinder.GetCorrespondingDowngradeScript(script, _downgradeScripts);

            if (correspondingDowngradeScript != null)
            {
                Log().WriteInformation("Script '{0}' has corresponding downgrade script with name '{1}'.", script.Name, correspondingDowngradeScript.Name);
                downgradeScriptParam.Value = correspondingDowngradeScript.Contents;
            }
            else
            {
                Log().WriteInformation("Script '{0}' don't have corresponding downgrade script.", script.Name);
                downgradeScriptParam.Value = DBNull.Value;
            }

            command.Parameters.Add(downgradeScriptParam);

            command.CommandText = GetInsertJournalEntrySql("@scriptName", "@applied", "@downgradeScript");
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

        protected abstract string GetDowngradeScriptSql(string scriptName);

        public virtual string GetDowngradeScript(string scriptName)
        {
            Log().WriteInformation("Newer (unrecognized) script '{0}' found, searching for corresponding revert script.", scriptName);

            return ConnectionManager().ExecuteCommandsWithManagedConnection(dbCommandFactory =>
            {
                using (var command = dbCommandFactory())
                {
                    command.CommandText = GetDowngradeScriptSql(scriptName);
                    command.CommandType = CommandType.Text;

                    return command.ExecuteScalar().ToString();
                }
            });
        }

        protected abstract string DeleteScriptFromJournalSql(string scriptName);

        public virtual void RevertScript(string scriptName, string downgradeScript)
        {
            if (string.IsNullOrWhiteSpace(downgradeScript))
            {
                Log().WriteWarning("Script '{0}' does not have a revert script.", scriptName);
                return;
            }

            Log().WriteInformation("Script '{0}' was not recognized in cuurent version and will be reverted.", scriptName);

            try
            {
                var connectionManager = ConnectionManager();
                var scriptStatements = connectionManager.SplitScriptIntoCommands(downgradeScript);

                connectionManager.ExecuteCommandsWithManagedConnection(dbCommandFactory =>
                {
                    foreach (var statement in scriptStatements)
                    {
                        using (var command = dbCommandFactory())
                        {
                            command.CommandText = statement;
                            command.CommandType = CommandType.Text;

                            command.ExecuteNonQuery();

                            command.CommandText = DeleteScriptFromJournalSql(scriptName);

                            command.ExecuteNonQuery();
                        }
                    }
                });
            }
            catch (Exception ex)
            {
                Log().WriteError("Script '{0}' wasn't reverted, Exception was thrown:\r\n{1}", scriptName, ex.ToString());
                throw;
            }
        }

        protected abstract string AddDowngradeScriptColumnSql();

        public override void EnsureTableExistsAndIsLatestVersion(Func<IDbCommand> dbCommandFactory)
        {
            if (journalIsInLatestVersion)
            {
                return;
            }

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
                        command.CommandText = AddDowngradeScriptColumnSql();
                        command.CommandType = CommandType.Text;

                        command.ExecuteNonQuery();
                    }
                }
            }

            journalIsInLatestVersion = true;
        }

        protected virtual bool IsTableInLatestVersion(Func<IDbCommand> dbCommandFactory)
        {
            using (var command = dbCommandFactory())
            {
                command.CommandText = DoesDowngradeColumnExistSql();
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

        protected abstract string DoesDowngradeColumnExistSql();
    }
}

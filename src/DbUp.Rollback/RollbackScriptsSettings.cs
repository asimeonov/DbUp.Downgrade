using System;

namespace DbUp.Rollback
{
    public class RollbackScriptsSettings
    {
        public Func<string, bool> ScriptsFilter { get; private set; }

        public Func<string, bool> RollbackScriptsFilter { get; private set; }

        public RollbackScriptsSettings(Func<string, bool> scriptsFilter, Func<string, bool> rollbackScriptsFilter)
        {
            ScriptsFilter = scriptsFilter;
            RollbackScriptsFilter = rollbackScriptsFilter;
        }

        public static RollbackScriptsSettings FromSuffix(string suffixName = "_rollback")
        {
            Func<string, bool> scriptsFilter = s => s.EndsWith(".sql", StringComparison.OrdinalIgnoreCase) && !s.EndsWith($"{suffixName}.sql", StringComparison.OrdinalIgnoreCase);
            Func<string, bool> rollbackScriptsFilter = s => s.EndsWith($"{suffixName}.sql", StringComparison.OrdinalIgnoreCase);

            return new RollbackScriptsSettings(scriptsFilter, rollbackScriptsFilter);
        }

        public static RollbackScriptsSettings FromFolder(string folderName = "RollbackScripts")
        {
            Func<string, bool> scriptsFilter = s => s.EndsWith(".sql", StringComparison.OrdinalIgnoreCase) && !s.Contains($".{folderName}.");
            Func<string, bool> rollbackScriptsFilter = s => s.Contains($".{folderName}.");

            return new RollbackScriptsSettings(scriptsFilter, rollbackScriptsFilter);
        }
    }
}
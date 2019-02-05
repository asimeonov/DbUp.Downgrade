using System;

namespace DbUp.Downgrade
{
    public class DowngradeScriptsSettings
    {
        public Func<string, bool> ScriptsFilter { get; private set; }

        public Func<string, bool> DowngradeScriptsFilter { get; private set; }

        public DowngradeScriptsSettings(Func<string, bool> scriptsFilter, Func<string, bool> downgradeScriptsFilter)
        {
            ScriptsFilter = scriptsFilter;
            DowngradeScriptsFilter = downgradeScriptsFilter;
        }

        public static DowngradeScriptsSettings FromSuffix(string suffixName = "_downgrade")
        {
            Func<string, bool> scriptsFilter = s => s.EndsWith(".sql", StringComparison.OrdinalIgnoreCase) && !s.EndsWith($"{suffixName}.sql", StringComparison.OrdinalIgnoreCase);
            Func<string, bool> downgradeScriptsFilter = s => s.EndsWith($"{suffixName}.sql", StringComparison.OrdinalIgnoreCase);

            return new DowngradeScriptsSettings(scriptsFilter, downgradeScriptsFilter);
        }

        public static DowngradeScriptsSettings FromFolder(string folderName = "DowngradeScripts")
        {
            Func<string, bool> scriptsFilter = s => s.EndsWith(".sql", StringComparison.OrdinalIgnoreCase) && !s.Contains($".{folderName}.");
            Func<string, bool> downgradeScriptsFilter = s => s.Contains($".{folderName}.");

            return new DowngradeScriptsSettings(scriptsFilter, downgradeScriptsFilter);
        }
    }
}

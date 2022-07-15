using System;
using System.Collections.Generic;

namespace DbUp.Downgrade
{
    public class DowngradeScriptsSettings
    {
        public Func<string, bool> ScriptsFilter { get; private set; }

        public Func<string, bool> DowngradeScriptsFilter { get; private set; }

        public KeyValuePair<DowngradeScriptsSettingsMode, string> SettingsMode { get; private set; }

        public DowngradeScriptsSettings(Func<string, bool> scriptsFilter, Func<string, bool> downgradeScriptsFilter, 
            KeyValuePair<DowngradeScriptsSettingsMode, string> settingsMode)
        {
            ScriptsFilter = scriptsFilter;
            DowngradeScriptsFilter = downgradeScriptsFilter;
            SettingsMode = settingsMode;
        }

        /// <summary>
        /// Builds <see cref="DowngradeScriptsSettings"/> from suffix added to downgrade scripts located in the same folder as upgrade scirpts.
        /// </summary>
        /// <param name="suffixName">Name of the suffix. Default to: _downgrade</param>
        /// <returns></returns>
        public static DowngradeScriptsSettings FromSuffix(string suffixName = "_downgrade")
        {
            Func<string, bool> scriptsFilter = s => s.EndsWith(".sql", StringComparison.OrdinalIgnoreCase) && !s.EndsWith($"{suffixName}.sql", StringComparison.OrdinalIgnoreCase);
            Func<string, bool> downgradeScriptsFilter = s => s.EndsWith($"{suffixName}.sql", StringComparison.OrdinalIgnoreCase);

            return new DowngradeScriptsSettings(scriptsFilter, downgradeScriptsFilter, new KeyValuePair<DowngradeScriptsSettingsMode, string>(DowngradeScriptsSettingsMode.Suffix, suffixName));
        }

        /// <summary>
        /// Builds <see cref="DowngradeScriptsSettings"/> from Folder contaning downgrade scripts.
        /// </summary>
        /// <param name="folderName">Name of the folder.</param>
        /// <returns></returns>
        public static DowngradeScriptsSettings FromFolder(string folderName = "DowngradeScripts")
        {
            Func<string, bool> scriptsFilter = s => s.EndsWith(".sql", StringComparison.OrdinalIgnoreCase) && !s.Contains($".{folderName}.");
            Func<string, bool> downgradeScriptsFilter = s => s.Contains($".{folderName}.");

            return new DowngradeScriptsSettings(scriptsFilter, downgradeScriptsFilter, new KeyValuePair<DowngradeScriptsSettingsMode, string>(DowngradeScriptsSettingsMode.Folder, folderName));
        }
    }
}

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using DbUp.Engine;

namespace DbUp.Downgrade.Helpers
{
    /// <summary>
    /// Comparing Scripts and DowngradeScripts embedded in assembly.
    /// </summary>
    public class EmbeddedDowngradeScriptFinder : IDowngradeScriptFinder
    {
        private readonly DowngradeScriptsSettings _downgradeScriptsSettings;

        /// <summary>
        /// Creates EmbeddedDowngradeScriptFinder with DowngradeScriptsSettings.
        /// </summary>
        /// <param name="downgradeScriptsSettings">Pre-builded DowngradeScriptsSettings.</param>
        public EmbeddedDowngradeScriptFinder(DowngradeScriptsSettings downgradeScriptsSettings)
        {
            if(downgradeScriptsSettings == null)
            {
                throw new ArgumentNullException("downgradeScriptsSettings");
            }

            _downgradeScriptsSettings = downgradeScriptsSettings;
        }

        public SqlScript GetCorrespondingDowngradeScript(SqlScript scriptToExecute, List<SqlScript> downgradeScripts)
        {
            switch (_downgradeScriptsSettings.SettingsMode.Key)
            {
                case DowngradeScriptsSettingsMode.Suffix:
                    string executingScriptName = Path.GetFileNameWithoutExtension(scriptToExecute.Name);
                    return downgradeScripts.SingleOrDefault(s => s.Name.Contains(executingScriptName));
                case DowngradeScriptsSettingsMode.Folder:
                    foreach (var downgradeScript in downgradeScripts)
                    {
                        string[] downgradeScriptStringParts = downgradeScript.Name.Split(new string[] { _downgradeScriptsSettings.SettingsMode.Value }, StringSplitOptions.None);
                        if(downgradeScriptStringParts.Length > 1 && scriptToExecute.Name.Contains(downgradeScriptStringParts[1]))
                        {
                            return downgradeScript;
                        }
                    }
                    return null;
                default:
                    throw new NotSupportedException();
            }
        }
    }
}

using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Microsoft.Extensions.Configuration;

namespace LiveReloadServer
{
    public class Helpers
    {

        public static string AppHeader;
        public static string ExeName = "LiveReloadServer";

        public static void OpenUrl(string url)
        {
            Process p = null;
            try
            {
                var psi = new ProcessStartInfo(url);
                p = Process.Start(psi);
            }
            catch
            {
                // hack because of this: https://github.com/dotnet/corefx/issues/10361
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ||
                    RuntimeInformation.OSDescription.Contains("microsoft-standard"))  // wsl
                {
                    url = url.Replace("&", "^&");
                    try
                    {
                        Process.Start(new ProcessStartInfo("cmd.exe", $"/c start {url}") {CreateNoWindow = true});
                    }
                    catch
                    {
                        ConsoleHelper.WriteEmbeddedColorLine($"Open your browser at: [darkcyan]{url}[/darkcyan]");
                    }
                }
                else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                {
                    p = Process.Start("xdg-open", url);
                }
                else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                {
                    p = Process.Start("open", url);
                }
                else
                {
                    ConsoleHelper.WriteEmbeddedColorLine($"Open your browser at: [darkcyan]{url}[/darkcyan]");
                }
            }

            p?.Dispose();
        }

        /// <summary>
        /// Retrieves a string value from the configuration and optionally sets a default value
        /// if the value is not set.
        /// </summary>
        /// <param name="key"></param>
        /// <param name="config"></param>
        /// <param name="defaultValue"></param>
        /// <returns></returns>
        public static string GetStringSetting(string key, IConfiguration config, string defaultValue = null)
        {
            var value = config[key];
            if (value == null)
                value = defaultValue;

            return value;
        }

        /// <summary>
        /// Retrieves a string value from the configuration and optionally sets a default value
        /// if the value is not set.
        /// </summary>
        /// <param name="key"></param>
        /// <param name="config"></param>
        /// <param name="defaultValue"></param>
        /// <returns></returns>
        public static int GetIntegerSetting(string key, IConfiguration config, int defaultValue = 0)
        {
            var value = config[key];
            if (value == null)
                return defaultValue;

            if (!int.TryParse(value, out int resultValue))
                return defaultValue;

            return resultValue;
        }

        public static bool GetLogicalSetting(string key, IConfiguration config, bool defaultValue = false)
        {
            bool? resultValue = null;
            var temp = config[key];

            if (!string.IsNullOrEmpty(temp))
            {
                if (temp.Equals("true", StringComparison.InvariantCultureIgnoreCase))
                    resultValue = true;
                if (temp.Equals("false", StringComparison.InvariantCultureIgnoreCase))
                    resultValue = false;
            }

            if (resultValue == null)
            {
                if (Environment.CommandLine.Contains($"-{key}", StringComparison.OrdinalIgnoreCase))
                    resultValue = true;
                else
                    resultValue = defaultValue;
            }

            return resultValue.Value;
        }
    }
}

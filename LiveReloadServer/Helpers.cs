using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;

namespace LiveReloadServer
{
    public class Helpers
    {

        public static string AppHeader;
        public static string ExeName = "LiveReloadServer";

        public static void OpenUrl(string url)
        {
            try
            {
                Process.Start(url);
            }
            catch
            {
                // hack because of this: https://github.com/dotnet/corefx/issues/10361
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    url = url.Replace("&", "^&");
                    Process.Start(new ProcessStartInfo("cmd", $"/c start {url}") { CreateNoWindow = true });
                }
                else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                {
                    Process.Start("xdg-open", url);
                }
                else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                {
                    Process.Start("open", url);
                }
                else
                {
                    throw;
                }

            }

        }

        public static bool GetLogicalSetting(string key, IConfiguration config)
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
                if (Environment.CommandLine.Contains($"-{key}", StringComparison.InvariantCultureIgnoreCase))
                    resultValue = true;
                else
                    resultValue = false;
            }

            return resultValue.Value;
        }
    }
}

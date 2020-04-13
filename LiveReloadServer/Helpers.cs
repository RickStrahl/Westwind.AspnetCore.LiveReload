using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
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

    
    /// <summary>
    /// Console Helper class that provides coloring to individual commeands
    /// </summary>
    public static class ConsoleHelper
    {

        /// <summary>
        /// WriteLine with color
        /// </summary>
        /// <param name="text"></param>
        /// <param name="color"></param>
        public static void WriteLine(string text, ConsoleColor color = ConsoleColor.White)
        {
            var oldColor = Console.ForegroundColor;
            Console.ForegroundColor = color;
            Console.WriteLine(text);
            Console.ForegroundColor = oldColor;
        }

        /// <summary>
        /// Write with color
        /// </summary>
        /// <param name="text"></param>
        /// <param name="color"></param>
        public static void Write(string text, ConsoleColor color = ConsoleColor.White)
        {
            var oldColor = Console.ForegroundColor;
            Console.ForegroundColor = color;
            Console.Write(text);
            Console.ForegroundColor = oldColor;
        }


        /// <summary>
        /// Write a Success Line - green
        /// </summary>
        /// <param name="text"></param>
        public static void WriteSuccess(string text)
        {
            WriteLine(text, ConsoleColor.Green);
        }

        
        /// <summary>
        /// Write a Error Line - Red
        /// </summary>
        /// <param name="text"></param>
        public static void WriteError(string text)
        {
            WriteLine(text, ConsoleColor.Red);
        }


        /// <summary>
        /// Write a Info Line - dark cyan
        /// </summary>
        /// <param name="text"></param>
        public static void WriteInfo(string text)
        {
            WriteLine(text, ConsoleColor.DarkCyan);
        }

        /// <summary>
        /// Write a Warning Line - Yellow
        /// </summary>
        /// <param name="text"></param>
        public static void WriteWarning(string text)
        {
            WriteLine(text, ConsoleColor.Yellow);
        }

        public static void WriteWrappedHeader(string headerText, char wrapperChar = '-', ConsoleColor headerColor = ConsoleColor.Yellow)
        {
            string line = new StringBuilder().Insert(0, wrapperChar.ToString(), headerText.Length).ToString();    

           Console.WriteLine(line);
            WriteLine(headerText,headerColor);
            Console.WriteLine(line);
        }
    }

}

using System;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Localization;
using Westwind.Utilities;

namespace LiveReloadServer
{
    /// <summary>
    /// Console Helper class that provides coloring to individual commands
    /// </summary>
    public static class ConsoleHelper
    {

        /// <summary>
        /// WriteLine with color
        /// </summary>
        /// <param name="text"></param>
        /// <param name="color"></param>
        public static void WriteLine(string text, ConsoleColor? color = null)
        {

            var oldColor = Console.ForegroundColor;

            if (color != null)
                Console.ForegroundColor = color.Value;

            Console.WriteLine(text);

            Console.ForegroundColor = oldColor;
        }

        /// <summary>
        /// Writes out a line by color
        /// </summary>
        /// <param name="text"></param>
        /// <param name="color"></param>
        public static void WriteLine(string text, string color)
        {
            if (string.IsNullOrEmpty(color))
            {
                WriteLine(text);
                return;
            }

            if (!Enum.TryParse(color, true, out ConsoleColor col))
            {
                WriteLine(text);
            }
            else
            {
                WriteLine(text, col);
            }
        }

        /// <summary>
        /// Write with color
        /// </summary>
        /// <param name="text"></param>
        /// <param name="color"></param>
        public static void Write(string text, ConsoleColor? color = null)
        {
            var oldColor = Console.ForegroundColor;

            if (color != null)
                Console.ForegroundColor = color.Value;

            Console.Write(text);

            Console.ForegroundColor = oldColor;
        }

        /// <summary>
        /// Writes out a line by color
        /// </summary>
        /// <param name="text"></param>
        /// <param name="color"></param>
        public static void Write(string text, string color)
        {
            if (string.IsNullOrEmpty(color))
            {
                Write(text);
                return;
            }

            if (!ConsoleColor.TryParse(color, true, out ConsoleColor col))
            {
                Write(text);
            }
            else
            {
                Write(text, col);
            }
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
            WriteLine(headerText, headerColor);
            Console.WriteLine(line);
        }

        /// <summary>
        /// Allows a string to be written with embedded color values using:
        /// This is [red]Red[/red] text and this is [cyan]Blue[/blue] text
        /// </summary>
        /// <param name="text">Text to display</param>
        /// <param name="color">Base text color</param>
        public static void WriteEmbeddedColorLine(string text, ConsoleColor? color = null)
        {
            if (color == null)
                color = Console.ForegroundColor;

            if (string.IsNullOrEmpty(text))
            {
                WriteLine(string.Empty);
                return;
            }

            int at = text.IndexOf("[");
            int at2 = text.IndexOf("]");
            if (at == -1 || at2 <= at)
            {
                WriteLine(text, color);
                return;
            }

            while (true)
            {
                var match = Regex.Match(text,"\\[.*?\\].*?\\[/.*?\\]");
                if (match.Length < 1)
                {
                    Write(text, color);
                    break;
                }

                // write up to expression
                Write(text.Substring(0, match.Index), color);

                // strip out the expression
                string highlightText = StringUtils.ExtractString(text, "]", "[");
                string colorVal = StringUtils.ExtractString(text, "[", "]");

                Write(highlightText, colorVal);

                // remainder of string
                text = text.Substring(match.Index + match.Value.Length);
            }

            Console.WriteLine();
        }
    }
}

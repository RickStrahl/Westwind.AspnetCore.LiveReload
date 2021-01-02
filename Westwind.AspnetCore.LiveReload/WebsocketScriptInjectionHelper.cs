using System;
using System.IO;
using System.Reflection;
using System.Resources;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Westwind.AspNetCore.LiveReload;

namespace Westwind.AspNetCore.LiveReload
{

    /// <summary>
    /// Helper class that handles the HTML injection into
    /// a string or byte array.
    /// </summary>
    public static class WebsocketScriptInjectionHelper
    {
        private const string STR_WestWindMarker = "<!-- West Wind Live Reload -->";
        private const string STR_BodyMarker = "</body>";

        private static readonly byte[] _bodyBytes = Encoding.UTF8.GetBytes(STR_BodyMarker);
        private static readonly byte[] _markerBytes = Encoding.UTF8.GetBytes(STR_WestWindMarker);


        /// <summary>
        /// Injects WebSocket Refresh code into JavaScript document
        /// just above the `&lt;/body&gt;` tag.
        /// </summary>
        /// <param name="html"></param>
        /// <param name="context"></param>
        /// <returns></returns>
        public static string InjectLiveReloadScript(string html, HttpContext context)
        {
            if (html.Contains(STR_WestWindMarker))
                return html;

            string script = GetWebSocketClientJavaScript(context);
            html = html.Replace(STR_BodyMarker, script);

            return html;
        }

        /// <summary>
        /// Adds Live Reload WebSocket script into the page before the body tag.
        /// </summary>
        /// <param name="buffer"></param>
        /// <param name="offset"></param>
        /// <param name="count"></param>
        /// <param name="context"></param>
        /// <param name="baseStream">The raw Response Stream</param>
        /// <returns></returns>
        public static Task InjectLiveReloadScriptAsync(byte[] buffer, int offset, int count, HttpContext context, Stream baseStream)
        {
            Span<byte> currentBuffer = buffer;
            var curBuffer = currentBuffer.Slice(offset, count).ToArray();
            return InjectLiveReloadScriptAsync(curBuffer, context, baseStream);
        }

        /// <summary>
        /// Adds Live Reload WebSocket script into the page before the body tag.
        /// </summary>
        /// <param name="buffer"></param>
        /// <param name="context"></param>
        /// <param name="baseStream">The raw Response Stream</param>
        /// <returns></returns>
        public static async Task InjectLiveReloadScriptAsync(byte[] buffer, HttpContext context, Stream baseStream)
        {
            var index = buffer.LastIndexOf(_markerBytes);

            if (index > -1)
            {
                await baseStream.WriteAsync(buffer, 0, buffer.Length);
                return;
            }

            index = buffer.LastIndexOf(_bodyBytes);
            if (index == -1)
            {
                await baseStream.WriteAsync(buffer, 0, buffer.Length);
                return;
            }

            var endIndex = index + _bodyBytes.Length;

            // Write pre-marker buffer
            await baseStream.WriteAsync(buffer, 0, index - 1);


            // Write the injected script
            var scriptBytes = Encoding.UTF8.GetBytes(GetWebSocketClientJavaScript(context));
            await baseStream.WriteAsync(scriptBytes, 0, scriptBytes.Length);

            // Write the rest of the buffer/HTML doc
            await baseStream.WriteAsync(buffer, endIndex, buffer.Length - endIndex);
        }

        static int LastIndexOf<T>(this T[] array, T[] sought) where T : IEquatable<T> =>
            array.AsSpan().LastIndexOf(sought);

        private static string _ClientScriptString = string.Empty;

        public static string GetWebSocketClientJavaScript(HttpContext context, bool returnScriptOnly = false)
        {

            var config = LiveReloadConfiguration.Current;

            var host = context.Request.Host;
            string hostString;
            if (!string.IsNullOrEmpty(config.WebSocketHost))
                hostString = config.WebSocketHost + config.WebSocketUrl;
            else
            {
                var prefix = context.Request.IsHttps ? "wss" : "ws";
                hostString = $"{prefix}://{host.Host}:{host.Port}" + config.WebSocketUrl;
            }

            if (string.IsNullOrEmpty(_ClientScriptString))
            {
                // Load `/LiveReloadClientScript.js from resource stream
                lock (_ClientScriptString)
                {
                    if (string.IsNullOrEmpty(_ClientScriptString))
                    {
                        using (var scriptStream = Assembly.GetExecutingAssembly()
                            .GetManifestResourceStream("Westwind.AspNetCore.LiveReload.LiveReloadClientScript.js"))
                        {
                            if (scriptStream == null)
                                throw new InvalidDataException("Unable to load LiveReloadClientScript.js Resource");

                            var buffer = new byte[scriptStream.Length];
                            scriptStream.Read(buffer, 0, buffer.Length);
                            _ClientScriptString = System.Text.Encoding.UTF8.GetString(buffer);
                        }
                    }
                }
            }

            if (returnScriptOnly)
                return _ClientScriptString.Replace("{0}", hostString);

            // otherwise return the embeddable script block string that replaces the ending </body> tag

            var script = $@"
<!-- West Wind Live Reload -->
";

            if (string.IsNullOrEmpty(config.LiveReloadScriptUrl))
                script += "<script>\n" + _ClientScriptString.Replace("{0}", hostString) + "\n</script>";
            else
                script += $"<script src=\"{config.LiveReloadScriptUrl}\"></script>";

script += @"
<!-- End Live Reload -->

</body>";

            return script;
        }

    }
}

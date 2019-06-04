using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Westwind.AspNetCore.LiveReload;

namespace Westwind.AspnetCore.LiveReload
{

    /// <summary>
    /// Helper class that handles the HTML injection into
    /// a string or byte array.
    /// </summary>
    public static class WebsocketScriptInjectionHelper
    {
        private const string STR_WestWindMarker = "<!-- West Wind Live Reload -->";
        private const string STR_BodyMarker = "</body>";

        private static readonly Memory<byte> _bodySpan = Encoding.UTF8.GetBytes(STR_BodyMarker);
        private static Memory<byte> _markerSpan = Encoding.UTF8.GetBytes(STR_WestWindMarker);


        /// <summary>
        /// Injects WebSocket Refresh code into JavaScript document
        /// just above the `</body>` tag.
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
        /// <param name="context"></param>
        /// <returns></returns>
        public static byte[] InjectLiveReloadScript(byte[] buffer, HttpContext context)
        {
            Span<byte> spanBuffer = buffer;

            var index = spanBuffer.LastIndexOf(_markerSpan.ToArray());
            if (index > -1)
                return buffer;

            index = spanBuffer.LastIndexOf(_bodySpan.ToArray());
            if (index == -1)
                return buffer;

            var endIndex = index + _bodySpan.Length;

            string script = GetWebSocketClientJavaScript(context);

            Span<byte> scriptBytes = Encoding.UTF8.GetBytes(script);

            using (var ms = new MemoryStream(buffer.Length + scriptBytes.Length))
            {
                ms.Write(buffer, 0, index - 1);
                ms.Write(scriptBytes.ToArray(), 0, scriptBytes.Length);
                ms.Write(buffer, endIndex, buffer.Length - endIndex);

                return ms.ToArray();
            }
        }

        /// <summary>
        /// Adds Live Reload WebSocket script into the page before the body tag.
        /// </summary>
        /// <param name="buffer"></param>
        /// <param name="offset"></param>
        /// <param name="count"></param>
        /// <param name="context"></param>
        /// <returns></returns>
        public static byte[] InjectLiveReloadScript(byte[] buffer, int offset, int count, HttpContext context)
        {
            Span<byte> currentBuffer = buffer;
            var curBuffer = currentBuffer.Slice(offset, count).ToArray();
            return InjectLiveReloadScript(curBuffer, context);
        }


        public static string GetWebSocketClientJavaScript(HttpContext context)
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

            string script = $@"
<!-- West Wind Live Reload -->
<script>
(function() {{

var retry = 0;
var connection = tryConnect();

function tryConnect(){{
    try{{
        var host = '{hostString}';
        connection = new WebSocket(host); 
    }}
    catch(ex) {{ console.log(ex); retryConnection(); }}

    if (!connection)
       return null;

    clearInterval(retry);

    connection.onmessage = function(message) 
    {{ 
        if (message.data == 'DelayRefresh') {{
                    alert('Live Reload Delayed Reload.');
            setTimeout( function() {{ location.reload(true); }},{config.ServerRefreshTimeout});
                }}
        if (message.data == 'Refresh') 
          location.reload(true); 
    }}    
    connection.onerror = function(event)  {{
        console.log('Live Reload Socket error.', event);
        retryConnection();
    }}
    connection.onclose = function(event) {{
        console.log('Live Reload Socket closed.');
        retryConnection();
    }}

    console.log('Live Reload socket connected.');
    return connection;  
}}
function retryConnection() {{   
   retry = setInterval(function() {{ 
                console.log('Live Reload retrying connection.'); 
                connection = tryConnect();  
                if(connection) location.reload(true);                    
            }},{config.ServerRefreshTimeout});
}}

}})();
</script>
<!-- End Live Reload -->

</body>";
            return script;
        }

    }
}

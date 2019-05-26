using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;

namespace Westwind.AspNetCore.LiveReload
{
    public class LiveReloadMiddleware
    {
        private readonly RequestDelegate _next;
        internal static HashSet<WebSocket> ActiveSockets = new HashSet<WebSocket>();

       
        public LiveReloadMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            var config = LiveReloadConfiguration.Current;
            if (!config.LiveReloadEnabled)
            {
                await _next(context);
                return;
            }

            // Handle WebSocket Connection
            if (context.Request.Path ==  config.WebSocketUrl)
            {
                if (context.WebSockets.IsWebSocketRequest)
                {
                    WebSocket webSocket = await context.WebSockets.AcceptWebSocketAsync();
                    if (!ActiveSockets.Contains(webSocket))
                        ActiveSockets.Add(webSocket);

                    await WebSocketWaitLoop(webSocket); // this waits until done
                }
                else
                {
                    context.Response.StatusCode = 400;
                }

                return;
            }

            // Inject Refresh JavaScript Into HTML content
            var existingBody = context.Response.Body;

            using (var newContent = new MemoryStream(2000))
            {
                context.Response.Body = newContent;

                await _next(context);

                // Inject Script into HTML content
                if (context.Response.ContentType != null &&
                    context.Response.ContentType.Contains("text/html", StringComparison.InvariantCultureIgnoreCase))

                {
                    string html = Encoding.UTF8.GetString(newContent.ToArray());
                    html = InjectLiveReloadScript(html, context);

                    context.Response.Body = existingBody;

                    // Send our modified content to the response body.
                    await context.Response.WriteAsync(html);
                }
                else
                {
                    // bypass - return raw output
                    context.Response.Body = existingBody;
                    context.Response.Body.Write(newContent.ToArray());
                }
            }
        }



        public string InjectLiveReloadScript(string html, HttpContext context)
        {
            var config = LiveReloadConfiguration.Current;

            var Request = context.Request;
            
            if (html.Contains("<!-- West Wind Live Reload -->")) return html;

            var prefix = "ws";
            if (context.Request.IsHttps)
                prefix = "wss";

            var host = $"{prefix}://{context.Request.Host.Host}:{context.Request.Host.Port}" + config.WebSocketUrl;

            string script = $@"
<!-- West Wind Live Reload -->
<script>
(function() {{

var retry = 0;
var connection = tryConnect();

function tryConnect(){{
    try{{
        var host = '{host}';
        var connection = new WebSocket(host); 
    }}
    catch(ex) {{ retry(); }}

    if (!connection)
       return null;

    clearInterval(retry);

    connection.onmessage = function(message) 
    {{ 
        if (message.data == 'DelayRefresh') {{
                    alert('Live Reload Delayed Reload.');
            setTimeout( function() {{ location.reload(); }},{config.ServerRefreshTimeout});
                }}
        if (message.data == 'Refresh') 
          location.reload(); 
    }}    
    connection.onerror = function(event)  {{
        console.log('Live Reload Socket error.');
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
                if(connection) location.reload();                    
            }},{config.ServerRefreshTimeout});
}}

}})();
</script>
<!-- End Live Reload -->

</body>";

            html = html.Replace("</body>", script);

            return html;
        }


        /// <summary>
        ///  Web Socket event loop. Just sits and waits
        /// for disconnection or error to break.
        /// </summary>
        /// <param name="webSocket"></param>
        /// <returns></returns>
        private async Task WebSocketWaitLoop(WebSocket webSocket)
        {
            // File Watcher was started by Middleware extensions

            var buffer = new byte[1024];
            while (webSocket.State.HasFlag(WebSocketState.Open))
            {
                try
                {
                    var received = await webSocket.ReceiveAsync(buffer, CancellationToken.None);
                }
                catch
                {
                    break;
                }
            }

            ActiveSockets.Remove(webSocket);
            await webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Socket closed", CancellationToken.None);
        }

        /// <summary>
        /// Static method that can be called from code to force
        /// the browser to refresh itself.
        ///
        /// Use Delayed refresh for server code refreshes that
        /// are slow to refresh due to restart
        /// </summary>
        /// <param name="delayed"></param>
        /// <returns></returns>
        public static async Task RefreshWebSocketRequest(bool delayed = false)
        {
            string msg = "Refresh";
            if (delayed)
                msg = "DelayRefresh";

            byte[] refresh = Encoding.UTF8.GetBytes(msg);
            foreach (var sock in ActiveSockets)
            {
                await sock.SendAsync(new ArraySegment<byte>(refresh, 0, refresh.Length),
                    WebSocketMessageType.Text,
                    true,
                    CancellationToken.None);
            }
        }
    }
}
 
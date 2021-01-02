using System;
using System.Collections.Concurrent;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Westwind.AspNetCore.LiveReload;
using Microsoft.Extensions.Logging;

using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.Hosting;

namespace Westwind.AspNetCore.LiveReload
{
    /// <summary>
    /// Live Reload middleware routes WebSocket Server requests
    /// for the Live Reload push to connected browsers and handles
    /// injecting WebSocket client JavaScript into any HTML content.
    /// </summary>
    public class LiveReloadMiddleware
    {
        private readonly RequestDelegate _next;

        /// <summary>
        /// Concurrent dictionary as a Hashset. The byte is just a throwaway value
        /// </summary>
        internal static ConcurrentDictionary<WebSocket,byte> ActiveSockets { get; }= new ConcurrentDictionary<WebSocket,byte>();

        private IHostApplicationLifetime applicationLifetime = null;

        public LiveReloadMiddleware(RequestDelegate next,IHostApplicationLifetime lifeTime)
        {
            applicationLifetime = lifeTime;
            _next = next;
        }

        /// <summary>
        /// Routes to WebSocket handler and injects javascript into
        /// HTML content
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>

        public async Task InvokeAsync(HttpContext context)
        {
            var config = LiveReloadConfiguration.Current;
            if (!config.LiveReloadEnabled)
            {
                await _next(context);
                return;
            }

            // serve live reload script
            if(await HandleServeLiveReloadScript(context))
                return;

            // see if we have a WebSocket request. True means we handled
            if (await HandleWebSocketRequest(context))
                return;

            // Check other content for HTML
            await HandleHtmlInjection(context);
        }

        public async Task<bool> HandleServeLiveReloadScript(HttpContext context)
        {
            if (context.Request.Path == LiveReloadConfiguration.Current.LiveReloadScriptUrl)
            {
                context.Response.ContentType = "text/javascript";
                await context.Response.WriteAsync(WebsocketScriptInjectionHelper.GetWebSocketClientJavaScript(context, true));
                return true;
            }

            return false;
        }

        /// <summary>
        /// Inspects all non WebSocket content for HTML documents
        /// and if it finds HTML injects the JavaScript needed to
        /// refresh the browser via Web Sockets.
        ///
        /// Uses a wrapper stream to wrap the response and examine
        /// only text/html requests - other content is passed through
        /// as is.
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        private async Task HandleHtmlInjection(HttpContext context)
        {
            var path = context.Request.Path.Value;

            // Use a custom StreamWrapper to rewrite output on Write/WriteAsync
            using (var filteredResponse = new ResponseStreamWrapper(context.Response.Body, context))
            {
                // Use new IHttpResponseBodyFeature for abstractions of pilelines/streams etc.
                // For 3.x this works reliably while direct Response.Body was causing random HTTP failures
                context.Features.Set<IHttpResponseBodyFeature>(new StreamResponseBodyFeature(filteredResponse));

                await _next(context);
            }

        }


        /// <summary>
        /// Checks for WebService Requests and if it is routes it to the
        /// WebSocket handler event loop.
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        private async Task<bool> HandleWebSocketRequest(HttpContext context)
        {
            var config = LiveReloadConfiguration.Current;

            // Handle WebSocket Connection
            if (context.Request.Path == config.WebSocketUrl)
            {
                if (context.WebSockets.IsWebSocketRequest)
                {
                    using (var webSocket = await context.WebSockets.AcceptWebSocketAsync())
                    {

                        if (!ActiveSockets.ContainsKey(webSocket))
                            ActiveSockets.TryAdd(webSocket, 0);

                        await WebSocketWaitLoop(webSocket); // this waits until done
                    }
                }
                else
                {
                    context.Response.StatusCode = 400;
                }

                return true;
            }

            return false;
        }


        /// <summary>
        ///  Web Socket event loop. Just sits and waits
        /// for disconnection or error to break.
        /// </summary>
        /// <param name="webSocket">The Web Socekt to wait on</param>
        /// <returns></returns>
        private async Task WebSocketWaitLoop(WebSocket webSocket)
        {
            // File Watcher was started by Middleware extensions
            var buffer = new byte[1024];
            while (webSocket.State.HasFlag(WebSocketState.Open))
            {
                try
                {
                    var received =
                        await webSocket.ReceiveAsync(buffer, applicationLifetime.ApplicationStopping);
                }
                catch
                {
                    break;
                }
            }

            ActiveSockets.TryRemove(webSocket,out byte throwAway);

            if (webSocket.State != WebSocketState.Closed &&
                webSocket.State != WebSocketState.Aborted)
            {
                try
                {
                    await webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure,
                        "Socket closed",
                        applicationLifetime.ApplicationStopping);
                }
                catch
                {
                    // this may throw on shutdown and can be ignored
                }
            }

        }

        /// <summary>
        /// Static method that can be called from code to force
        /// the browser to refresh itself.
        /// 
        /// Use Delayed refresh for server code refreshes that
        /// are slow to refresh due to restart
        /// </summary>
        /// <param name="delayed">If true will use the configuration delay configured to refresh the browser</param>
        /// <param name="logger">Logger instance so we can track socket send errors</param>
        /// <returns></returns>
        public static async Task RefreshWebSocketRequest(bool delayed = false, ILogger logger = null)
        {
            string msg = "Refresh";
            if (delayed)
                msg = "DelayRefresh";

            byte[] refresh = Encoding.UTF8.GetBytes(msg);
            foreach (var kv in ActiveSockets)
            {
                try
                {
                    // key is the webSocket
                    await kv.Key.SendAsync(new ArraySegment<byte>(refresh, 0, refresh.Length),
                        WebSocketMessageType.Text,
                        true,
                        CancellationToken.None);
                }
                catch(Exception ex)
                {
                    logger?.LogWarning($"LiveReload refresh failed: {ex.Message}");
                }
            }
        }

    }
}

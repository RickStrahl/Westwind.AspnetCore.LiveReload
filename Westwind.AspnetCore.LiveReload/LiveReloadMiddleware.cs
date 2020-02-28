﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using Westwind.AspnetCore.LiveReload;

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
        private readonly IOptionsMonitor<LiveReloadConfiguration> _configuration;
        internal static HashSet<WebSocket> ActiveSockets = new HashSet<WebSocket>();


        public LiveReloadMiddleware(RequestDelegate next, IOptionsMonitor<LiveReloadConfiguration> configuration)
        {
            _next = next;
            _configuration = configuration;
        }


        /// <summary>
        /// Routes to WebSocket handler and injects javascript into
        /// HTML content
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>

        public async Task InvokeAsync(HttpContext context)
        {
            var config = _configuration.CurrentValue;
            if (!config.LiveReloadEnabled)
            {
                await _next(context);
                return;
            }

            // see if we have a WebSocket request. True means we handled
            if (await HandleWebSocketRequest(context))
                return;

            // Check other content for HTML
            await HandleHtmlInjection(context);
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
            using (var filteredResponse = new ResponseStreamWrapper(context.Response.Body, context))
            {
                context.Response.Body = filteredResponse;
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
            var config = _configuration.CurrentValue;

            // Handle WebSocket Connection
            if (context.Request.Path == config.WebSocketUrl)
            {
                if (context.WebSockets.IsWebSocketRequest)
                {
                    using (var webSocket = await context.WebSockets.AcceptWebSocketAsync())
                    {
                        if (!ActiveSockets.Contains(webSocket))
                            ActiveSockets.Add(webSocket);

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
            if (webSocket.State != WebSocketState.Closed &&
                webSocket.State != WebSocketState.Aborted)
                await webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Socket closed",
                                           CancellationToken.None);

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

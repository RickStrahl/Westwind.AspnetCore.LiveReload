using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Westwind.AspNetCore.LiveReload
{
    public class LiveReloadConfiguration
    {
        /// <summary>
        /// Current configuration instance accessible through out the middleware
        /// </summary>
        public static LiveReloadConfiguration Current { get; set; }


        /// <summary>
        /// Determines whether live reload is enabled. If off, there's no
        /// overhead in this middleware and it simply passes through requests.
        /// </summary>
        public bool LiveReloadEnabled { get; set; } = true;

        /// <summary>
        /// Optional - the folder to monitor for file changes. By default
        /// this value is set to the Web application root folder (ContentRootPath)
        /// </summary>
        public string FolderToMonitor { get; set; }


        /// <summary>
        /// Comma delimited list of file extensions that the file watcher
        /// responds to.
        ///
        /// Note the `.live` extension which is used for server restarts. Please
        /// make sure you always add that to your list or server reloads won't work.
        /// </summary>
        public string ClientFileExtensions { get; set; } = ".cshtml,.css,.js,.htm,.html,.ts,.razor";

        /// <summary>
        /// Optional filter for the given paths. If used, it should return true if the file
        /// should be watched, false otherwise.
        /// </summary>
        public Func<string, bool> FileIncludeFilter {get; set; }= null;


        /// <summary>
        /// The timeout to wait before refreshing the page when shutting down
        /// </summary>
        public int ServerRefreshTimeout { get; set; } = 3000;


        /// <summary>
        /// The URL used for the Web Socket connection on the page to refresh
        /// </summary>
        public string WebSocketUrl { get; set; } = "/__livereload";

        /// <summary>
        /// Optional WebSocket host. Use this if you are on an Https2 connection
        /// to point at a http connection. "ws://localhost:5000"
        /// </summary>
        public string WebSocketHost { get; set; }

        /// <summary>
        /// Optionally passed in token to cancel out of pending Web Socket
        /// wait operations in order to allow for a controlled shutdown.
        ///
        /// Provide a cancellation token from Shutdown services or a hosted
        /// service.
        /// </summary>
        public CancellationToken ShutdownCancellationToken {get; set;}



    }
}

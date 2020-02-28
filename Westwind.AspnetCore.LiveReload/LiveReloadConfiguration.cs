﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Westwind.AspNetCore.LiveReload
{
    public class LiveReloadConfiguration
    {
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
        /// The timeout to wait before refreshing the page when shutting down
        /// </summary>
        public int ServerRefreshTimeout { get; set; } = 3000;

        
        /// <summary>
        /// The URL used for the Web Socket connection on the page to refresh
        /// </summary>
        public string WebSocketUrl { get; set; } = "/__livereload";

        /// <summary>
        /// Optional WebSocket host. Use this if you are on an Https2 connection
        /// to point at a http1 connection. "ws://localhost:5000"
        /// </summary>
        public string WebSocketHost { get; set; }

    }
}

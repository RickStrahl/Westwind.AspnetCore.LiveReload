using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Extensions.Configuration;

namespace LiveReloadServer
{
    public class LiveReloadServerConfiguration //: LiveReloadConfiguration
    {

        public static LiveReloadServerConfiguration Current;

        /// <summary>
        /// The WebRoot folder that is used to serve Web content from.
        /// If not specified the launching folder is used.
        /// </summary>
        public string WebRoot { get; set; }


        /// <summary>
        /// The port that the server is bound to. Defaults to 5200
        /// </summary>
        public int Port { get; set; } = 5200;

        /// <summary>
        /// The IP or Host address to bind the server to. By default uses localhost
        /// which is **local only**. To bind to a publicly exposed IP address
        /// use an explicit IP address or `0.0.0.0`
        /// </summary>
        public string Host { get; set; } = "localhost";


        /// <summary>
        /// Determines whether the server is launched with SSL support
        /// </summary>
        public bool UseSsl { get; set; } = false;


        /// <summary>
        /// Determines whether pages are refreshed when a change is made to any of the
        /// monitored file extensions set in `ClientFileExtensions`.
        /// </summary>
        public bool UseLiveReload { get; set; } = true;

        /// <summary>
        /// Extensions monitored for live reload
        /// </summary>
        public string Extensions { get; set; } = ".cshtml,.css,.js,.htm,.html,.ts,.razor";

        /// <summary>
        /// Determines whether Razor pages are executed. Razor pages must
        /// be self contained without 'code behind models'.
        /// </summary>
        public bool UseRazor { get; set; } = false;


        /// <summary>
        /// Default page files to redirect to in extensionless URLs if the files exist in folder
        /// </summary>
        public string DefaultFiles { get; set; } = "index.html,default.htm";

        /// <summary>
        /// Determines whether the browser is opened
        /// </summary>
        public bool OpenBrowser { get; set; } = true;

        /// <summary>
        /// Determines whether the server console window shows the URLs
        /// </summary>
        public bool ShowUrls {get; set; } = true;


        #region Markdown Settings

        /// <summary>
        /// Determines whether Markdown processing is enabled
        /// </summary>
        public bool UseMarkdown { get; set; } = false;

        /// <summary>
        /// When set copies the Markdown Resources into the
        /// </summary>
        public bool CopyMarkdownResources { get; set; } = false;

        public string MarkdownTemplate { get; set; } = "~/markdown-themes/__MarkdownPageTemplate.cshtml";

        /// <summary>
        /// Page theme used by the provide Markdown page wrapper themes:
        /// github*|dharkan|medium|blackout|westwind
        ///
        /// Themes can be customized by creating a new folder in the
        /// </summary>
        public string MarkdownTheme { get; set; } = "github";

        /// <summary>
        /// Theme used for source code syntax coloring.
        /// github*|dharkan|medium|blackout|westwind
        /// </summary>
        public string MarkdownSyntaxTheme { get; set; } = "github";

        #endregion

        #region Server Settings

        /// <summary>
        /// Allows you to set additional Mime mappings for use in the LiveReload Server
        /// Console application. Format:
        /// ".dll"    - "application/octet-stream"
        /// ".custom" - "text/plain"
        /// </summary>
        public Dictionary<string,string> AdditionalMimeMappings {get; set; }


        /// <summary>
        /// An optional fallback path that's used in the LiveReloadServer
        /// to redirect to another URL if a folder isn't found on the server.
        /// Used for SPA application that have client navigation URLS and
        /// need to redirect back a starting page.
        /// Example: "/index.html"
        /// </summary>
        public string FolderNotFoundFallbackPath { get; set; }

        #endregion

        /// <summary>
        /// Error message set when load fails
        /// </summary>
        public string ErrorMessage = null;

        /// <summary>
        /// Loads and fixes up configuration values from the configuraton.
        /// </summary>
        /// <remarks>
        /// Note we're custom loading this to allow for not overly string command line syntax
        /// </remarks>
        /// <param name="Configuration">.NET Core Configuration Provider</param>
        public bool LoadFromConfiguration(IConfiguration Configuration)
        {
            Current = this;

            WebRoot = Configuration["WebRoot"];
            if (string.IsNullOrEmpty(WebRoot))
                WebRoot = Environment.CurrentDirectory;
            else
                WebRoot = Path.GetFullPath(WebRoot, Environment.CurrentDirectory);

            Port = Helpers.GetIntegerSetting("Port", Configuration, Port);
            UseSsl = Helpers.GetLogicalSetting("UseSsl", Configuration, UseSsl);
            Host = Helpers.GetStringSetting("Host", Configuration, Host);
            DefaultFiles = Helpers.GetStringSetting("DefaultFiles", Configuration, DefaultFiles);
            Extensions = Helpers.GetStringSetting("Extensions", Configuration, Extensions);


            UseLiveReload = Helpers.GetLogicalSetting("UseLiveReload", Configuration, UseLiveReload);
            UseRazor = Helpers.GetLogicalSetting("UseRazor", Configuration);
            ShowUrls = Helpers.GetLogicalSetting("ShowUrls", Configuration, ShowUrls);
            OpenBrowser = Helpers.GetLogicalSetting("OpenBrowser", Configuration, OpenBrowser);

            FolderNotFoundFallbackPath = Helpers.GetStringSetting("FolderNotFoundFallbackPath",Configuration,null);

            // Enables Markdown Middleware and optionally copies Markdown Templates into output folder
            UseMarkdown = Helpers.GetLogicalSetting("UseMarkdown", Configuration, false);
            if (UseMarkdown)
            {
                // defaults to true but only if Markdown is enabled!
                CopyMarkdownResources = Helpers.GetLogicalSetting("CopyMarkdownResources", Configuration, CopyMarkdownResources);
            }
            MarkdownTemplate = Helpers.GetStringSetting("MarkdownTemplate", Configuration, MarkdownTemplate);
            MarkdownTheme = Helpers.GetStringSetting("MarkdownTheme", Configuration, MarkdownTheme);
            MarkdownSyntaxTheme = Helpers.GetStringSetting("MarkdownSyntaxTheme", Configuration, MarkdownSyntaxTheme);

            return true;
        }

        /// <summary>
        /// Returns the Host displayname for a browser URL. Fixes up local
        /// host names of `127.0.0.1` and `0.0.0.0` and `*` as `localhost`
        /// </summary>
        /// <param name="host"></param>
        /// <returns></returns>
        public string GetHostName(string host = null)
        {
            if(host == null)
                host = Host;

            if (string.IsNullOrEmpty(host) || host == "0.0.0.0" || host == "127.0.0.1" || host == "*")
                return "localhost";

            return host;
        }

        /// <summary>
        /// Retrieves the adjusted HTTP URL - adjusted for localhost for
        /// local urls
        /// </summary>
        /// <returns></returns>
        public string GetHttpUrl(bool noHostNameTranslation = false)
        {
            var hostName = Host;
            if (!noHostNameTranslation)
                hostName = GetHostName();

            return $"http{(UseSsl ? "s" : "")}://{hostName}:{Port}";
        }




    }
}

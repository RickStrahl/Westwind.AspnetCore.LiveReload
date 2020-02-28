using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Options;
using Westwind.AspNetCore.LiveReload;

namespace Westwind.AspnetCore.LiveReload
{
    /// <summary>
    /// The MS Options system will call this in order to initialize a bunch of the settings that need access
    /// to other objects that are registered in DI.
    ///
    /// This is a *POST* Configure so it happens after all the other user configuration in order to clean up
    /// for users and handle any options that they may have left at the default.
    /// </summary>
    /// <seealso cref="Microsoft.Extensions.Options.IPostConfigureOptions{Westwind.AspNetCore.LiveReload.LiveReloadConfiguration}" />
    internal class PostConfigureLiveReloadConfiguration : IPostConfigureOptions<LiveReloadConfiguration>
    {
#if NETCORE2
        private readonly IHostingEnvironment _environment;
#else
        private readonly IWebHostEnvironment _environment;
#endif

        public PostConfigureLiveReloadConfiguration(
#if NETCORE2
            IHostingEnvironment environment
#else
            IWebHostEnvironment environment
#endif
        )
        {
            _environment = environment;
        }

        public void PostConfigure(string name, LiveReloadConfiguration options)
        {
            if (string.IsNullOrEmpty(options.FolderToMonitor))
            {
                options.FolderToMonitor = _environment.ContentRootPath;
            }

            else if (options.FolderToMonitor.StartsWith("~"))
            {
                if (options.FolderToMonitor.Length > 1)
                {
                    var folder = options.FolderToMonitor.Substring(1);
                    if (folder.StartsWith('/') || folder.StartsWith("\\"))
                        folder = folder.Substring(1);
                    options.FolderToMonitor = Path.Combine(_environment.ContentRootPath, folder);
                    options.FolderToMonitor = Path.GetFullPath(options.FolderToMonitor);
                }
                else
                {
                    options.FolderToMonitor = _environment.ContentRootPath;
                }
            }
        }
    }
}

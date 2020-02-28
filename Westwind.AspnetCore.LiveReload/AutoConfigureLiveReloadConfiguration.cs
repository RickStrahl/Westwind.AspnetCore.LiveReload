using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Westwind.AspNetCore.LiveReload;

namespace Westwind.AspnetCore.LiveReload
{
    /// <summary>
    /// The MS Options system will call this to automatically initialize the configuration from the configuration file.
    /// This is one of the "magic" pieces of glue that allows pulling from DI and configuring nicely during the AddXX
    /// function call.
    /// </summary>
    internal class AutoConfigureLiveReloadConfiguration : IConfigureOptions<LiveReloadConfiguration>
    {
        private readonly IConfiguration _configuration;

        public AutoConfigureLiveReloadConfiguration(
            IConfiguration configuration
        )
        {
            _configuration = configuration;
        }

        /// <summary>
        /// This gets called during the standard configuration process in order to automatically wire up the configuration settings
        /// to the <c>LiveReload</c> configuration section.
        /// </summary>
        /// <param name="options">The options instance to configure.</param>
        public void Configure(LiveReloadConfiguration options)
        {
            _configuration.Bind("LiveReload", options);
        }
    }
}

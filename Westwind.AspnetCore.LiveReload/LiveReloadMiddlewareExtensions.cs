using System;
using System.IO;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using Westwind.AspnetCore.LiveReload;

namespace Westwind.AspNetCore.LiveReload
{
    /// <summary>
    /// The Middleware Hookup extensions.
    /// </summary>
    public static class LiveReloadMiddlewareExtensions
    {
        /// <summary>
        /// Bypass the automatic configuration that uses the <c>LiveReload</c> section of the configuration file.
        /// </summary>
        /// <param name="services"></param>
        /// <param name="configuration">The selected configuration section to use typically selected via <c>Configuration.GetSection("Section:Name")</c>.</param>
        /// <param name="configAction">An optional custom configuration action you would like to occur in order to further configure the settings.</param>
        public static IServiceCollection AddLiveReload(this IServiceCollection services, IConfiguration configuration, Action<LiveReloadConfiguration> configAction = null)
        {
            // Conveniently allow the user to select their own section in the configuration file to use
            services.Configure<LiveReloadConfiguration>(configuration);

            if (!(configAction is null)) services.Configure(configAction);

            // Make sure that the post configure is registered (don't register it twice to avoid problems)
            services.TryAddTransient<IPostConfigureOptions<LiveReloadConfiguration>, PostConfigureLiveReloadConfiguration>();
            // Due to how Middleware works in ASP.NET Core you cannot register middleware this way
            // They are "magically" present when the .UseMiddleware() happens since the middleware method needs to
            // resolve dependencies from DI in a way that is abnormal for normal DI. RequestDelegate in the constructor
            // isn't actually resolved from DI, its just ASP.NET Core magic making it seem like everything is DI.
            //services.TryAddSingleton<LiveReloadMiddleware>();

            return services;
        }

        /// <summary>
        /// Configure the MarkdownPageProcessor in Startup.ConfigureServices.
        /// </summary>
        /// <param name="services"></param>
        /// <param name="configAction"></param>
        /// <returns></returns>
        public static IServiceCollection AddLiveReload(this IServiceCollection services, Action<LiveReloadConfiguration> configAction = null)
        {
            // This does the automatic configuration from IConfiguration
            services.TryAddTransient<IConfigureOptions<LiveReloadConfiguration>, AutoConfigureLiveReloadConfiguration>();

            // This gives an extra configuration ability to users
            if (!(configAction is null)) services.Configure(configAction);

            // Make sure that the post configure is registered (don't register it twice to avoid problems)
            services.TryAddTransient<IPostConfigureOptions<LiveReloadConfiguration>, PostConfigureLiveReloadConfiguration>();
            // Due to how Middleware works in ASP.NET Core you cannot register middleware this way
            // They are "magically" present when the .UseMiddleware() happens since the middleware method needs to
            // resolve dependencies from DI in a way that is abnormal for normal DI. RequestDelegate in the constructor
            // isn't actually resolved from DI, its just ASP.NET Core magic making it seem like everything is DI.
            //services.TryAddSingleton<LiveReloadMiddleware>();

            return services;
        }

        /// <summary>
        /// Hook up the Markdown Page Processing functionality in the Startup.Configure method
        /// </summary>
        /// <param name="builder"></param>
        /// <returns></returns>
        public static IApplicationBuilder UseLiveReload(this IApplicationBuilder builder)
        {
            var configuration = builder.ApplicationServices.GetRequiredService<IOptionsMonitor<LiveReloadConfiguration>>();

            builder.UseWhen(
                // Decide if the middleware should activate
                (_) => configuration.CurrentValue.LiveReloadEnabled,
                (subAppBuilder) =>
                {
                    var webSocketOptions = new WebSocketOptions()
                    {
                        KeepAliveInterval = TimeSpan.FromSeconds(240),
                        ReceiveBufferSize = 256
                    };
                    subAppBuilder.UseWebSockets(webSocketOptions);

                    subAppBuilder.UseMiddleware<LiveReloadMiddleware>();

                    LiveReloadFileWatcher.StartFileWatcher(configuration);

                    // this isn't necessary as the browser disconnects and on reconnect refreshes
                    //LiveReloadMiddleware.RefreshWebSocketRequest();
                }
            );

            return builder;
        }

    }
}

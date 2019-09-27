using System;
using System.Linq;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

namespace LiveReloadServer
{
    public class Program
    {
        public static IHost WebHost;

        public static void Main(string[] args)
        {
            try
            {
                var builder = CreateHostBuilder(args);
                if (builder == null)
                    return;

                WebHost = builder.Build();
                WebHost.Run();
            }
            catch (Exception ex)
            {
                Console.Clear();
                Console.WriteLine("---------------------------------------------------------------------------");
                Console.WriteLine("Live Reload Server");
                Console.WriteLine("---------------------------------------------------------------------------");
                Console.WriteLine("Unable to start the Web Server...");
                Console.WriteLine("Most likely this means the port is already in use by another application.");
                Console.WriteLine("Please try and choose another port with the `--port` switch. And try again.");
                Console.WriteLine("\r\n\r\nException Info:");
                Console.WriteLine(ex.Message);
                Console.WriteLine("---------------------------------------------------------------------------");
            }
        }





        public static IHostBuilder CreateHostBuilder(string[] args)
        {
            // Custom Config
            var config = new ConfigurationBuilder()
                .AddJsonFile("LiveReloadServer.json", optional: true)
                .AddEnvironmentVariables("LiveReloadServer_")
                .AddCommandLine(args)
                .Build();


            if (args.Contains("--help", StringComparer.InvariantCultureIgnoreCase) ||
                args.Contains("/h") || args.Contains("-h"))
            {
                ShowHelp();
                return null;
            }

            return Host.CreateDefaultBuilder(args)
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder
                        .UseConfiguration(config);

                    string sport = config["Port"];
                    bool useSsl = config["UseSsl"].Equals("true",StringComparison.InvariantCultureIgnoreCase);
                    int.TryParse(sport, out int port);
                    if (port == 0)
                        port = 5000;

                    webBuilder.UseUrls($"http{(useSsl ? "s" : "")}://0.0.0.0:{port}");

                    webBuilder
                        .UseStartup<Startup>();
                });
        }


        static void ShowHelp()
        {
            Console.WriteLine(@"
Live Reload Server
------------------
(c) Rick Strahl, West Wind Technologies, 2019

Static and Razor File Service with Live Reload for changed content.

Syntax:
-------
LiveReloadServer  <options>

Commandline options (optional):

--WebRoot            <path>  (current Path if not provided)
--Port               5200*
--UseSsl             True|False*
--LiveReloadEnabled  True*|False
--ShowUrls           True|False*
--OpenBrowser        True*|False
--DefaultFiles       ""index.html,default.htm,default.html""

Live Reload options:

--LiveReload.ClientFileExtensions   "".cshtml,.css,.js,.htm,.html,.ts""
--LiveReload ServerRefreshTimeout   3000,
--LiveReload.WebSocketUrl           ""/__livereload""

Configuration options can be specified in:

* Environment Variables with a `LiveReloadServer` prefix. Example: 'LiveReloadServer_Port'
* Command Line options as shown above

Examples:
---------
LiveReload --WebRoot ""c:\temp\My Site"" --port 5500 --useSsl false

$env:LiveReload_Port 5500
LiveReload
");
        }


        #region External Access

        public static void Start(string[] args)
        {
            var builder = CreateHostBuilder(args);
            if (builder == null)
                return;

            WebHost = builder.Build();
            WebHost.Start();
        }

        public static void Stop()
        {
            WebHost.StopAsync().GetAwaiter().GetResult();
        }
        #endregion
    }


}

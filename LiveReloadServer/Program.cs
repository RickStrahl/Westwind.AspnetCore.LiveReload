using System;
using System.Linq;
using System.Reflection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

namespace LiveReloadServer
{
    public class Program
    {

        public static IHost WebHost;
        public static string AppHeader;

        public static void Main(string[] args)
        {
            try
            {
                var version = Assembly.GetExecutingAssembly().GetName().Version;
                var ver = version.Major + "." + version.Minor +
                          (version.Build > 0 ? "." + version.Build : string.Empty);
                AppHeader = $"Live Reload Server v{ver}";

                var builder = CreateHostBuilder(args);
                if (builder == null)
                    return;

                WebHost = builder.Build();
                WebHost.Run();
            }
            catch (Exception ex)
            {
                // can't catch internal type
                if (ex.StackTrace.Contains("ThrowOperationCanceledException"))
                    return;

                string headerLine = new string('-', AppHeader.Length);
                //Console.Clear();
                Console.WriteLine(headerLine);
                Console.WriteLine(AppHeader);
                Console.WriteLine(headerLine);
                Console.WriteLine("Unable to start the Web Server...");
                Console.WriteLine("Most likely this means the port is already in use by another application.");
                Console.WriteLine("Please try and choose another port with the `--port` switch. And try again.");
                Console.WriteLine("\r\n\r\nException Info:");
                Console.WriteLine(ex.Message);
                Console.WriteLine("---");
                Console.WriteLine(ex.StackTrace);
                Console.WriteLine(ex.Source);
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
                    var webRoot = config["WebRoot"];
                    if (!string.IsNullOrEmpty(webRoot))
                        webBuilder.UseWebRoot(webRoot);

                    webBuilder
                        .UseConfiguration(config);

                    
                    string tSsl = config["UseSsl"];
                    bool useSsl = !string.IsNullOrEmpty(tSsl) && tSsl.Equals("true", StringComparison.InvariantCultureIgnoreCase);

                    string sport = config["Port"];
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

            string razorFlag = null;
#if USE_RAZORPAGES
            razorFlag = "\r\n--RazorEnabled       True|False";
#endif

            string headerLine = new string('-',AppHeader.Length);

            Console.WriteLine($@"
{headerLine}
{AppHeader}
{headerLine}
(c) Rick Strahl, West Wind Technologies, 2019

Static and Razor File Service with Live Reload for changed content.

Syntax:
-------
LiveReloadServer  <options>

Commandline options (optional):

--WebRoot            <path>  (current Path if not provided)
--Port               5200*
--UseSsl             True|False*
--LiveReloadEnabled  True*|False{razorFlag}
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

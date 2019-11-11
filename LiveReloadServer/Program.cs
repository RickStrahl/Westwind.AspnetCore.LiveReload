using System;
using System.IO;
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
                AppHeader = $"Live Reload Web Server v{ver}";


                if (args.Contains("--help", StringComparer.InvariantCultureIgnoreCase) ||
                    args.Contains("/h") || args.Contains("-h"))
                {
                    ShowHelp();
                    return;
                }

                var builder = CreateHostBuilder(args);
                if (builder == null)
                    return;

                WebHost = builder.Build();
                WebHost.Run();
            }
            catch (IOException ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("\r\nUnable to start the Web Server");
                Console.ResetColor();
                Console.WriteLine("------------------------------");
                
                
                Console.WriteLine("The server port is already in use by another application.");
                Console.WriteLine("Please try and choose another port with the `--port` switch. And try again.");
                Console.WriteLine("\r\n\r\nException Info:");
                Console.WriteLine(ex.Message);
                Console.WriteLine("---------------------------------------------------------------------------");
            }
            catch (Exception ex)
            {
                // can't catch internal type
                if (ex.StackTrace.Contains("ThrowOperationCanceledException"))
                    return;

                string headerLine = new string('-', AppHeader.Length);
                Console.WriteLine(headerLine);
                Console.WriteLine(AppHeader);
                Console.WriteLine(headerLine);

                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("\r\nYikes. That wasn't supposed to happen. Something went wrong!");
                Console.WriteLine();
                Console.ResetColor();

                Console.WriteLine("The Live Reload Server has run into a problem and has stopped working.");
                Console.WriteLine("Here's some more information...");
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
            

            return Host.CreateDefaultBuilder(args)
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    // Custom Config
                    var config = new ConfigurationBuilder()
                        .AddJsonFile("LiveReloadServer.json", optional: true)
                        .AddJsonFile("LiveReloadWebServer.json", optional: true)
                        .AddEnvironmentVariables("LIVERELOADSERVER_")
                        .AddEnvironmentVariables("LIVERELOADWEBSERVER_")
                        .AddCommandLine(args)
                        .Build();


                    webBuilder
                        .UseConfiguration(config);

                    var webRoot = config["WebRoot"];
                    if (!string.IsNullOrEmpty(webRoot))
                        webBuilder.UseWebRoot(webRoot);


                    string sport = config["Port"];
                    int.TryParse(sport, out int port);
                    if (port == 0)
                        port = 5000;

                    bool useSsl = StartupHelpers.GetLogicalSetting("UseSsl", config);
                    webBuilder.UseUrls($"http{(useSsl ? "s" : "")}://0.0.0.0:{port}");

                    webBuilder
                        .UseStartup<Startup>();
                });
        }


        static void ShowHelp()
        {

            string razorFlag = null;
            bool useRazor = false;
#if USE_RAZORPAGES
            razorFlag = "\r\n--UseRazor         True|False*";
            useRazor = true;
#endif

            string headerLine = new string('-', AppHeader.Length);
            string exe = "LiveReloadServer";

            if (Environment.CommandLine.Contains("LiveReloadWebServer", StringComparison.InvariantCultureIgnoreCase))
                exe = "LiveReloadWebServer";
            
            Console.WriteLine($@"
{headerLine}
{AppHeader}
{headerLine}
(c) Rick Strahl, West Wind Technologies, 2019

Static and Razor File Service with Live Reload for changed content.

Syntax:
-------
{exe}  <options>

--WebRoot            <path>  (current Path if not provided)
--Port               5200*
--UseSsl             True|False*{razorFlag}
--ShowUrls           True|False*
--OpenBrowser        True*|False
--DefaultFiles       ""index.html,default.htm""*
--Extensions         Live Reload Extensions monitored
                     ""{(useRazor ? ".cshtml," : "")}.css,.js,.htm,.html,.ts""*

Configuration options can be specified in:

* Command Line options as shown above
* Logical Command Line Flags for true can be set with -UseSsl or -UseRazor
* Environment Variables with `LiveReloadServer_` prefix. Example: 'LiveReloadServer_Port'
* You use -UseSsl without True or False to set a logical value to true

Examples:
---------
{exe} --WebRoot ""c:\temp\My Site"" --port 5500 -useSsl -useRazor --openBrowser false

$env:{exe}_Port 5500
$env:{exe}_WebRoot c:\mySites\Site1\Web
{exe}
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

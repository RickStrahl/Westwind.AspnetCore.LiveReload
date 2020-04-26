using System;
using System.IO;
using System.Linq;
using System.Reflection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace LiveReloadServer
{
    public class Program
    {

        public static IHost WebHost;
        

        public static void Main(string[] args)
        {

            if (Environment.CommandLine.Contains("LiveReloadWebServer", StringComparison.InvariantCultureIgnoreCase))
                Helpers.ExeName = "LiveReloadWebServer";

            try
            {
                var version = Assembly.GetExecutingAssembly().GetName().Version;
                var ver = version.Major + "." + version.Minor +
                          (version.Build > 0 ? "." + version.Build : string.Empty);
                Helpers.AppHeader = $"Live Reload Web Server v{ver}";


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

                string headerLine = new string('-', Helpers.AppHeader.Length);
                Console.WriteLine(headerLine);
                Console.WriteLine(Helpers.AppHeader);
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
                        .AddEnvironmentVariables()
                        .AddEnvironmentVariables("LIVERELOADSERVER_")
                        .AddEnvironmentVariables("LIVERELOADWEBSERVER_")
                        .AddCommandLine(args)
                        .Build();


                    var environment = config["Environment"];
                    if (environment == null)
                        environment = "Production";

                    // Custom Logging
                    webBuilder
                        .ConfigureLogging(logging =>
                        {
                            logging.ClearProviders();
                            logging.AddConsole();
                            logging.AddConfiguration(config);
                        })
                        .UseConfiguration(config);

                    

                    var webRoot = config["WebRoot"];
                    if (!string.IsNullOrEmpty(webRoot))
                        webBuilder.UseWebRoot(webRoot);
                    
                    string sport = config["Port"];
                    int.TryParse(sport, out int port);
                    if (port == 0)
                        port = 5200;

                    bool useSsl = Helpers.GetLogicalSetting("UseSsl", config);
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
            razorFlag = "\r\n--UseRazor           True|False*";
            useRazor = true;
#endif

            string headerLine = new string('-', Helpers.AppHeader.Length);
            
            Console.WriteLine($@"
{headerLine}
{Helpers.AppHeader}
{headerLine}
(c) Rick Strahl, West Wind Technologies, 2019-2020

Static and Razor File Service with Live Reload for changed content.

Syntax:
-------
{Helpers.ExeName}  <options>

--WebRoot                <path>  (current Path if not provided)
--Port                   5200*
--UseSsl                 True|False*{razorFlag}
--ShowUrls               True|False*
--OpenBrowser            True*|False
--DefaultFiles           ""index.html,default.htm""*
--Extensions             ""{(useRazor ? ".cshtml," : "")}.css,.js,.htm,.html,.ts""*
--Environment            Production*|Development

Razor Pages:
------------
--UseRazor              True|False*

Markdown Options:
-----------------
--UseMarkdown           True|False*  Renders .md files
--CopyMarkdownResources True*|False  Copies Markdown rendering templates
--MarkdownTemplate      ""~/markdown-themes/__MarkdownTestmplatePage.cshtml""*

Configuration options can be specified in:

* Command Line options as shown above
* Logical Command Line Flags for true can be set like: -UseSsl or -UseRazor or -OpenBrowser
* Environment Variables with '{Helpers.ExeName.ToUpper()}_' prefix. Example: '{Helpers.ExeName.ToUpper()}_PORT'

Examples:
---------
{Helpers.ExeName} --WebRoot ""c:\temp\My Site"" --port 5500 -useSsl -useRazor --openBrowser false

$env:{Helpers.ExeName}_Port 5500
$env:{Helpers.ExeName}_WebRoot c:\mySites\Site1\Web
{Helpers.ExeName}
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

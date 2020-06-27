using System;
using System.IO;
using System.Linq;
using System.Net.Sockets;
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
                Console.WriteLine("\r\nUnable to start the Web Server.");
                Console.ResetColor();
                Console.WriteLine("------------------------------");


                Console.WriteLine("The server port is already in use by another application.");
                Console.WriteLine("Please try and choose another port with the `--port` switch. And try again.");
                Console.WriteLine("\r\n\r\nException Info:");
                Console.WriteLine(ex.Message);
                Console.WriteLine("---------------------------------------------------------------------------");
            }
            catch (SocketException ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("\r\nUnable to start the Web Server.");
                Console.ResetColor();
                Console.WriteLine("------------------------------");

                Console.WriteLine("The server Host IP address is invalid.");
                Console.WriteLine("Please try and choose another host IP address with the `--host` switch. And try again.");
                Console.WriteLine("\r\n\r\nException Info:");
                Console.WriteLine(ex.Message);
                Console.WriteLine("---------------------------------------------------------------------------");
            }
            catch (Exception ex)
            {
                // can't catch internal type
                if (ex.StackTrace.Contains("ThrowOperationCanceledException"))
                    return;

                WriteStartupErrorMessage(ex.Message, ex.StackTrace, ex.Source);
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

                    var serverConfig = new LiveReloadServerConfiguration();
                    serverConfig.LoadFromConfiguration(config);

                    // Custom Logging
                    webBuilder
                        .ConfigureLogging(logging =>
                        {
                            logging.ClearProviders();
                            logging.AddConsole();
                            logging.AddConfiguration(config);
                        })
                        .UseConfiguration(config);

                    var webRoot = serverConfig.WebRoot;
                    if (!string.IsNullOrEmpty(webRoot))
                        webBuilder.UseWebRoot(webRoot);
                  
                    webBuilder.UseUrls($"http{(serverConfig.UseSsl ? "s" : "")}://{serverConfig.Host}:{serverConfig.Port}");

                    webBuilder
                        .UseStartup<Startup>();
                });
        }


        
        public static void WriteStartupErrorMessage(string message, string stackTrace = null, string source = null)
        {
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
            Console.WriteLine(message);

            if (!string.IsNullOrEmpty(stackTrace))
            {
                Console.WriteLine("---");
                Console.WriteLine(stackTrace);
                Console.WriteLine(source);
                Console.WriteLine("---------------------------------------------------------------------------");
            }
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

Static, Markdown and Razor Files Web Server with Live Reload for changed content.

Syntax:
-------
{Helpers.ExeName}  <options>

--WebRoot                <path>  (current Path if not provided)
--Port                   5200*
--Host                   0.0.0.0*|localhost|custom Ip - 0.0.0.0 allows external access
--UseSsl                 True|False*{razorFlag}

--UseLiveReload          True*|False
--Extensions             ""{(useRazor ? ".cshtml," : "")}.css,.js,.htm,.html,.ts""*
--DefaultFiles           ""index.html,default.htm""*

--ShowUrls               True|False*
--OpenBrowser            True*|False
--Environment            Production*|Development

Razor Pages:
------------
--UseRazor              True|False*

Markdown Options:
-----------------
--UseMarkdown           True|False*  
--CopyMarkdownResources True|False*  
--MarkdownTemplate      ~/markdown-themes/__MarkdownTestmplatePage.cshtml*
--MarkdownTheme         github*|dharkan|medium|blackout|westwind
--MarkdownSyntaxTheme   github*|vs2015|vs|monokai|monokai-sublime|twilight

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

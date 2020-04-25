using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Runtime.Loader;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Westwind.AspNetCore.LiveReload;
using Westwind.AspNetCore.Markdown;


namespace LiveReloadServer
{
    public class Startup
    {

        private string WebRoot;
        private int Port = 0;
        public bool UseLiveReload = true;
        public bool UseMarkdown = true;
        private bool UseRazor = false;

        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            // Get Configuration Settings
            UseLiveReload = Helpers.GetLogicalSetting("UseLiveReload", Configuration, true);
            UseMarkdown = Helpers.GetLogicalSetting("UseMarkdown", Configuration, true);
            UseRazor = Helpers.GetLogicalSetting("UseRazor", Configuration);

            WebRoot = Configuration["WebRoot"];
            if (string.IsNullOrEmpty(WebRoot))
                WebRoot = Environment.CurrentDirectory;
            else
                WebRoot = Path.GetFullPath(WebRoot, Environment.CurrentDirectory);

            if (UseLiveReload)
            {
                services.AddLiveReload(opt =>
                {
                    opt.FolderToMonitor = WebRoot;
                    opt.LiveReloadEnabled = UseLiveReload;

                    var extensions = Configuration["Extensions"];
                    if (!string.IsNullOrEmpty(extensions))
                        opt.ClientFileExtensions = extensions;

                    if (UseMarkdown && !opt.ClientFileExtensions.Contains(".md", StringComparison.OrdinalIgnoreCase))
                    {
                        opt.ClientFileExtensions += ",.md,.mkdown";
                    }
                });
            }

            if (UseMarkdown)
            {
                services.AddMarkdown(config =>
                {
                    var folderConfig = config.AddMarkdownProcessingFolder("/","~/__MarkdownPageTemplate.cshtml");
                    
                    // Optional configuration settings
                    folderConfig.ProcessExtensionlessUrls = true;  // default
                    folderConfig.ProcessMdFiles = true; // default

                });
                
                // we have to force MVC in order for the controller routing to work                    
                services
                    .AddMvc()
                    .AddApplicationPart(typeof(MarkdownPageProcessorMiddleware).Assembly)
                    .AddRazorRuntimeCompilation(
                        opt =>
                        {
                            opt.FileProviders.Add(new PhysicalFileProvider(WebRoot));
                        });



            }


#if USE_RAZORPAGES
            if (UseRazor)
            {
                var mvcBuilder = services.AddRazorPages(opt =>
                    {
                        opt.RootDirectory = "/";
                    })
                    .AddRazorRuntimeCompilation(
                        opt =>
                        {
                            opt.FileProviders.Add(new PhysicalFileProvider(WebRoot));
                        });


                LoadPrivateBinAssemblies(mvcBuilder);
            }
#endif
        }


        private static object consoleLock = new object();


        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, Microsoft.AspNetCore.Hosting.IWebHostEnvironment env)
        {
            bool useSsl = Helpers.GetLogicalSetting("useSsl", Configuration, false);
            bool showUrls = Helpers.GetLogicalSetting("ShowUrls", Configuration, true);
            bool openBrowser = Helpers.GetLogicalSetting("OpenBrowser", Configuration, true);

            string defaultFiles = Configuration["DefaultFiles"];
            if (string.IsNullOrEmpty(defaultFiles))
                defaultFiles = "index.html,default.htm,default.html";

            var strPort = Configuration["Port"];
            if (!int.TryParse(strPort, out Port))
                Port = 5200;

            if (UseLiveReload)
                app.UseLiveReload();

            ////if (env.IsDevelopment())
            ////    app.UseDeveloperExceptionPage();
            ////else
            app.UseExceptionHandler("/Error");

            if (showUrls)
            {
                var socketUrl = Configuration["LiveReload:WebSocketUrl"];

                app.Use(async (context, next) =>
                {
                    Stopwatch sw = new Stopwatch();
                    sw.Start();

                    await next();

                    // ignore Web socket requests
                    if(context.Request.Path.Value == socketUrl)
                        return;

                    // need to ensure this happens all at once otherwise multiple threads
                    // write intermixed console output on simultaneous requests
                    lock (consoleLock)
                    {
                        WriteConsoleLogDisplay(context, sw);
                    }
                });
            }

            app.UseMarkdown();

            app.UseDefaultFiles(new DefaultFilesOptions
            {
                FileProvider = new PhysicalFileProvider(WebRoot),
                DefaultFileNames = new List<string>(defaultFiles.Split(',', ';'))
            });

            app.UseStaticFiles(new StaticFileOptions
            {
                FileProvider = new PhysicalFileProvider(WebRoot), RequestPath = new PathString("")
            });

#if USE_RAZORPAGES
            if (UseRazor)
            {
                app.UseRouting();
                app.UseEndpoints(endpoints => { endpoints.MapRazorPages(); });
            }
#endif
            if (UseMarkdown)
            {
                app.UseRouting();
                app.UseEndpoints(endpoints =>
                {
                    endpoints.MapDefaultControllerRoute();
                });
            }


            var url = $"http{(useSsl ? "s" : "")}://localhost:{Port}";
            var extensions = Configuration["Extensions"];

            string headerLine = new string('-', Helpers.AppHeader.Length);
            Console.WriteLine(headerLine);
            ConsoleHelper.WriteLine(Helpers.AppHeader,ConsoleColor.Yellow);
            Console.WriteLine(headerLine);
            Console.WriteLine($"(c) West Wind Technologies, 2019-{DateTime.Now.Year}\r\n");

            Console.Write($"Site Url     : ");
            ConsoleHelper.WriteLine(url,ConsoleColor.DarkCyan);
            Console.WriteLine($"Web Root     : {WebRoot}");
            Console.WriteLine($"Executable   : {Assembly.GetExecutingAssembly().Location}");
            Console.WriteLine(
                $"Extensions   : {(string.IsNullOrEmpty(extensions) ? $"{(UseRazor ? ".cshtml," : "")}.css,.js,.htm,.html,.ts" : extensions)}");
            Console.WriteLine($"Live Reload  : {UseLiveReload}");

#if USE_RAZORPAGES
            Console.WriteLine($"Use Razor    : {UseRazor}");
#endif
            if (UseMarkdown)
                Console.WriteLine($"Use Markdown: {UseMarkdown}");
            Console.WriteLine($"Show Urls    : {showUrls}");
            Console.WriteLine($"Open Browser : {openBrowser}");
            Console.WriteLine($"Default Pages: {defaultFiles}");
            Console.WriteLine($"Environment  : {env.EnvironmentName}");

            Console.WriteLine();
            ConsoleHelper.Write(Helpers.ExeName +  "--help", ConsoleColor.DarkCyan);
            Console.WriteLine(" for start options...");
            Console.WriteLine();
            ConsoleHelper.WriteLine("Ctrl-C or Ctrl-Break to exit...",ConsoleColor.Yellow);

            Console.WriteLine("----------------------------------------------");

            var oldColor = Console.ForegroundColor;
            foreach (var assmbly in LoadedPrivateAssemblies)
            {
                var fname = Path.GetFileName(assmbly);
                ConsoleHelper.WriteLine("Additional Assembly: " + fname,ConsoleColor.DarkGreen);
            }

            foreach (var assmbly in FailedPrivateAssemblies)
            {
                var fname = Path.GetFileName(assmbly);
                ConsoleHelper.WriteLine("Failed Additional Assembly: " + fname,ConsoleColor.DarkGreen);
            }

            Console.ForegroundColor = oldColor;

            if (openBrowser)
                Helpers.OpenUrl(url);
        }

        private void WriteConsoleLogDisplay(HttpContext context, Stopwatch sw)
        {
            var url =
                $"{context.Request.Method}  {context.Request.Scheme}://{context.Request.Host}  {context.Request.Path}{context.Request.QueryString}";

            url = url.PadRight(80, ' ');

            var ct = context.Response.ContentType;
            bool isPrimary = ct != null &&
                             (ct.StartsWith("text/html") ||
                              ct.StartsWith("text/plain") ||
                              ct.StartsWith("application/json") ||
                              ct.StartsWith("text/xml"));


            var saveColor = Console.ForegroundColor;

            if (ct == null) // no response
            {
                ConsoleHelper.Write(url + " ", ConsoleColor.Red);
                isPrimary = true;
            }
            else if (isPrimary)
                ConsoleHelper.Write(url + " ", ConsoleColor.Gray);
            else
                ConsoleHelper.Write(url + " ", ConsoleColor.DarkGray);

            var status = context.Response.StatusCode;
            if (status >= 200 && status < 400)
                ConsoleHelper.Write(status.ToString(),
                    isPrimary ? ConsoleColor.Green : ConsoleColor.DarkGreen);
            else if (status == 401)
                ConsoleHelper.Write(status.ToString(),
                    isPrimary ? ConsoleColor.Yellow : ConsoleColor.DarkYellow);
            else if (status >= 400)
                ConsoleHelper.Write(status.ToString(), isPrimary ? ConsoleColor.Red : ConsoleColor.DarkRed);

            sw.Stop();
            ConsoleHelper.WriteLine($" {sw.ElapsedMilliseconds:n0}ms".PadLeft(8), ConsoleColor.DarkGray);


            Console.ForegroundColor = saveColor;
        }

        public static Type GetTypeFromName(string TypeName)
        {

            Type type = Type.GetType(TypeName);
            if (type != null)
                return type;

            // *** try to find manually
            foreach (Assembly ass in AssemblyLoadContext.Default.Assemblies)
            {
                type = ass.GetType(TypeName, false);

                if (type != null)
                    break;

            }

            return type;
        }

        private List<string> LoadedPrivateAssemblies = new List<string>();
        private List<string> FailedPrivateAssemblies = new List<string>();

        private void LoadPrivateBinAssemblies(IMvcBuilder mvcBuilder)
        {
            var binPath = Path.Combine(WebRoot, "privatebin");
            if (Directory.Exists(binPath))
            {
                var files = Directory.GetFiles(binPath);
                foreach (var file in files)
                {
                    if (!file.EndsWith(".dll", StringComparison.CurrentCultureIgnoreCase) &&
                        !file.EndsWith(".exe", StringComparison.InvariantCultureIgnoreCase))
                        continue;

                    try
                    {
                        var asm = AssemblyLoadContext.Default.LoadFromAssemblyPath(file);
                        mvcBuilder.AddApplicationPart(asm);
                        LoadedPrivateAssemblies.Add(file);
                    }
                    catch (Exception ex)
                    {
                        FailedPrivateAssemblies.Add(file + "\n    - " + ex.Message);
                    }

                }
            }

        }

    }
}

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
using Westwind.Utilities;


namespace LiveReloadServer
{
    public class Startup
    {
        public static string StartupPath { get; set; }

        private string WebRoot;
        private int Port = 0;
        public bool UseLiveReload = true;
        private bool UseRazor = false;

        public bool UseMarkdown = false;
        private bool CopyMarkdownResources;
        private string MarkdownTemplate = "~/markdown-themes/__MarkdownPageTemplate.cshtml";
        private string MarkdownTheme = "github";
        private string MarkdownSyntaxTheme = "github";

        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
            StartupPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            
            WebRoot = Configuration["WebRoot"];
            if (string.IsNullOrEmpty(WebRoot))
                WebRoot = Environment.CurrentDirectory;
            else
                WebRoot = Path.GetFullPath(WebRoot, Environment.CurrentDirectory);
            
            // Enable Live Reload Middleware
            UseLiveReload = Helpers.GetLogicalSetting("UseLiveReload", Configuration, true);

            // Razor enables compilation and Razor Page Engine
            UseRazor = Helpers.GetLogicalSetting("UseRazor", Configuration);

            // Enables Markdown Middleware and optionally copies Markdown Templates into output folder
            UseMarkdown = Helpers.GetLogicalSetting("UseMarkdown", Configuration, false);
            CopyMarkdownResources = false;
            if (UseMarkdown)
            {
                // defaults to true but only if Markdown is enabled!
                CopyMarkdownResources = Helpers.GetLogicalSetting("CopyMarkdownResources", Configuration,false);
                MarkdownTemplate = Configuration["MarkdownTemplate"] ?? MarkdownTemplate;
                MarkdownTheme = Configuration["MarkdownTheme"] ?? "github";
                MarkdownSyntaxTheme = Configuration["MarkdownSyntaxTheme"] ?? "github";
            }


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

            IMvcBuilder mvcBuilder = null;

#if USE_RAZORPAGES
            if (UseRazor)
            {
                mvcBuilder = services.AddRazorPages(opt =>
                {
                    opt.RootDirectory = "/";
                });
            }
#endif

            if (UseMarkdown)
            {
                services.AddMarkdown(config =>
                {
                    
                    //var templatePath = Path.Combine(WebRoot, "markdown-themes/__MarkdownPageTemplate.cshtml");
                    //if (!File.Exists(templatePath))
                    //    templatePath = Path.Combine(Environment.CurrentDirectory,"markdown-themes/__MarkdownPageTemplate.cshtml");
                    //else
                    var templatePath = MarkdownTemplate;
                    templatePath = templatePath.Replace("\\", "/");
                    
                    var folderConfig = config.AddMarkdownProcessingFolder("/",templatePath);
                    
                    // Optional configuration settings
                    folderConfig.ProcessExtensionlessUrls = true;  // default
                    folderConfig.ProcessMdFiles = true; // default

                    folderConfig.RenderTheme = MarkdownTheme;
                    folderConfig.SyntaxTheme = MarkdownSyntaxTheme;
                });
                
                // we have to force MVC in order for the controller routing to work                    
                mvcBuilder = services
                    .AddMvc();

                // copy Markdown Template and resources if it doesn't exist
                if (CopyMarkdownResources)
                    CopyMarkdownTemplateResources();
            }

            // If Razor or Markdown are enabled we need custom folders
            if (mvcBuilder != null)
            {
                mvcBuilder.AddRazorRuntimeCompilation(
                    opt =>
                    {
                        opt.FileProviders.Clear();
                        opt.FileProviders.Add(new PhysicalFileProvider(WebRoot));
                        opt.FileProviders.Add(new PhysicalFileProvider(Path.Combine(Startup.StartupPath,"templates")));
                        
                        
                    });

                LoadPrivateBinAssemblies(mvcBuilder);
            }
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
                    var originalPath = context.Request.Path.Value;

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
                        WriteConsoleLogDisplay(context, sw, originalPath);
                    }
                });
            }

            if (UseMarkdown)
                app.UseMarkdown();

            app.UseDefaultFiles(new DefaultFilesOptions
            {
                FileProvider = new PhysicalFileProvider(WebRoot),
                DefaultFileNames = new List<string>(defaultFiles.Split(',', ';'))
            });

            // add static files to WebRoot and our templates folder which provides markdown templates
            // and potentially other library resources in the future
            var wrProvider = new PhysicalFileProvider(WebRoot);
            var tpProvider= new PhysicalFileProvider(Path.Combine(Startup.StartupPath,"templates"));
            var compositeProvider = new CompositeFileProvider(wrProvider, tpProvider);
            app.UseStaticFiles(new StaticFileOptions
            {
                FileProvider = compositeProvider, //new PhysicalFileProvider(WebRoot),
                RequestPath = new PathString("")
            });

            if (UseRazor || UseMarkdown)
                app.UseRouting();

#if USE_RAZORPAGES
            if (UseRazor)
            {
                app.UseEndpoints(endpoints => { endpoints.MapRazorPages(); });
            }
#endif
            if (UseMarkdown)
            {
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

            Console.WriteLine($"Use Markdown : {UseMarkdown}");
            if (UseMarkdown)
            {
            Console.WriteLine($"  Resources  : {CopyMarkdownResources}");
            Console.WriteLine($"  Template   : {MarkdownTemplate}");
            Console.WriteLine($"  Theme      : {MarkdownTheme}");
            Console.WriteLine($"  SyntaxTheme: {MarkdownSyntaxTheme}");
            }
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

        private void WriteConsoleLogDisplay(HttpContext context, Stopwatch sw, string originalPath)
        {
            var url =
                $"{context.Request.Method}  {context.Request.Scheme}://{context.Request.Host}  {originalPath}{context.Request.QueryString}";

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

        /// <summary>
        /// Copies the Markdown Template resources into the WebRoot if it doesn't exist already.
        ///
        /// If you want to get a new set of template, delete the `markdown-themes` folder in hte
        /// WebRoot output folder.
        /// </summary>
        /// <returns>false if already exists and no files were copied. True if path doesn't exist and files were copied</returns>
        private bool CopyMarkdownTemplateResources()
        {
            // explicitly don't want to copy resources
            if (!CopyMarkdownResources)
                return false;

            var templatePath = Path.Combine(WebRoot,"markdown-themes");
            if (Directory.Exists(templatePath))
                return false;

            FileUtils.CopyDirectory(Path.Combine(Startup.StartupPath,"templates", "markdown-themes"),
                templatePath,
                deepCopy: true);

            return true;
        }
    }

    //public class MarkdownViewLocationExpander: IViewLocationExpander {

    //    private readonly IEnumerable<string> _paths;

    //    public MarkdownViewLocationExpander(IEnumerable<string> paths)
    //    {
    //        _paths = paths;
    //    }

    //    /// <summary>
    //    /// Used to specify the locations that the view engine should search to 
    //    /// locate views.
    //    /// </summary>
    //    /// <param name="context"></param>
    //    /// <param name="viewLocations"></param>
    //    /// <returns></returns>
    //    public IEnumerable<string> ExpandViewLocations(ViewLocationExpanderContext context, IEnumerable<string> viewLocations) {
    //        return viewLocations.Union(_paths);
    //    }

    //    public void PopulateValues(ViewLocationExpanderContext context) {
    //        context.Values["customviewlocation"] = nameof(MarkdownViewLocationExpander);
    //    }
    //}
}

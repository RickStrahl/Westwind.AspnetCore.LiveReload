using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Runtime.Loader;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Westwind.AspNetCore.LiveReload;
using Westwind.AspNetCore.Markdown;
using Westwind.Utilities;
using Microsoft.Extensions.FileProviders.Physical;

namespace LiveReloadServer
{
    public class Startup
    {
        /// <summary>
        /// Binary Startup Location irrespective of the environment path
        /// </summary>
        public static string StartupPath { get; set; }

        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
            StartupPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
        }

        public IConfiguration Configuration { get; }

        public LiveReloadServerConfiguration ServerConfig { get; set; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            ServerConfig = new LiveReloadServerConfiguration();
            ServerConfig.LoadFromConfiguration(Configuration);



            if (ServerConfig.UseLiveReload)
            {
                services.AddLiveReload(opt =>
                {
                    opt.FolderToMonitor = ServerConfig.WebRoot;
                    opt.LiveReloadEnabled = ServerConfig.UseLiveReload;


                    if (!string.IsNullOrEmpty(ServerConfig.Extensions))
                        opt.ClientFileExtensions = ServerConfig.Extensions;

                    if (ServerConfig.UseMarkdown && !opt.ClientFileExtensions.Contains(".md", StringComparison.OrdinalIgnoreCase))
                    {
                        opt.ClientFileExtensions += ",.md,.mkdown";
                    }
                });
            }

            IMvcBuilder mvcBuilder = null;

#if USE_RAZORPAGES
            if (ServerConfig.UseRazor)
            {
                mvcBuilder = services.AddRazorPages(opt => { opt.RootDirectory = "/"; });
            }
#endif

            if (ServerConfig.UseMarkdown)
            {
                services.AddMarkdown(config =>
                {

                    //var templatePath = Path.Combine(WebRoot, "markdown-themes/__MarkdownPageTemplate.cshtml");
                    //if (!File.Exists(templatePath))
                    //    templatePath = Path.Combine(Environment.CurrentDirectory,"markdown-themes/__MarkdownPageTemplate.cshtml");
                    //else
                    var templatePath = ServerConfig.MarkdownTemplate;
                    templatePath = templatePath.Replace("\\", "/");

                    var folderConfig = config.AddMarkdownProcessingFolder("/", templatePath);

                    // Optional configuration settings
                    folderConfig.ProcessExtensionlessUrls = true; // default
                    folderConfig.ProcessMdFiles = true; // default

                    folderConfig.RenderTheme = ServerConfig.MarkdownTheme;
                    folderConfig.SyntaxTheme = ServerConfig.MarkdownSyntaxTheme;
                });

                // we have to force MVC in order for the controller routing to work
                mvcBuilder = services
                    .AddMvc();

                // copy Markdown Template and resources if it doesn't exist
                if (ServerConfig.CopyMarkdownResources)
                    CopyMarkdownTemplateResources();
            }

            // If Razor or Markdown are enabled we need custom folders
            if (mvcBuilder != null)
            {
                mvcBuilder.AddRazorRuntimeCompilation(
                    opt =>
                    {
                        opt.FileProviders.Clear();
                        opt.FileProviders.Add(new PhysicalFileProvider(ServerConfig.WebRoot));
                        opt.FileProviders.Add(new PhysicalFileProvider(Path.Combine(Startup.StartupPath, "templates")));


                    });

                LoadPrivateBinAssemblies(mvcBuilder);
            }
        }


        private static object consoleLock = new object();


        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, Microsoft.AspNetCore.Hosting.IWebHostEnvironment env)
        {
            if (ServerConfig.UseLiveReload)
                app.UseLiveReload();

            ////if (env.IsDevelopment())
            ////    app.UseDeveloperExceptionPage();
            ////else
            app.UseExceptionHandler("/Error");

            if (ServerConfig.ShowUrls)
            {
                var socketUrl = Configuration["LiveReload:WebSocketUrl"];

                app.Use(async (context, next) =>
                {
                    var originalPath = context.Request.Path.Value;

                    Stopwatch sw = new Stopwatch();
                    sw.Start();

                    await next();

                    // ignore Web socket requests
                    if (context.Request.Path.Value == socketUrl)
                        return;

                    // need to ensure this happens all at once otherwise multiple threads
                    // write intermixed console output on simultaneous requests
                    lock (consoleLock)
                    {
                        WriteConsoleLogDisplay(context, sw, originalPath);
                    }
                });
            }

            if (ServerConfig.UseMarkdown)
                app.UseMarkdown();

            app.UseDefaultFiles(new DefaultFilesOptions
            {
                FileProvider = new PhysicalFileProvider(ServerConfig.WebRoot),
                DefaultFileNames = new List<string>(ServerConfig.DefaultFiles.Split(',', ';'))
            });

            // add static files to WebRoot and our templates folder which provides markdown templates
            // and potentially other library resources in the future

            var wrProvider = new PhysicalFileProvider(ServerConfig.WebRoot);
            var tpProvider = new PhysicalFileProvider(Path.Combine(Startup.StartupPath, "templates"));

            var extensionProvider = new FileExtensionContentTypeProvider();
            extensionProvider.Mappings.Add(".dll", "application/octet-stream");
            if (ServerConfig.AdditionalMimeMappings != null)
            {
                foreach (var map in ServerConfig.AdditionalMimeMappings)
                    extensionProvider.Mappings[map.Key] = map.Value;
            }

            var compositeProvider = new CompositeFileProvider(wrProvider, tpProvider);
            var staticFileOptions = new StaticFileOptions
            {
                FileProvider = compositeProvider, //new PhysicalFileProvider(WebRoot),
                RequestPath = new PathString(""),
                ContentTypeProvider = extensionProvider
            };
            app.UseStaticFiles(staticFileOptions);


            if (ServerConfig.UseRazor || ServerConfig.UseMarkdown)
                app.UseRouting();

#if USE_RAZORPAGES
            if (ServerConfig.UseRazor)
            {
                app.UseEndpoints(endpoints => { endpoints.MapRazorPages(); });
            }
#endif
            if (ServerConfig.UseMarkdown)
            {
                app.UseEndpoints(endpoints => { endpoints.MapDefaultControllerRoute(); });
            }

            app.Use(FallbackHandler);

            string headerLine = new string('-', Helpers.AppHeader.Length);
            Console.WriteLine(headerLine);
            ConsoleHelper.WriteLine(Helpers.AppHeader, ConsoleColor.Yellow);
            Console.WriteLine(headerLine);
            Console.WriteLine($"(c) West Wind Technologies, 2019-{DateTime.Now.Year}\r\n");

            Console.Write($"Site Url     : ");
            ConsoleHelper.WriteLine(ServerConfig.GetHttpUrl(true), ConsoleColor.DarkCyan);
            Console.WriteLine($"Web Root     : {ServerConfig.WebRoot}");
            Console.WriteLine($"Executable   : {Assembly.GetExecutingAssembly().Location}");
            Console.WriteLine($"Live Reload  : {ServerConfig.UseLiveReload}");
            if (ServerConfig.UseLiveReload)
                Console.WriteLine(
                    $"  Extensions : {(string.IsNullOrEmpty(ServerConfig.Extensions) ? $"{(ServerConfig.UseRazor ? ".cshtml," : "")}.css,.js,.htm,.html,.ts" : ServerConfig.Extensions)}");


#if USE_RAZORPAGES
            Console.WriteLine($"Use Razor    : {ServerConfig.UseRazor}");
#endif

            Console.WriteLine($"Use Markdown : {ServerConfig.UseMarkdown}");
            if (ServerConfig.UseMarkdown)
            {
                Console.WriteLine($"  Resources  : {ServerConfig.CopyMarkdownResources}");
                Console.WriteLine($"  Template   : {ServerConfig.MarkdownTemplate}");
                Console.WriteLine($"  Theme      : {ServerConfig.MarkdownTheme}");
                Console.WriteLine($"  SyntaxTheme: {ServerConfig.MarkdownSyntaxTheme}");
            }

            Console.WriteLine($"Show Urls    : {ServerConfig.ShowUrls}");
            Console.WriteLine($"Open Browser : {ServerConfig.OpenBrowser}");
            Console.WriteLine($"Default Pages: {ServerConfig.DefaultFiles}");
            Console.WriteLine($"Environment  : {env.EnvironmentName}");

            Console.WriteLine();
            ConsoleHelper.Write(Helpers.ExeName + "--help", ConsoleColor.DarkCyan);
            Console.WriteLine(" for start options...");
            Console.WriteLine();
            ConsoleHelper.WriteLine("Ctrl-C or Ctrl-Break to exit...", ConsoleColor.Yellow);

            Console.WriteLine("----------------------------------------------");

            var oldColor = Console.ForegroundColor;
            foreach (var assmbly in LoadedPrivateAssemblies)
            {
                var fname = Path.GetFileName(assmbly);
                ConsoleHelper.WriteLine("Additional Assembly: " + fname, ConsoleColor.DarkGreen);
            }

            foreach (var assmbly in FailedPrivateAssemblies)
            {
                var fname = Path.GetFileName(assmbly);
                ConsoleHelper.WriteLine("Failed Additional Assembly: " + fname, ConsoleColor.DarkGreen);
            }

            Console.ForegroundColor = oldColor;

            if (ServerConfig.OpenBrowser)
            {
                Helpers.OpenUrl(ServerConfig.GetHttpUrl());
            }
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
            var binPath = Path.Combine(ServerConfig.WebRoot, "privatebin");
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
            if (!ServerConfig.CopyMarkdownResources)
                return false;

            var templatePath = Path.Combine(ServerConfig.WebRoot, "markdown-themes");
            if (Directory.Exists(templatePath))
                return false;

            FileUtils.CopyDirectory(Path.Combine(Startup.StartupPath, "templates", "markdown-themes"),
                templatePath,
                deepCopy: true);

            return true;
        }

        private async Task FallbackHandler(HttpContext context, Func<Task> next)
        {
            // 404 - no match
            if (string.IsNullOrEmpty(ServerConfig.FolderNotFoundFallbackPath))
            {
                context.Response.StatusCode = 404;
                await context.Response.WriteAsync("<h1>Not Found</h1>");
                return;
            }

            // 404  - SPA fall through middleware - for SPA apps should fallback to index.html
            var path = context.Request.Path;
            if (string.IsNullOrEmpty(Path.GetExtension(path)))
            {
                var file = Path.Combine(ServerConfig.WebRoot,
                    ServerConfig.FolderNotFoundFallbackPath.Trim('/', '\\'));
                var fi = new FileInfo(file);
                if (fi.Exists)
                {
                    await context.Response.SendFileAsync(new PhysicalFileInfo(fi));
                    await context.Response.CompleteAsync();
                }
                else
                {
                    await context.Response.WriteAsync(@$"
<h1>Invalid Folder Not Found Fallback Path</h1>
<p>Fallback path: <b>{ServerConfig.FolderNotFoundFallbackPath}</b> </p>
<p>The file referenced in this path could not be found and the fallback route failed to load.</p>
<p>Please create the page or remove the FolderNotFoundFallbackPath setting.</p>");
                }

            }
        }
    }
}


using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Runtime.Loader;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Razor.RuntimeCompilation;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Westwind.AspNetCore.LiveReload;
using Westwind.Utilities;


namespace LiveReloadServer
{
    public class Startup
    {

        private string WebRoot;
        private int Port = 0;
        public bool UseLiveReload = true;
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
            UseLiveReload = Helpers.GetLogicalSetting("LiveReloadEnabled", Configuration);
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
                });
            }


#if USE_RAZORPAGES
            if (UseRazor)
            {
                var mvcBuilder = services.AddRazorPages(opt => { opt.RootDirectory = "/"; })
                    .AddRazorRuntimeCompilation(
                        opt =>
                        {

                            opt.FileProviders.Add(new PhysicalFileProvider(WebRoot));
                        });

                LoadPrivateBinAssemblies(mvcBuilder);
            }
#endif
        }



        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, Microsoft.AspNetCore.Hosting.IWebHostEnvironment env)
        {
            bool useSsl = Helpers.GetLogicalSetting("useSsl", Configuration);
            bool showUrls = Helpers.GetLogicalSetting("ShowUrls", Configuration);
            bool openBrowser = Helpers.GetLogicalSetting("OpenBrowser", Configuration);

            string defaultFiles = Configuration["DefaultFiles"];
            if (string.IsNullOrEmpty(defaultFiles))
                defaultFiles = "index.html,default.htm,default.html";

            var strPort = Configuration["Port"];
            if (!int.TryParse(strPort, out Port))
                Port = 5000;

            if (UseLiveReload)
                app.UseLiveReload();

            if (env.IsDevelopment())
                app.UseDeveloperExceptionPage();
            else
                app.UseExceptionHandler("/Error");

            if (showUrls)
            {
                app.Use(async (context, next) =>
                {
                    var url =
                        $"{context.Request.Method}  {context.Request.Scheme}://{context.Request.Host}  {context.Request.Path}{context.Request.QueryString}";
                    Console.WriteLine(url);
                    await next();
                });
            }

            app.UseDefaultFiles(new DefaultFilesOptions
            {
                FileProvider = new PhysicalFileProvider(WebRoot),
                DefaultFileNames = new List<string>(defaultFiles.Split(',', ';'))
            });

            app.UseStaticFiles(new StaticFileOptions
            {
                FileProvider = new PhysicalFileProvider(WebRoot),
                RequestPath = new PathString("")
            });

#if USE_RAZORPAGES
            if (UseRazor)
            {
                app.UseRouting();
                app.UseEndpoints(endpoints => { endpoints.MapRazorPages(); });
            }
#endif
            var url = $"http{(useSsl ? "s" : "")}://localhost:{Port}";
            var extensions = Configuration["Extensions"];

            string headerLine = new string('-', Helpers.AppHeader.Length);
            Console.WriteLine(headerLine);
            Console.WriteLine(Helpers.AppHeader);
            Console.WriteLine(headerLine);
            Console.WriteLine($"(c) West Wind Technologies, 2018-{DateTime.Now.Year}\r\n");
            Console.Write($"Site Url     : ");
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine(url);
            Console.ResetColor();
            Console.WriteLine($"Web Root     : {WebRoot}");
            Console.WriteLine(
                $"Extensions   : {(string.IsNullOrEmpty(extensions) ? $"{(UseRazor ? ".cshtml," : "")}.css,.js,.htm,.html,.ts" : extensions)}");
            Console.WriteLine($"Live Reload  : {UseLiveReload}");

#if USE_RAZORPAGES
            Console.WriteLine($"Use Razor    : {UseRazor}");
#endif
            Console.WriteLine($"Show Urls    : {showUrls}");
            Console.WriteLine($"Open Browser : {openBrowser}");
            Console.WriteLine($"Default Pages: {defaultFiles}");
            Console.WriteLine($"Environment  : {env.EnvironmentName}");

            Console.WriteLine();
            Console.WriteLine($"'{Helpers.ExeName} --help' for start options...");
            Console.WriteLine();
            Console.WriteLine("Ctrl-C or Ctrl-Break to exit...");

            Console.WriteLine("----------------------------------------------");

            var oldColor = Console.ForegroundColor;
            foreach (var assmbly in LoadedPrivateAssemblies)
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("Additional Assembly: " + assmbly);
            }
            foreach (var assmbly in FailedPrivateAssemblies)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("Failed Additional Assembly: " + assmbly);
            }
            Console.ForegroundColor = oldColor;

            if (openBrowser)
                Helpers.OpenUrl(url);
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

using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Westwind.AspNetCore.LiveReload;

namespace Westwind.AspNetCore.LiveReload.Web30
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
          }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.Configure<CookiePolicyOptions>(options =>
            {
                // This lambda determines whether user consent for non-essential cookies is needed for a given request.
                options.CheckConsentNeeded = context => true;
            });

            services.AddLiveReload(config =>
            {
                // optional - use config instead
                //config.LiveReloadEnabled = false;
                //config.FolderToMonitor = Env.ContentRootPath;
                //config.WebSocketHost = "wss://localhost:44365";  // explicitly provide the WebSocket Host if proxying

                // ignore certain files or folder
                config.FileInclusionFilter = (path)=>
                {
                    if (path.Contains("/LocalizationAdmin", StringComparison.OrdinalIgnoreCase))
                        return FileInclusionModes.DontRefresh;
    
                    return FileInclusionModes.ContinueProcessing;
                };
                // config.LiveReloadEnabled = true;   ideally set this in appsettings.json
                config.RefreshInclusionFilter = path =>
                {
                    // don't refresh files on the client in the /LocalizationAdmin folder
                    if (path.Contains("/LocalizationAdmin", StringComparison.OrdinalIgnoreCase))
                        return RefreshInclusionModes.DontRefresh;
    
                    return RefreshInclusionModes.ContinueProcessing;
                };
            });


            services.AddControllersWithViews()
                //.AddMvcOptions(opt => { opt.SerializerOptions.PropertyNameCaseInsensitive = true; });
                .AddNewtonsoftJson();

           services.AddRazorPages().AddRazorRuntimeCompilation();
           services.AddMvc().AddRazorRuntimeCompilation();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env, IHostApplicationLifetime lifetime)
        {
            app.UseLiveReload();

            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseExceptionHandler("/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }

            //app.UseHttpsRedirection();
            app.UseStaticFiles();

            app.UseCookiePolicy();

            app.UseRouting();

            app.UseAuthorization();

        

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllerRoute(
                    name: "default",
                    pattern: "{controller=Home}/{action=Index}/{id?}");
                endpoints.MapRazorPages();
            });


            // Check for lifetime shutdown working with WebSocket active
            lifetime.ApplicationStopping.Register(() =>
            {
                Console.WriteLine("*** Application is shutting down...");
            }, true);

            lifetime.ApplicationStopped.Register(() =>
            {
                Console.WriteLine("*** Application is shut down...");
            }, true);
        }
    }
}

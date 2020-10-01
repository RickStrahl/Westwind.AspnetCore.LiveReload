using System.Collections.Generic;
using System.Net.WebSockets;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Westwind.AspNetCore.LiveReload;

namespace Westwind.AspNetCore.LiveReload.Web
{
    public class Startup
    {

        internal static HashSet<WebSocket> ActiveSockets = new HashSet<WebSocket>();

        public IConfiguration Configuration { get; }
        public IHostingEnvironment Env { get; }
        
        public Startup(IConfiguration configuration, IHostingEnvironment env)
        {
            Configuration = configuration;
            Env = env;
        }


        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.Configure<CookiePolicyOptions>(options =>
            {
                // This lambda determines whether user consent for non-essential cookies is needed for a given request.
                options.CheckConsentNeeded = context => true;
                options.MinimumSameSitePolicy = SameSiteMode.None;
            });

            
            services.AddLiveReload(config =>
            {
                // optional - use config instead
                //config.LiveReloadEnabled = true;
                //config.FolderToMonitor = Env.ContentRootPath;
            });
        

            services.AddMvc().SetCompatibilityVersion(CompatibilityVersion.Version_2_2);
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IApplicationLifetime lifeTime)
        {
            
            app.UseLiveReload();   // for sample apply for everything - typically only for Development

            if (Env.IsDevelopment())
            {

                // add it here
                //app.UseLiveReload();
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseExceptionHandler("/Home/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                //app.UseHsts();
            }



            //app.UseEndpointRouting();

            app.UseHttpsRedirection();
            app.UseStaticFiles();
            app.UseCookiePolicy();

            app.UseMvc(routes =>
            {
                routes.MapRoute(
                    name: "default",
                    template: "{controller=Home}/{action=Index}/{id?}");
            });
        }
    }
}

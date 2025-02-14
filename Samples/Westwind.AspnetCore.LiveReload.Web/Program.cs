// *** Minimal API Sample - for Startup.cs code see Web50 Sample
using System;
using System.Runtime.InteropServices;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Westwind.AspNetCore.LiveReload;


var builder = WebApplication.CreateBuilder(args);
var services = builder.Services;
var configuration = builder.Configuration;
var host = builder.Host;
var env = builder.Environment;


services.Configure<CookiePolicyOptions>(options =>
{
    // This lambda determines whether user consent for non-essential cookies is needed for a given request.
    options.CheckConsentNeeded = context => true;
});


// Live Reload - explicitly add optional options. In most cases parameterless is enough
services.AddLiveReload(config =>
{
    // optional - use file config instead
    //config.LiveReloadEnabled = false;
    //config.FolderToMonitor = Env.ContentRootPath;
    //config.WebSocketHost = "wss://localhost:44365";  // explicitly provide the WebSocket Host if proxying

    // ignore certain files or folder
    config.FileInclusionFilter = (path) =>
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


services.AddControllersWithViews();
    //.AddMvcOptions(opt => { opt.SerializerOptions.PropertyNameCaseInsensitive = true; });
    //.AddNewtonsoftJson();

services.AddRazorPages().AddRazorRuntimeCompilation();
services.AddMvc().AddRazorRuntimeCompilation();


var app = builder.Build();

var lifetime = app.Services.GetService<IHostApplicationLifetime>();

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

app.UseDefaultFiles();

app.UseCookiePolicy();

app.UseRouting();

//app.UseAuthentication();
//app.UseAuthorization();

app.MapDefaultControllerRoute();
    //.MapControllers();
app.MapRazorPages();


// Check for lifetime shutdown working with WebSocket active
lifetime.ApplicationStopping.Register(() =>
{
    Console.WriteLine("*** Application is shutting down...");
}, true);

lifetime.ApplicationStopped.Register(() =>
{
    Console.WriteLine("*** Application is shut down...");
}, true);




Console.ForegroundColor = ConsoleColor.DarkYellow;
Console.WriteLine($@"-------------------------------------
Westwind.AspNetCore.LiveReload Sample
-------------------------------------");
Console.ResetColor();

var urls = builder.WebHost.GetSetting(WebHostDefaults.ServerUrlsKey)?.Replace(";", " ");
Console.Write($"    Urls: ");
Console.ForegroundColor = ConsoleColor.DarkCyan;
Console.WriteLine($"{urls}", ConsoleColor.DarkCyan);
Console.ResetColor();

Console.WriteLine($" Runtime: {RuntimeInformation.FrameworkDescription} - {app.Environment.EnvironmentName}");
Console.WriteLine($"Platform: {RuntimeInformation.OSDescription}");
Console.WriteLine();


app.Run();

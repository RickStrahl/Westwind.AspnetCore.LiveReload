# Live Reload Middleware for ASP.NET Core

[![NuGet](https://img.shields.io/nuget/v/Westwind.AspnetCore.LiveReload.svg)](https://www.nuget.org/packages/Westwind.AspnetCore.LiveReload/)
[![](https://img.shields.io/nuget/dt/Westwind.AspnetCore.LiveReload.svg)](https://www.nuget.org/packages/Westwind.AspnetCore.LiveReload/)

* **Live Reload Middleware Component**  
Add the middleware to an existing Web UI Project to provide Live Reload functionality that causes the active page to reload if a file is changed.

* **[Generic local Web Server as a Dotnet Tool](https://github.com/RickStrahl/LiveReloadServer)**  
There's also a standalone, self-contained local Web Server using this Live Reload functionality available as [Dotnet Tool](https://www.nuget.org/packages/LiveReloadServer/) and [Chocolatey Package](https://chocolatey.org/packages/LiveReloadWebServer). The server optionally also support loose Razor Pages and rendering of Markdown documents. Simply run `LiveReloadServer --WebRoot <folder>` to locally serve a Web site.

## Install the Live Reload Middleware
You can install the Live Reload middleware [from NuGet](https://www.nuget.org/packages/Westwind.AspNetCore.LiveReload):

```ps
PS> Install-Package WestWind.AspnetCore.LiveReload
```

or via the .NET Core CLI:

```bash
dotnet add package Westwind.AspnetCore.LiveReload
```

It works with:

* Client side static files  (HTML, CSS, JavaScript etc.)
* ASP.NET Core Views/Pages (.cshtml)
* Server Side compiled code updates (combined w/ `dotnet watch`)
* Limited Blazor Support ([see below](#blazor-support))

The Middleware is self-contained and has no external dependencies - there's nothing else to install or run. You should run `dotnet watch run` to automatically reload server side code to reload the server.  The middleware can then automatically refresh the browser. The extensions monitored for are configurable.

* [Detailed blog post with Implementation Details](https://weblog.west-wind.com/posts/2019/Jun/03/Building-Live-Reload-Middleware-for-ASPNET-Core)
* [NuGet Package](https://www.nuget.org/packages/Westwind.AspNetCore.LiveReload)

#### Minimum Requirements:

* ASP.NET Core 2.1
* Works with .NET Core 2.1, 3.x, .NET 5 Preview

Here's a short video that demonstrates some of the functionality:

![](https://github.com/RickStrahl/Westwind.AspnetCore.LiveReload/blob/master/Westwind.AspNetCore.LiveReload.gif?raw=true)

This demonstrates updating Razor Views/Pages, static CSS and HTML content, and making a source code change in a controller that affects the UI. The only thing 'running' is `dotnet.watch.run` and there are no manual updates.

## What does it do?
This middleware monitors for file changes in your project and tries to automatically refresh your browser when a change is detected. It uses a `FileWatcher` to monitor for file changes, and a `WebSocket` 'server' that client pages connect to refresh the page. The middleware intercepts all HTML page requests and injects a block of JavaScript code that hooks up the client WebSocket interface to support the 'remote' refresh operation. When file changes are detected the server pushes the refresh requests to the pages that are listening on the WebSocket. 

This tool uses raw WebSockets, so it's very light weight with no additional library dependencies. You can also turn off Live Reload with a configuration setting in which case the middleware is not hooked up at all.

In order to restart the server for server code changes you need to run your application with `dotnet watch run`. This built-in tool automatically restarts your .NET Core application anytime a code change is made. `dotnet watch run` is optional, but without it server side code changes require you to manually restart the server. Razor Views/Pages don't require `dotnet watch run` to refresh since they are dynamically compiled in development.

### Configuration
The full configuration and run process looks like this:

* Add `services.AddLiveReload()` in `Startup.ConfigureServices()`
* Add `app.UseLiveReload()` in `Startup.Configure()` before any output generating middleware
* Run `dotnet watch run` to run your application

Add the namespace in `Startup.cs`:

```cs
using Westwind.AspNetCore.LiveReload;
```
  
#### Startup.ConfigureServices()
Start with the following in `Startup.ConfigureServices()`:

```cs
services.AddLiveReload(config =>
{
    // optional - use config instead
    //config.LiveReloadEnabled = true;
    //config.FolderToMonitor = Path.GetFullname(Path.Combine(Env.ContentRootPath,"..")) ;
});

// for ASP.NET Core 3.0 add Runtime Razor Compilation
// services.AddRazorPages().AddRazorRuntimeCompilation();
// services.AddMvc().AddRazorRuntimeCompilation();
```

The `config` parameter is optional and it's actually recommended you set any values via configuration (see below). 

> #### Enable ASP.NET Core 3.0 Runtime Razor View Compilation
> **ASP.NET Core 3.0 by default doesn't compile Razor views at runtime**, so any changes to Razor Views and Pages will not auto-reload in 3.0. You need to add the `Microsoft.AspNetCore.Mvc.Razor.RuntimeCompilation` package, and explicitly enable runtime compilation in `ConfigureServices()`:
> ```cs
> services.AddRazorPages().AddRazorRuntimeCompilation();
> services.AddMvc().AddRazorRuntimeCompilation();
> ```

#### Startup.Configure()
In `Startup.Configure()` add: 
 
```cs
// Before any other output generating middleware handlers including error handlers
app.UseLiveReload();

app.UseStaticFiles();
app.UseMvcWithDefaultRoute();
```

anywhere before the MVC route. I recommend you add this early in the middleware pipeline before any other output generating middleware runs as it needs to intercept any HTML content and inject the Live Reload script into it.

And you can use these configuration settings:

```json
{
  "LiveReload": {
    "LiveReloadEnabled": true,
    "ClientFileExtensions": ".cshtml,.css,.js,.htm,.html,.ts,.razor,.custom",
    "ServerRefreshTimeout": 3000,
    "WebSocketUrl": "/__livereload",
    "WebSocketHost":null, 
    "FolderToMonitor": "~/"
  }
}
```

All of these settings are optional.

* **LiveReloadEnabled**  
If this flag is false live reload has no impact as it simply passes through requests.  
*The default is:* `true`.

   > I recommend you put: `"LiveReloadEnabled": false` into `appsettings.json` and `"LiveReloadEnabled": true` into `appsettings.Development.json` so this feature isn't accidentally enabled in Production.


* **ClientFileExtensions**  
File extensions that the file watcher watches for in the Web project. These are files that can refresh without a server recompile, so don't include source code files here. Source code changes are handled via restarts with `dotnet watch run`.

* **FileInclusionFilter**
This filter allows to control whether a file change should cause the browser to refresh. This is useful to explicitly exclude files or files in entire sub-folders that shouldn't cause the browser to refresh, even though they are included in the file extension list for refreshes. 

The `path` passed in is a full OS path.

```csharp
services.AddLiveReload(config => {
    config.FileInclusionFilter = path =>
    {
        // don't trigger refreshes for files changes in the LocalizationAdmin sub-folder
        if (path.Contains("\\LocalizationAdmin", StringComparison.OrdinalIgnoreCase))
            return FileInclusionModes.DontRefresh;

        // explicitly force a file to refresh without regard to file extension et al. rules
        // are checked before the configuration file filter is applied
        if (path.Contains("/customfile.mm"))
            return FileInclusionModes.ForceRefresh;
            
        // continue regular file extension list filtering
        return FileInclusionModes.ContinueProcessing;
    };
})
```

* **RefreshInclusionFilter**  
This filter lets you control whether a URL should refresh or not. This setting is useful for excluding individual files or folders from auto-refreshing in the browser. 

The `path` passed in is a Root Relative Web Path.

```cs
services.AddLiveReload(config =>
{                
    // config.LiveReloadEnabled = true;   ideally set this in appsettings.json
    config.RefreshInclusionFilter = path =>
    {
        // don't refresh files on the client in the /LocalizationAdmin folder
        if (path.Contains("/LocalizationAdmin", StringComparison.OrdinalIgnoreCase))
            return RefreshInclusionModes.DontRefresh;
    
        return RefreshInclusionModes.ContinueProcessing;
    };
});    
```

* **ServerRefreshTimeout**
Set this value to get a close approximation how long it takes your server to restart when `dotnet watch run` reloads your application. This minimizes how frequently the client page monitors for the Web socket to become available again after disconnecting.

* **WebSocketUrl**  
The site relative URL to the Web socket handler.

* **WebSocketHost**  
An explicit WebSocket host URL. Useful if you are running on HTTP2 which doesn't support WebSockets (yet) and you can point at another exposed host URL in your server that serves HTTP1.1. Don't set this unless you have to - the default uses the current host of the request. Example: `wss://localhost:5200`

* **FolderToMonitor**  
This is the folder that's monitored. By default it's `~/` which is the Web Project's content root (not the Web root). Other common options are: `~/wwwroot` for Web only, `~/../` for **the entire solution**, or `~/../OtherProject/` for **another project** (which works well for client side Razor).

## Try it out
So to check out this functionality you can run the simple stock ASP.NET Core sample project. Let's demonstrate the three common live reload scenarios:

* Updating Static Files
* Updating Razor Views/Pages
* Updating on server code changes


### Update Static Files

* Start the application (recommend `dotnet watch run` but not required)
* Open the Index Page
* Open `wwwroot/css/site.css'
* Make a change in the CSS  (change the Font-size in the first `html` entries)
* Save the file

You should see the change reflected immediately. Sometime you may have to refresh once to get the cache to reset for CSS changes to show, but subsequent refreshes should show immediately.


### Update Razor Views

* Start the application (recommend `dotnet watch run` but not required)
* Open the Index Page
* Open `Views/Home/Index.cshtml`
* Make a change in the Header text - `Welcome Live Reload`
* Save the file

You should see the change reflected immediately.


### Server Changes

* Start the application with `dotnet watch run` (required or you need to manually restart)
* Open the Index Page
* Open `Controllers/HomeController.cs`
* Make a change in `ViewBag.Message`
* Save the file

The page will refresh but it will take a while as the server has to restart. Typically 3-5 seconds or so for a simple project, longer for more complex projects obviously.

You may have to tweak the `ServerRefreshTimeout` value to account for the time your server takes to restart to get a reliable refresh.

## HTTP2 Support
If you're using this extension with HTTP2 connections make sure you set your connections to support **both Http1 and Http2**. WebSockets don't work over HTTP2, so you need to also expose HTTP1 endpoints.

To do this you you can use this in your startup Builder configuration:

```cs
public static IWebHostBuilder CreateWebHostBuilder(string[] args) =>
    WebHost.CreateDefaultBuilder(args)
        .ConfigureKestrel(options =>
        {
            options.ConfigureEndpointDefaults(c => c.Protocols = HttpProtocols.Http1AndHttp2);
        })
        .UseStartup<Startup>();
```

The important bit is `c.Protocols = HttpProtocols.Http1AndHttp2`.

## Blazor Support?
Several people have asked about Blazor support and yes this tool can provide refresh to Blazor applications and yes it can - sort of. 

### Server Side Blazor
If you are using a server side Blazor project you can just use `dotnet watch run` which automatically provides browser refresh (unreliable but it sort of works). You'll need to add:

```xml
<ItemGroup>
    <Watch Include="**\*.razor" />
</ItemGroup>
```

and that should work. In my experience this is really flakey though and you can double that up with this Live Reload addin which will also refresh the page when the project restarts.

### Client Side Blazor
For client side Blazor the story is more complex and there's no real good solution for quick auto-reload, because client side blazor has no individual page recompile, but has to completely recompile the blazor project. 

Live Reload can work with this but it's slow as both the Blazor project has to be recompiled and the server project restarted (don't know if there's a way to just trigger a recompile of the client project on its own - if you think of a way please file an issue so we can add that!)

The following is based on the default project template that uses two projects for a client side blazor: The ASP.NET Core hosting project and the Blazor client project.


* Add LiveReload to the **ASP.NET Core Server Project**
* Set up monitoring for the entire solution (or the Blazor Project only)
* Add the Blazor extension

You can do this in configuration via:

```json
{
  "LiveReload": {
    "LiveReloadEnabled": true,
    "ClientFileExtensions": ".css,.js,.htm,.html,.ts,.razor,.cs",
    "FolderToMonitor": "~/.."
  }
}
```

This adds the `.razor,.cs` extensions and it basically monitors the entire Solution (`~/..`) for changes. Alternately you can also point at the Blazor project instead:

```json
"FolderToMonitor": "~/../MyBlazorProject"
```

Since Blazor projects tend to not care about the .NET Core backend that just acts as static file service you probably only need to monitor the client side project in Blazor projects. Either the entire solution or Blazor project folders work.

* Start the application with `dotnet watch run` (required or you need to manually restart)
* Open the Index Page
* Open `Pages/Index.razor`
* Make a change in the page
* Save the file

Reload will not be quick because the Blazor client project **and** the .NET Core project will recompile and restart. For a simple hello world it takes about 5 seconds on my local setup. For a full blown applications this may be even slower.

Obviously this is not ideal, but it's better than nothing. Live Reload works as it should but the underlying problem is that the actual content is not refreshing quickly enough to make this really viable.

We can only hope Microsoft come up with a built-in solution to trigger the recompilation of the client project or better yet recompilation of a single view as it's changed. 


## Change Log

### Version 0.2.11

* **Fix: Now refreshes Developer Error Page**  
When an error page using `app.UseDeveloperExceptionPage();` is displayed, the page is now refreshed when you make any changes to fix your error. The error page refreshes using the original URL that caused the error and should display the fixed page - or not if you mucked it up  :smile:.

### Version 0.2.10

* **Add RefreshInclusionFilter Configuration Property** 
This setting allows you to programmatically exclude a file or folder from live refreshing. The `Func<string, RefreshInclusionModes>` hook allows you to explicitly create rules to include or exclude files or entire folders from processing.

* **Add FileInclusionFilter Configuration Property**  
Adds a configuration `Func<string, FileInclusionModes>` hook to allow explicitly specifying whether files are checked for change notifications. You can either include or reject files in this hook function which gives precise control over what files are monitored and passed through for triggering browser refresh.   
*Note: this feature was added by PR in 0.2.7, but has been changed to return an `FileInclusionModes` enum for more explicit options rather than the original `bool`*.

### Version 0.2.4

* **Better preservation of scroll position in Refresh script**  
Changed the client script to use `location.reload()` instead of a force refresh with `location.reload(true)`. The forced refresh also forces the top of the page to be displayed, while reload preserves scroll position in most browsers (Edge Chromium not for some reason)

* **Add Lifecycle Management CancellationToken to Web Socket Read**  
Added the cancellation token to the `WebSocket.Receive()` operation so the socket can safely abort when the application is shut down. Previously this was causing problems in some application that used ASP.NET Core Lifecycle operations to not fire those event due to the still active WebSocket timing out before the app would shut down.

* **Fix: Rendering Errors when Live Reload Enabled**  
Fix issue where rendering was failing due to a missing `await` on output generation which would intermittently cause pages to fail. Fixed.

* **Fix: Errors with Concurrency for WebSocket Storage**   
Switched to ConcurrentDictionary for the Web Socket list storage.

### Version 0.1.17

* **Delay load injected WebSocket Script Code**   
Change the injected WebSocket script code so it delay loads to avoid potential page load hangs or dual updates.

* **Update Response Rewrite to use IHttpResponseStreamFeature**  
Update internal code used to rewrite response data for HTML content using the recommended HTTP Features rather than rewriting the Response.Body stream directly.

#### Version 0.1.14

* **Change Targeting to .NET Core 2.1 and 3.1**  
Changed targets to the LTS releases of .NET Core. Also changed dependencies to `Microsoft.AspNetCore.App` to reference all base ASP.NET Core base dependencies for better update package management support for apps integrating with this library.

* **Update Samples**  
Updated the samples to be easier to use and provide links to files that can be edited so it's easier to try out the sample and see live reloading work. Also re-targeted the sample app to .NET Core 2.2 and 3.1 (two separate projects due to separate ASP.NET configuration config).


#### Version 0.1.07

* **Add explicit support for .NET Core 3.0**  
Add a .NET Core 3.0 target to the NuGet package, to minimize package resolution issues.

* **Add LiveReloadWebServer Chocolatey Package**  
Add support for a deployed fully self-contained [Chocolatey Package](https://chocolatey.org/packages/LiveReloadWebServer).

#### Version 0.1.5.4

* **[Add new standalone LiveReloadServer Dotnet Tool](LiveReloadServe/README.md)**  
Added a new generic live reload server that's used that can be used to serve generic static HTML content and loose Razor Pages out of a folder. It's basically a standalone static Web Server which by default has Live Reload enabled. Although meant as a local server/tool, it can also be used like any other ASP.NET Core application and run behind a Web server like IIS or NGINX.

* **Fix issues with .NET Core 3.0**  
Fixed various compatibility issues related to API changes in .NET Core 3.0. Library now works with .NET Core 3.0.

#### Version 0.1.5.2

* **Fix bug with Static HTML Files Content Length**  
Fix `Response.ContentLength` by setting to `null` to force contentlength be calculated. Static files set this value to a fixed length and when we re-write this breaks the length. Fixed.

* **Update ASP.NET Core Packages to 2.1.1**  
Updated all core dependencies to use patched 2.1.1. Stick with 

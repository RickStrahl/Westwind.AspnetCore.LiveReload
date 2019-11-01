# Live Reload Middleware for ASP.NET Core

[![NuGet](https://img.shields.io/nuget/v/Westwind.AspnetCore.LiveReload.svg)](https://www.nuget.org/packages/Westwind.AspnetCore.LiveReload/)
![](https://img.shields.io/nuget/dt/Westwind.AspnetCore.LiveReload.svg)

This project provides

* **Live Reload Middleware Component**  
Add the middleware to an existing Web UI Project to provide Live Reload functionality that causes the active page to reload if a file is changed.

* **[Generic Static and Razor File  Web Server with Live Reload as a Dotnet Tool](LiveReloadServer%2FREADME.md)**  
There's also a [Dotnet Tool](https://www.nuget.org/packages/LiveReloadServer/) and [Chocolatey Package](https://chocolatey.org/packages/LiveReloadWebServer) that provide a generic Static File and Razor Page Web Server with automatically enabled Live Reload functionality. Simply do `LiveReload` in folder with static HTML resources and you can serve the pages. Make changes and see the the changes reflected immediately. There's separate information in the [LiveReload Server](LiveReloadServer/README.md) project.

## Middleware
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
* Add `app.UseLiveReload()` in `Startup.Configure()`
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
// Before any other output generating middleware handlers
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
    "WebSocketHost": "ws://localhost:5000",
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

* **ServerRefreshTimeout**  
Set this value to get a close approximation how long it takes your server to restart when `dotnet watch run` reloads your application. This minimizes how frequently the client page monitors for the Web socket to become available again after disconnecting.

* **WebSocketUrl**  
The site relative URL to the Web socket handler.

* **WebSocketHost**  
An explicit WebSocket host URL. Useful if you are running on HTTP2 which doesn't support WebSockets (yet) and you can point at another exposed host URL in your server that serves HTTP1.1. Don't set this unless you have to - the default uses the current host of the request.

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

#### Version 1.7

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
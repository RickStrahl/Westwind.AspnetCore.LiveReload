
# Live Reload Middleware for ASP.NET Core

This is a Live Reload Middleware component that monitors file changes in your project and automatically reloads the browser's active page when a change is detected.

It works with:

* Client side static code files 
* ASP.NET Core Views/Pages (.cshtml)
* Server Side Code Updates (combined w/ `dotnet watch`)


The Middleware is self-contained and has no external dependencies - nothing else to run. 

> Current releases are an early prototype.

Minimum Requirements:

* ASP.NET Core 2.1

## Install from NuGet
You can install this middleware from:

```ps
PS> Install-Package WestWind.AspnetCore.LiveReload
```

## What does it do?
This middleware monitors for file changes in your project and tries to automatically refresh your browser when a change is detected. It uses a File Watcher to monitor for file changes, and a WebSocket connection in the browser to refresh the page. The middleware intercepts all HTML page requests and injects a block of code that hooks up the WebSocket interface to support the 'remote' refresh operation. 

This tool uses raw Web Sockets so it's very light weight with no additional library dependencies. You can also turn off Live Reload with a configuration setting in which case the middle ware is not hooked up at all.

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

Start with the following in `Startup.ConfigureServices()`:

```cs
services.AddLiveReload(config =>
{
    // optional - use config instead
    //config.LiveReloadEnabled = true;
    //config.FolderToMonitor = Path.GetFullname(Path.Combine(Env.ContentRootPath,"..")) ;
});
```

The parameter is optional and it's actually recommended you set any values via configuration (see below).

In `Startup.Configure()` add: 
 
```cs
app.UseLiveReload();
```

anywhere before the MVC route. I recommend you add this early in the middleware pipeline before any other output generating middleware runs as it needs to intercept any HTML content and inject the Live Reload script into it.

And you can use these configuration settings:

```json
{
  "LiveReload": {
    "LiveReloadEnabled": true,
    "ClientFileExtensions": ".cshtml,.css,.js,.htm,.html,.ts,.custom",
    "ServerRefreshTimeout": 3000,
    "WebSocketUrl": "/__livereload"
  }
}
```

All of these settings are optional.

* **LiveReloadEnabled**  
If this flag is false live reload has no impact as it simply passes through requests. `true` by default.

* **ClientFileExtensions**  
File extensions that the file watcher watches for in the Web project. These are files that can refresh without a server recompile, so don't include source code files here. Source code changes are handled via restarts with `dotnet watch run`.

* **ServerRefreshTimeout**  
Set this value to get a close approximation how long it takes your server to restart when `dotnet watch run` reloads your application. This minimizes how frequently the client page monitors for the Web socket to become available again after disconnecting.

* **WebSocketUrl**  
The site relative URL to the Web socket handler.

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







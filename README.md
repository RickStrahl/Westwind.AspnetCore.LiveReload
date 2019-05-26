
# West Wind Live Reload Middleware for ASP.NET Core

This is a Live Reload Middleware component that monitors file changes in your project and automatically reloads the browser's active page.

It works with:

* Client side static code files 
* ASP.NET Core Views (.cshtml)
* Server Side Code Updates (combined w/ `dotnet watch`)


The Middleware is self-contained and has no external dependencies except some of the base ASP.NET Core libraries. 

> Current releases are an early prototype.

## Install from NuGet
You can install this middleware from:

```ps
PS> Install-Package WestWind.AspnetCore.LiveReload
```

## What does it do?
This middleware monitors for file changes in your project and tries to automatically refresh your browser when a change is detected. It uses a file watcher to monitor for file changes and a WebSocket connection in the browser to refresh the page. The middleware intercepts all HTML page requests and injects a block of code that hooks up the WebSocket interface to support the 'remote' refresh operation.

In order to restart the server on server code changes you need to run `dotnet watch run`. This built-in tool automatically restarts your .NET Core application anytime a code change is made. The Live Reload middleware works without `dotnet watch run` to refresh client and view changes but for server code changes the 

### Configuration
The full configuration and run process looks like this:

* Add `services.AddLiveReload()` in `Startup.ConfigureServices()`
* Add `app.UseLiveReload()` in `Startup.Configure()`
* Run `dotnet watch run` to run your application

*Note: `dotnet watch run` is optional but if you don't run it you have to manually restart your server for each server code change. Without static file and Razor View/Page changes still auto-refresh*

Add the namespace:

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



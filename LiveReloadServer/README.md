# Live Reload WebServer Dotnet Tool

This is a self-contained Web Server for serving static HTML and loose Razor files that automatically includes Live Reload functionality. 

* Generic Static File Web Server you can launch in any folder
* Just start `LiveReloadServer` in a folder or specify `--webroot` folder
* Also supports Razor Pages that don't have external dependencies
* Automatic LiveReload functionality for change detection and browser refresh
* Options to customize location, port, files checked etc.
* Easily installed and updated with `dotnet tool -g install LiveReloadServer`

This Dotnet Tool is a generic local Web Server that you can start in **any folder** to provide simple and quick HTTP access. You can serve static resoures as well as loose Razor Pages as long as those Razor Pages don't require external dependencies.

Live Reload is enabled by default and checks for changes to common static files as well as Razor pages. If a checked file is changed, the browser's current page is refreshed. 

You can also use this 'generic' server behind a live Web Server by using installing the main project as a deployed Web application.

### Requirements
* Dotnet Core SDK 3.0+
* If optionally hosting requires a Web Server that supports WebSockets

## Installation
You can install this server as a .NET Tool using Dotnet SDK Tool installation:

```powershell
dotnet install -g LiveReloadServer
```

To use it, navigate to a folder that you want to serve HTTP files out of:

```ps
# will serve files out of https://localhost:5200
LiveReloadServer
```

### Launching the Web Server
You can use the command line to customize how the server runs. By default files are served out of the current directory but you can override the `WebRoot` folder.

Use commandlines to customize:

```ps
LiveReloadServer --WebRoot "c:/temp/My Web Site" --port 5200 --useSsl False
```

There are a number of Configuration options available:


```text
Live Reload Server
------------------
(c) Rick Strahl, West Wind Technologies, 2019

Static and Razor File Service with Live Reload for changed content.

Syntax:
-------
LiveReloadServer  <options>

Commandline options (optional):

--WebRoot            <path>  (current Path if not provided)
--Port               5200*
--UseSsl             True*|False
--LiveReloadEnabled  True*|False
--RazorEnabled       True*|False
--OpenBrowser        True*|False
--DefaultFiles       ""index.html,default.htm,default.html""

Live Reload options:

--LiveReload.ClientFileExtensions   "".cshtml,.css,.js,.htm,.html,.ts""
--LiveReload ServerRefreshTimeout   3000,
--LiveReload.WebSocketUrl:          ""/__livereload""

Configuration options can be specified in:

* Environment Variables with a `LiveReloadServer` prefix. Example: 'LiveReloadServer_Port'
* Command Line options as shown above

Examples:
---------
LiveReload --WebRoot ""c:\temp\My Site"" --port 5500 --useSsl false

$env:LiveReload_Port 5500
LiveReload
```

You can also use Environment variables to set these save options by using a `LiveReloadServer_` prefix:

```ps
$env:LiveReload_Port 5500
LiveReload
```
## Static Files
The Web Server automatically serves all static files and Live Reload is automatically enabled unless explicitly turned off. Making a change to any static file causes the current HTML page loaded in the browser to be reloaded.

## Razor Files
You can also use 'loose Razor Files' in the designated folder, which means you can use `.cshtml` Razor Pages with this server with single file functionality. There is support for Layout pages, ViewStart, ViewImport, partials etc. 

But there's no support for:

* No compiled Source Code files (.cs)
* No external Package/Assembly loading

### Razor Limitations
Razor Pages served are limited to **self-contained single file Pages** as no code outside of a Page can be compiled at runtime, or even reference an exeternal package/assembly that isn't installed in the actually server's start folder.

**Essentially you're limited to using just the built-in .NET Framework/Core ASP.NET features.**

The goal of this tool isn't to provide the full Razor Pages environment here - if that's what you need build a proper ASP.NET Core Web application. Rather it's meant to provide just **slightly enhanced static page like behavior** for otherwise mostly static Web functionality.
  
Also keep in mind this is meant as a generic **local** server and although you can in theory be hosted on a Web site, the primary use case for this library is local hosting either for testing or for integration into local (desktop) applications that might require visual HTML content.

More might be possible in this generic server, but currently that has not been explored. If that's of interest to you or you want to contribute, please file an issue to discuss and explore the use cases and what might be possible. As it stands for now, Razor functionality is kind of a **gimmicky, limited use scenario** that works for basic use cases.

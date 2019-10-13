# Live Reload WebServer Dotnet Tool

This is a self-contained Web Server for serving static HTML and loose Razor files that automatically includes Live Reload functionality. 

* Generic Static File Web Server you can launch in any folder
* Just start `LiveReloadServer` in a folder or specify `--webroot` folder
* Automatic LiveReload functionality for change detection and browser refresh
* Options to customize location, port, files checked etc.
* Easily installed and updated with `dotnet tool -g install LiveReloadServer`
* Also supports Razor Pages that don't have external dependencies

You can grab the compiled tool as:

* [Dotnet Tool](https://www.nuget.org/packages/LiveReloadServer/)   
  `dotnet tool install -g LiveReloadServer`
* [Chocolatey Package](https://chocolatey.org/packages/LiveReloadWebServer)   
 `choco install LiveReloadWebServer`
* [Direct Single File Download](https://github.com/RickStrahl/Westwind.AspnetCore.LiveReload/raw/master/LiveReloadServer/LiveReloadWebServer.exe)

> All three versions have the same features and interface, just the delivery mechanism is different. However, the Chocolatey and Standalone EXE versions **do not have Razor enabled** in order to keep the size down.  You can build your custom versions that build a Razor enabled version.
  
### What does it do?
This tool is a generic local Web Server that you can start in **any folder** to provide simple and quick HTTP access. You can serve static resoures as well as loose Razor Pages as long as those Razor Pages don't require external dependencies.

Live Reload is enabled by default and checks for changes to common static files. If a checked file is changed, the browser's current page is refreshed. You can map additional extensions that trigger the LiveReload.

You can also use this 'generic' server behind a live Web Server by using installing the main project as a deployed Web application.

### Requirements
* Dotnet Tool: Dotnet Core SDK 3.0+
* Chocolatey or 
* If optionally hosting requires a Web Server that supports WebSockets

## Installation
You can install this server as a .NET Tool using Dotnet SDK Tool installation:

```powershell
dotnet install -g LiveReloadServer
```

To use it, navigate to a folder that you want to serve HTTP files out of:

```ps
# will serve current folder files out of http://localhost:5200
LiveReloadServer

# specify a folder instead of current folder and a different port
LiveReloadServer --webroot "c:/temp/My Local WebSite" --port 5350 -UseSsl

# Customize some options
LiveReloadServer --LiveReloadEnabled False --OpenBrowser False -UseSsl -UseRazor
```

You can also install from Chocolatey in which case there are no dependencies (but you won't get Razor support):

```ps
choco install LiveReloadWebServer
```

### Launching the Web Server
You can use the command line to customize how the server runs. By default files are served out of the current directory on port `5200`, but you can override the `WebRoot` folder.

Use commandlines to customize:

```ps
LiveReloadServer --WebRoot "c:/temp/My Web Site" --port 5200 -useSsl
```

There are a number of Configuration options available:

```text
Syntax:
-------
LiveReloadServer  <options>

--WebRoot            <path>  (current Path if not provided)
--Port               5200*
--UseSsl             True|False*
--ShowUrls           True|False*
--UseRazor           True|False* (only available in .NET Tool)
--OpenBrowser        True*|False
--DefaultFiles       "index.html,default.htm"*
--Extensions         Live Reload Extensions monitored
                     ".css,.js,.htm,.html,.ts"*

Configuration options can be specified in:

* Command Line options as shown above
* Logical Command Line Flags for true can be set with -UseSsl or -UseRazor
* Environment Variables with `LiveReloadServer_` prefix. Example: 'LiveReloadServer_Port'
* You use -UseSsl without True or False to set a logical value to true

Examples:
---------
LiveReload --WebRoot "c:\temp\My Site" --port 5500 -useSsl -useRazor --openBrowser false

$env:LiveReloadServer_Port 5500
$env:LiveReloadServer_WebRoot c:\mySites\Site1\Web
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

In short, this is not meant to be an Application Development environment, but rather provide **static pages with benefits**.

Some things you can do that are useful:

* Update a Copyright notice year with `2017-@DateTime.Now.Year`
* Read authentication values
* Check versions of files on disk to display version number for downloads

All these things use intrinsic built in features which while limited to generic functionality are still very useful for simple scripting scenarios.

### Razor Limitations
Razor Pages served are limited to **self-contained single file Pages** as no code outside of a Page can be compiled at runtime, or even reference an external package/assembly that isn't installed in the actually server's start folder.

**Essentially you're limited to using just the built-in .NET Framework/Core ASP.NET features.**

* No support for external libraries or NuGet Packages (only what is compiled in)
* No data access support
* No compiled code files (no .cs)

The goal of this tool isn't to provide the full Razor Pages environment - if that's what you need build a proper ASP.NET Core Web application. Rather it's meant to provide just **slightly enhanced static page like behavior** for otherwise mostly static Web functionality.
  
Also keep in mind this is meant as a generic **local** server and although you can in theory be hosted on a Web site, the primary use case for this library is local hosting either for testing or for integration into local (desktop) applications that might require visual HTML content.

More might be possible in this generic server, but currently that has not been explored. If that's of interest to you or you want to contribute, please file an issue to discuss and explore the use cases and what might be possible. As it stands for now, Razor functionality is kind of a **gimmicky, limited use scenario** that works for basic use cases.

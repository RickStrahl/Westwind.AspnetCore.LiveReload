# Live Reload WebServer Dotnet Tool

[![NuGet](https://img.shields.io/nuget/v/LiveReloadServer.svg)](https://www.nuget.org/packages/LiveReloadServer/)
[![](https://img.shields.io/nuget/dt/LiveReloadServer.svg)](https://www.nuget.org/packages/LiveReloadServer/)

[![NuGet](https://img.shields.io/chocolatey/v/livereloadwebserver.svg)](https://chocolatey.org/packages/livereloadwebserver) [![](https://img.shields.io/chocolatey/dt/livereloadwebserver.svg)](https://chocolatey.org/packages/livereloadwebserver)

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
* [Direct Single File Download (zipped)](https://github.com/RickStrahl/Westwind.AspnetCore.LiveReload/raw/master/LiveReloadServer/LiveReloadWebServer.zip)

> All three versions have the same features and interface, just the delivery mechanism and the executable name is different. The EXE uses `LiveReloadWebServer` while the Dotnet Tool uses `LiveReloadServer`.
  
### What does it do?
This tool is a generic **local Web Server** that you can start in **any folder** to provide simple and quick HTTP access to HTML and other Web resources. You can serve any static resources - HTML, CSS, JS etc. - as well as loose Razor Pages that don't require any code behind or dependent source code. 

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

You can also install from Chocolatey:

```ps
choco install LiveReloadWebServer
```

Note that EXE filename is `LiveReloadWebServer` which is different from the Dotnet Tool's `LiveReloadServer` so they can exist side by side without conflict.

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
--UseRazor           True|False*
--ShowUrls           True|False*
--OpenBrowser        True*|False
--DefaultFiles       "index.html,default.htm"*
--Extensions         ".cshtml,.css,.js,.htm,.html,.ts"*
--Environment        Production*|Development

Configuration options can be specified in:

* Command Line options as shown above
* Logical Command Line Flags for true can be set with -UseSsl or -UseRazor
* Environment Variables with `LiveReloadServer_` prefix. Example: 'LiveReloadServer_Port'
* You use -UseSsl without True or False to set a logical value to true

Examples:
---------
LiveReloadServer --WebRoot "c:\temp\My Site" --port 5500 -useSsl -useRazor --openBrowser false

$env:LiveReloadServer_Port 5500
$env:LiveReloadServer_WebRoot c:\mySites\Site1\Web
LiveReloadServer
```

You can also use Environment variables to set these save options by using a `LiveReloadServer_` prefix:

```ps
$env:LiveReload_Port 5500
LiveReload
```
## Static Files
The Web Server automatically serves all static files and Live Reload is automatically enabled unless explicitly turned off. Making a change to any static file causes the current HTML page loaded in the browser to be reloaded.

You can specify explicit file extensions to monitor using the `--Extensions` switch. The default is: `".cshtml,.css,.js,.htm,.html,.ts"`.

> #### Slow First Time Razor Startup
> First time Razor Page startup can be slow. Cold start requires the Razor Runtime to load the compiler and related resources so the very first page hit can take a few seconds before the Razor page renders. Subsequent page compilation is faster but still 'laggy' (few hundred ms), and previously compiled pages run very fast at pre-compiled speed.

## Razor Files
LiveReloadServer has **basic Razor Pages support**, which means you can create **single file, inline Razor content in Razor pages** as well as use Layout, Partials, ViewStart etc. in the traditional Razor Pages project hierarchy. As long as **all code** is inside of `.cshtml` Razor pages it should work.

### No Compiled C# Code
However, there's **no support for code behind razor models** or  **loose C# .cs compilation** as runtime compilation outside of Razor is not supported. All dynamic compilable code has to live in Razor `.shtml` content.

### External Assembly Support
You can however add **external assemblies** to support external code in your site, by adding final dependent assemblies (not NuGet packages!) into a `./privatebin` folder below your WebRoot. Assemblies in this folder will be loaded when the site is launched and become available for access in your Razor page code.

## Using Razor Features
To serve a Razor page create a page that uses some .NET code using C# Razor syntax. For example here's a `hello.cshtml`:

```html
@page
<html>
<body>
<h1>Hello World</h1>

<p>Time is: @DateTime.Now.ToString("hh:mm:ss tt")</p>

<hr>

@{
    var client = new System.Net.WebClient();
    var xml = await client.DownloadStringTaskAsync("https://west-wind.com/files/MarkdownMonster_version.xml");
    var start = xml.IndexOf("<Version>") + 9;        
    var end = xml.LastIndexOf("</Version>");
    var version = xml.Substring(start, end - start);
}

<h3>Latest Markdown Monster Version: @version</h3>
<hr>

</body>
</html>
```

Assuming you place this into the `--WebRoot` folder that's the root you can then access this page with:

```
http://localhost:5200/hello
```

Note it's the name of the Razor Page **without the extension**. If you create the `.cshtml` in a sub-folder, just provide the path name:

```
http://localhost:5200/subfolder/hello
```

Same as you would expect with Razor Page in full ASP.NET Core applications.

I want to stress though, that this is a limited Razor Pages implementation that is not meant to substitute for a full ASP.NET Core Razor Application. Since there's no code behind compilation or ability to 

### Files and Folder Support
As mentioned above you can use most Razor Pages file based constructs like _Layout and Partial pages, ViewStart as well as shared folders etc. The root folder works like any other Razor Pages folder or Area in ASP.NET Core and so all the relative linking and child page access are available.

### Error Page
When an error occurs errors are fired into an `Error.cshtml` page. You have to create this page as the tool uses a folder that you provide. 

You can create an error page and return error information like this using the HttpContext features to retrieve an `IExceptionHandlerPathFeature`

```html
@page

<html>
<body>
<h1>Razor Pages Error Page</h1>
<hr/>
<div style="font-size: 1.2em;margin: 20px;">
    Yikes. Something went wrong...
</div>
@{
            var errorHandler = HttpContext
                .Features
                .Get<Microsoft.AspNetCore.Diagnostics.IExceptionHandlerFeature>();
}
<hr/>
@if(errorHandler != null )
{
            var error = errorHandler.Error;
            var message = error?.Message;
            if (message == null)
              message = "No Errors found.";

            <text>            
            @message     
            </text>

            <pre>
            @error?.StackTrace
            </pre>
}
</body>
</html>
```

> @icon-info-circle Set Development Environment
> Note you can set the Development Environment by setting the `LIVERELOADWEBSERVER_ENVIRONMENT` variable to `Production` or `Development`. In Development mode it will show the error information above. The default is production.

### Non-Razor Page Code is not supported
The following code execution features are not available:

* No code behind model code (.cs)
* No compiled source code files (.cs)

You can however load external assemblies by placing assemblies in `./privatebin` of the web root.

I want to stress that LiveReloadServer is not meant to replace RazorPages or a full ASP.NET Core application - it is meant as a local or lightweight static site Web Server with 'benefits' of some dynamic code execution. But it's not well suited to building a business application!

### Load External Assemblies
It's also possible to pull in additional assemblies that can then be accessed in the Razor Pages. To do this:

* Create a `./privateBin` folder in your specified WebRoot folder
* Add any **assemblies** and their dependencies there

You have to use **assemblies** rather than NuGet packages and you are responsible for adding all required dependencies in the `./privatebin` folder in order for the application to run. For example, if I wanted to add `Westwind.AspNetCore.Markdown` for Markdown features I can add the `Westwind.AspNetCore.Markdown.dll`. However, that dll also has a dependency on `Markdig.dll` so that assembly has to be available in the `./PrivateBin` folder as well.

Finding all dependencies may be tricky since NuGet doesn't show you all `dll` dependencies, so this may require some sleuthing in a project's `project.dep.json` file in a `publish` folder.


### Use Cases for a Static Server with Benefits
Some things you can do that are useful:

* Update a Copyright notice year with `2017-@DateTime.Now.Year`
* Read authentication values
* Check versions of files on disk to display version number for downloads
* Download content from other Web sites to retrieve information or text

All these things use intrinsic built-in features of .NET or ASP.NET which, while limited to generic functionality, are still very useful for simple scripting scenarios.

Also keep in mind this is meant as a generic **local** server and although you can in theory host this generic server on a Web site, the primary use case for this library is local hosting either for testing or for integration into local (desktop) applications that might require visual HTML content and a Web server to serve local Web content.

### More Features?
The primary goal of LiveReload server is as a local server, not a hosted do-it-all solution. Other features may be explored but at the moment the feature set is well suited to the stated usage scenario I intended it for.

More features like dynamic compilation of loose C# code files at runtime might be possible in this generic server, but currently that has not been explored. Personally I think this goes against the simplicity of this solution. If you really have a need for complex code that requires breaking out of Razor Page script code, it's time to build a full ASP.NET Core RazorPages application instead of using this server. 

But that won't stop some from asking or trying to hook it up anyway I bet :smile:

If that's of interest to you or you want to contribute, please file an issue to discuss and explore the use cases and what might be possible.

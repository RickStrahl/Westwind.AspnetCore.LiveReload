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
* Markdown Page rendering to HTML with theming, Page template and Live Reload Support
* Self-contained Razor Pages support with Live Reload Support

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

--WebRoot               <path>  (current Path if not provided)
--Port                  5200*
--UseSsl                True|False*{razorFlag}
--ShowUrls              True|False*
--OpenBrowser           True*|False
--DefaultFiles          ""index.html,default.htm""*
--Extensions            "".cshtml,.css,.js,.htm,.html,.ts,.md""*
--Environment           Production*|Development

Razor Pages:
------------
--UseRazor              True|False*

Markdown Options:
-----------------
--UseMarkdown           True|False*  
--CopyMarkdownResources True|False*
--MarkdownTemplate      ""~/markdown-themes/__MarkdownTestmplatePage.cshtml""*


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
The Web Server automatically serves all static files and Live Reload is automatically enabled unless explicitly turned off. HTML pages, CSS and scripts, and any other specific files with extensions you add are autoamtically reloaded whenever you make a change to the files.

You can specify explicit file extensions to monitor using the `--Extensions` switch. The default is: `".cshtml,.css,.js,.htm,.html,.ts,.md"`.

## Markdown File Rendering
You can enable Markdown support in this server by setting `--UseMarkdown True`. This serves HTML content directly off any `.md` or `.mkdown` files in the Web root. The server provides default templates for the HTML styling, but you can override the rendering behavior with a **custom Razor template** that provides the chrome around the rendered Markdown, additional styling and syntax coloring.

Markdown pages are rendered as HTML and like other resources are tracked for changes. If you make a change to the Markdown document, the browser is refreshed and immediately shows that change. If you create a custom Razor template, changes in that are also detected and cause an immediate refresh.

For this feature to work with the default templates all you do is set the `--UseMarkdown True` command line switch. Then access any `.md` in the browser either by the `.md` or simply without an extension.
  
To access `README.md` in the WebRoot you would access:

* https://localhost:5200/README.md  
* https://localhost:5200/README

### Customizing Markdown Templates and Styling
Default styling for Markdown comes from a Razor template that is provided as part of the distribution in the install folder's `./templates/markdown-themes` directory. This folder is hoisted as `~/markdown-themes` into the Web site, which makes the default CSS and script resources available to the Web site. This folder by default is routed back to the launch (not Web) root and is used for all sites you run through this server.

There are several ways you can customize the Markdown styling and supporting resources:

* Copy the existing `markdown-themes` folder into your Web root and modify
* Create a custom Razor template

### Copy the Existing `markdown-themes` Folder
You can copy the `markdown-themes` folder into your Website either manually or more easily via the `--CopyMarkdownResources True` command line flag. When you use this flag, the `markdown-themes` folder will be copied from the launch root into your Web root **if it doesn't exist already**. 

Once moved to the new location in your web root folder, you can modify the `__MarkdownPageTemplate.cshtml` page and customize the rendering. For example, you can add a Layout page (if Razor support is enabled) to add site wide styling or you can modify the page theme and syntax coloring theme.

More on customization below.

### Using a custom Markdown Template
The template used for Markdown rendering is an MVC View template that is passed a `MarkdownModel` that contains the rendered markdown and a few other useful bits of information like the path, file name and title of the document. 

You can override the template used by using the `--MarkdownPageTemplate` command line switch:

```ps
LiveReloadServer --WebRoot ./website/site1 -UseMarkdown --MarkdownPageTemplate ~/MyMarkdownTemplate.cshtml
```

> Note although you can use a completely separate template, keep in mind that there's a bit of styling and scripts are required in order to render Markdown. For example, code snippets coloring needs a JavaScript library and text styling may require custom CSS. It'll render without but it won't look pretty without a bit of styling.

To give you an idea what a template should look like, here's what the default template looks like:

```html
@model Westwind.AspNetCore.Markdown.MarkdownModel
<html>
<head>
    @if (!string.IsNullOrEmpty(Model.BasePath))
    {
        <base href="@Model.BasePath" />
    }
    <title>@Model.Title</title>
    <!-- *** Markdown Themes: Github, Dharkan, Westwind, Medium, Blackout -->
    <link rel="stylesheet" href="~/markdown-themes/Dharkan/theme.css" />
    <link rel="stylesheet" href="~/markdown-themes/scripts/fontawesome/css/font-awesome.min.css" />
    <style>   
        pre > code {
            white-space: pre;
        }
    </style>
</head>
<body>
    <div id="MainContent">        
        @Model.RenderedMarkdown
    </div>

    <script src="~/markdown-themes/scripts/highlightjs/highlight.pack.js"></script>
    <script src="~/markdown-themes/scripts/highlightjs-badge.min.js"></script>
    
<!-- *** Code Syntax Themes: vs2015, vs, github, monokai, monokai-sublime, twilight -->
<link href="~/markdown-themes/scripts/highlightjs/styles/vs2015.css" rel="stylesheet" />
    <script>
        setTimeout(function () {
            var pres = document.querySelectorAll("pre>code");
            for (var i = 0; i < pres.length; i++) {
                hljs.highlightBlock(pres[i]);
            }
        });

    </script>
</body>
</html>
```

The model is passed as `MarkdownModel` and it contains the `.RenderedMarkdown` property which is the rendered HTML output (as an `HtmlString`). There's also the `.Title` which is parsed from the document based on a header if present. The model also contains the original Markdown and a YAML header if it was present.

#### Default Theme Overrides
If you stick with the default theming you can override:

* The overall render theme 
    ```html
    <!-- *** Markdown Themes: Github, Dharkan, Westwind, Medium, Blackout -->
    <link rel="stylesheet" href="~/markdown-themes/Dharkan/theme.css" />
    ```    
* The syntax coloring
    ```html
    <!-- *** Code Syntax Themes: vs2015, vs, github, monokai, monokai-sublime, twilight -->
    <link href="~/markdown-themes/scripts/highlightjs/styles/vs2015.css" rel="stylesheet" />
    ```

#### Completely Custom CSS Markup
You can create any HTML and CSS to render your Markdown of course if you prefer. The `markdown-themes` themes can give you a good start of things that you typically have to support in Markdown content so they offer a good starting point for your own themes. Pick a theme and customize, or if you are keen - go ahead and start completely clean.

## Razor Files
LiveReloadServer has **basic Razor Pages support**, which means you can create **single file, inline Razor content in Razor pages** as well as use Layout, Partials, ViewStart etc. in the traditional Razor Pages project hierarchy. As long as **all code** is inside of `.cshtml` Razor pages it should work.

> #### Slow First Time Razor Startup
> First time Razor Page startup can be slow. Cold start requires the Razor Runtime to load the compiler and related resources so the very first page hit can take a few seconds before the Razor page renders. Subsequent page compilation is faster but still 'laggy' (few hundred ms), and previously compiled pages run very fast at pre-compiled speed.

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

## Version History

### Version 0.2.2

* **Add Markdown File Support**  
Added support for optionally serving Markdown files as HTML from the local site. Markdown files are loaded as `.md`,`.markdown`, `.mkdown` or as extensionless URLs from the Web site and can participate in Live Reload functionality.

* **Add Console Application Icon** 
Added Console Application icon so application is easier to identify in the Task list and when running on the Desktop. 

* **Update the Sample Application**  
Updated the .NET Core 3.1 Sample application to properly display reference links. Add Markdown Example.

* **Fix: Server Timeout not respected**   
The server timeout was not respected previously and has been fixed to properly wait for the configured period before refreshing the browser instance.

* **Fix: Command Line Parsing for Logical Switches**  
Fix issue with logical switches like `-UseSSL` which were not properly working when configuration was present in the configuration file. Settings of the command line now properly override configuration setting in the config file.
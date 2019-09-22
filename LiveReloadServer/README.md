# Live Reload WebServer Dotnet Tool

This is a simple self-contained static file Web Server that automatically includes Live Reload functionality. Change to a folder with Web files, and launch `LiveReloadServer` to serve files in that folder. You can make changes to static files and see the changes reflected.

Live Reload Web Server for static and loose Razor files. Use it to host a local folder as a Web site and automatically refresh page content as content is changed. It's a quick and easy way to 'run' a local Web site and make interactive changes to it.

You can install this server as a .NET Tool using the Dotnet SDK

```powershell
dotnet install -g LiveReloadServer
```

To use it, navigate to a folder that you want to serve HTTP files out of:


```ps
# will serve files out of https://localhost:5000
LiveReloadServer
```

Use commandlines to customize:

```ps
LiveReloadServer --WebRootPath "c:/temp/My Web Site" --port 5200 --useSsl False
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
--Port               5000*
--UseSsl             True*|False
--LiveReloadEnabled  True*|False
--RazorEnabled       True*|False
--OpenBrowser        True*|False
--DefaultFiles       ""index.html,default.htm,default.html""

Live Reload options:

--LiveReload.LiveReloadEnabled      true*|false
--LiveReload.ClientFileExtensions   "".cshtml,.css,.js,.htm,.html,.ts""
--LiveReload ServerRefreshTimeout   3000,
--LiveReload.WebSocketUrl:          ""/__livereload""

Configuration options can be specified in:

* LiveReloadServer.json as JSON property values
* Environment Variables with a `LiveReloadServer` prefix. Example: 'LiveReloadServer_Port'
* Command Line options as shown above

Example:
---------
LiveReload --WebRoot ""c:\temp\My Site"" --port 5500 --useSsl false
```

You can also set configuration settings in a `LiveReloadServer.json`.
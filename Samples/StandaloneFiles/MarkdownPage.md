# Hello World from Markdown

This is a **sample test page** using  *Markdown text* to render HTML. This page is a plain .md file rendered on disk and turned into HTML as part of the Live Reload server code.

Here's a code snippet:

```cs
if (UseMarkdown)
{
    services.AddMarkdown(config =>
    {
        var folderConfig = config.AddMarkdownProcessingFolder("/","~/__MarkdownPageTemplate.cshtml");
        
        // Optional configuration settings
        folderConfig.ProcessExtensionlessUrls = true;  // default
        folderConfig.ProcessMdFiles = true; // default

    });
    
    // we have to force MVC in order for the controller routing to work                    
    services
        .AddMvc()
        .AddApplicationPart(typeof(MarkdownPageProcessorMiddleware).Assembly)
        .AddRazorRuntimeCompilation(
            opt =>
            {
                opt.FileProviders.Add(new PhysicalFileProvider(WebRoot));
            });
}
```

* List 1
* List 2
* List 3

This is really the behavior I'd like to see in markdown.


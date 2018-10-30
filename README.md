# Hangfire.Tags

[![Build status](https://ci.appveyor.com/api/projects/status/hqrtav24894dtjcp/branch/master?svg=true)](https://ci.appveyor.com/project/faceit/hangfire-tags/branch/master)
[![NuGet](https://img.shields.io/nuget/v/Hangfire.Tags.svg)](https://www.nuget.org/packages/Hangfire.Tags/)
![MIT License](https://img.shields.io/badge/license-MIT-orange.svg)

Inspired by the lack of searching and grouping, Hangfire.Tags provides a way to search and group different jobs. 

![sidemenu](Sidemenu.png)
![dashboard](Dashboard.png)

## Features

 - **100% Safe**: no Hangfire-managed data is ever updated, hence there's no risk to corrupt it.
 - **Attributes**: supports [Tag("{0}")] syntax for creating a default set of tags when creating a job.
 - **Extensions**: has extension methods on PerformContext, but also on string (for instance for adding tags to a jobid).
 - **Clean up**: uses Hangfire sets, which are cleaned when jobs are removed.

## Setup

In .NET Core's Startup.cs:
```c#
public void ConfigureServices(IServiceCollection services)
{
    services.AddHangfire(config =>
    {
        config.UseSqlServerStorage("connectionSting");
        config.UseTagsWithSql();
    });
}
```

Otherwise,
```c#
GlobalConfiguration.Configuration
    .UseSqlServerStorage("connectionSting")
    .UseTagsWithSql();
```

**NOTE**: If you have Dashboard and Server running separately, 
you'll need to call `UseTags()` or `UseTagsWithSql()` on both.

### Additional options

As usual, you may provide additional options for `UseTags()` method.

Here's what you can configure:
- **BackgroundColor** – default background color for the tags
- **TextColor** – default text color of the tags

**NOTE**: After you initially add Hangfire.Tags (or change the options above) you may need to clear browser cache, as generated CSS/JS can be cached by browser.

## Providers

In order to properly cleanup tags for expired jobs, an extension is required for the default storage providers. At this moment, only a provider for SQL Server is available.

## Tags

Hangfire.Tags provides extension methods on `PerformContext` object, 
hence you'll need to add it as a job argument. 

**NOTE**: Like `IJobCancellationToken`, `PerformContext` is a special argument type which Hangfire will substitute automatically. You should pass `null` when enqueuing a job.

Now you can add a tag:

```c#
public void TaskMethod(PerformContext context)
{
    context.AddTag("Hello, world!");
}
```

which results in the tag hello-world.

You can also add tags using attributes, either on the class, or on the method (or both!)

```c#
[Tag("TaskMethod")]
public void TaskMethod(PerformContext context)
{
    ....
}
```

## Search tags

In the Dashboard, when clicking on Jobs, you'll see a new menu item, called Tags. By default this page will show you all defined tags in the system. Clicking on a tag will show a list of all jobs with that tag attached.

## License

Copyright (c) 2018 2Face-IT B.V.

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.
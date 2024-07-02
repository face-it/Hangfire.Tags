# Hangfire.Tags

[![Build status](https://ci.appveyor.com/api/projects/status/hqrtav24894dtjcp/branch/main?svg=true)](https://ci.appveyor.com/project/faceit/hangfire-tags/branch/main)
[![NuGet](https://img.shields.io/nuget/v/FaceIT.Hangfire.Tags.svg)](https://www.nuget.org/packages/FaceIT.Hangfire.Tags/)
![MIT License](https://img.shields.io/badge/license-MIT-orange.svg)
[![Donate](https://img.shields.io/badge/Donate-PayPal-green.svg)](https://www.paypal.com/cgi-bin/webscr?cmd=_donations&business=HYKKSJL8B7XE4&currency_code=EUR&source=url)

Inspired by the lack of searching and grouping, Hangfire.Tags provides a way to search and group different jobs. 

![sidemenu](https://raw.githubusercontent.com/face-it/Hangfire.Tags/main/Sidemenu-dark.png)
![dashboard](https://raw.githubusercontent.com/face-it/Hangfire.Tags/main/Dashboard.png)

## Contributers

- **[Yong Liu](https://github.com/yongliu-mdsol)**: Special thanks for all the pull requests you've created. Thank you so much!
- **[Adam Taylor](https://github.com/granicus422)**: MySql support

## Features

- **100% Safe**: no Hangfire-managed data is ever updated, hence there's no risk to corrupt it.
- **Attributes**: supports [Tag("{0}")] syntax for creating a default set of tags when creating a job.
- **Extensions**: has extension methods on PerformContext, but also on string (for instance for adding tags to a jobid).
- **Clean up**: uses Hangfire sets, which are cleaned when jobs are removed.
- **Filtering**: allows filtering of tags based on tags and states, this makes it easy to requeue failed jobs with a certain tag.
- **Searching**: allows you to search for tags
- **Storages**: has an storage for SQL Server, MySql, PostgreSql and initial Redis support
- **Dark and light mode**: supports the new dark and light mode support of Hangfire

## Setup

In .NET Core's Startup.cs:

```c#
public void ConfigureServices(IServiceCollection services)
{
    services.AddHangfire(config =>
    {
        config.UseSqlServerStorage("connectionSting");
        // config.UseTagsWithPostgreSql();
        // config.UseTagsWithMySql();
        // config.UseTagsWithRedis();
        config.UseTagsWithSql();
    });
}
```

Otherwise,

```c#
GlobalConfiguration.Configuration
    .UseSqlServerStorage("connectionSting")
    //.UseTagsWithPostgreSql()
    //.UseTagsWithMySql()
    //.UseTagsWithRedis();
    .UseTagsWithSql();
```

**NOTE**: If you have Dashboard and Server running separately,
you'll need to call `UseTags()`, `UseTagsWithSql()`, `UseTagsWithPostgreSql()`, `UseTagsWithMySql()` or `UseTagsWithRedis()` on both.

### Sql Options
If you have a custom HangFire schema in your database, you'll need to pass your sql options to your storage method. For example:

```csharp
        var tagsOptions = new TagsOptions() { TagsListStyle = TagsListStyle.Dropdown };
        var hangfireSqlOptions = new SqlServerStorageOptions
        {
            SchemaName = "MyCustomHangFireSchema",
        };
        services.AddHangfire(hangfireConfig => hangfireConfig
            .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
            .UseColouredConsoleLogProvider()
            .UseSimpleAssemblyNameTypeSerializer()
            .UseRecommendedSerializerSettings()
            .UseSqlServerStorage("dbConnection", hangfireSqlOptions)
            .UseTagsWithSql(tagsOptions, hangfireSqlOptions)
        );
```

### Additional options

As usual, you may provide additional options for `UseTags()` method.

Here's what you can configure:

- **TagColor**/**DarkTagColor** - default background color for the tags
- **TextColor**/**TextColor** - default text color of the tags
- **Tags interface** - you can specify an autocomplete tags search (Yong Liu)
- **MaxLength** - the maximum length of the tags, automatically set to 100 for SQL Server

**NOTE**: After you initially add Hangfire.Tags (or change the options above) you may need to clear browser cache, as generated CSS/JS can be cached by browser.

## Providers

In order to properly cleanup tags for expired jobs, an extension is required for the default storage providers. Right now, there are three providers: for SQL server, for PostgreSQL and for MySql.

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

The default view for showing the tags is a so called tagcloud. If you prefer an autocomplete dropdown list, you can specify that using the options:

```c#
var options = new TagsOptions()
{
   TagsListStyle = TagsListStyle.Dropdown
};
config.UseTagsWithSql(options);
```

The result will look like this:
![tagsearch](https://raw.githubusercontent.com/face-it/Hangfire.Tags/main/Tagsearch.png)

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
In .NET Core's Startup.cs:

```c#
public void ConfigureServices(IServiceCollection services)
{
    services.AddHangfireTagsRedisStackExchange(); 
    services.AddHangfire((serviceProvider, configuration) =>
    {
        config.UseRedisStorage();
        config.UseTagsWithRedis(serviceProvider);
        
    });
}
```



Adapt the code to your needs, specifying options, storage and so on
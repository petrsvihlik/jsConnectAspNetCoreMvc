# jsConnectAspNetCoreMvc
[![Build status](https://ci.appveyor.com/api/projects/status/2pd8rmd5kaico1au?svg=true)](https://ci.appveyor.com/project/petrsvihlik/jsconnectaspnetcoremvc)
[![Build status](https://img.shields.io/nuget/v/jsConnectAspNetCoreMvc.svg)](https://www.nuget.org/packages/jsConnectAspNetCoreMvc/)



Vanilla Forums' jsConnect for ASP.NET Core MVC

## Getting started

**1.** Install the [NuGet package](https://www.nuget.org/packages/jsConnectAspNetCoreMvc/) and reference `jsConnect` from your web application project.

**2.** Configure the DI Container. Inject `IConfiguration` and `ILogger`.

```csharp
public void ConfigureServices(IServiceCollection services)
{
	services
	.AddMvc() // Or AddControllers, or similar...
	.AddApplicationPart(typeof(VanillaApiClient).GetTypeInfo().Assembly) // Add the controllers from this
	.AddJsonOptions(o =>
	{
		o.JsonSerializerOptions.PropertyNamingPolicy = null;
		o.JsonSerializerOptions.IgnoreNullValues = true;
	});

   services.AddSingleton(Configuration);
   services.AddTransient<HashAlgorithm>(h => SHA512.Create()); // Reflect the hashing algorithm set in Vanilla Forums
}
```

**3.** Add configuration
```json
"Vanilla": {
   "ClientId": "your_client_id",
   "ClientSecret": "your_secret",
   "TimestampValidFor": "int_seconds",
   "AllowWhitespaceInUsername": "bool" 
}
```
**4.** Make sure the `HttpContext.User` has the following claims set according to the [documentation](http://docs.vanillaforums.com/help/sso/jsconnect/seamless/):
```csharp
System.Security.Claims.ClaimTypes.NameIdentifier // required
System.Security.Claims.ClaimTypes.Name // required
System.Security.Claims.ClaimTypes.Email // required
"AvatarUrl" // optional
"Roles" // optional
```

**5.** Point Authentication URL of your Vanilla Forums to http://yourapp/jsconnect/authenticate

**6.** You are done!

**7.** Additionally, you can add the `Vanilla:TimestampValidFor` setting (in seconds). By default, it's 30 minutes.
See all configuration options in the [JsConnectController](https://github.com/petrsvihlik/jsConnectAspNetCoreMvc/blob/master/src/jsConnect/Controllers/JsConnectController.cs#L23).

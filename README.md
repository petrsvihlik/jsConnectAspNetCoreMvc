# jsConnectAspNetCoreMvc
[![Build status](https://ci.appveyor.com/api/projects/status/2pd8rmd5kaico1au?svg=true)](https://ci.appveyor.com/project/petrsvihlik/jsconnectaspnetcoremvc)
[![Build status](https://img.shields.io/nuget/v/jsConnectAspNetCoreMvc.svg)](https://www.nuget.org/packages/jsConnectAspNetCoreMvc/)



Vanilla Forums' jsConnect for ASP.NET Core MVC

## Getting started

**1.** Add the [NuGet package](https://www.nuget.org/packages/jsConnectAspNetCoreMvc/) to the "dependencies" section of your project.json file:
```json
"dependencies": { 
   "jsConnectAspNetCoreMvc": "1.0.*" 
}
```
ASP.NET Core MVC will automatically discover the controller inside the assembly.

**2.** Configure the DI Container. The library expects that you inject a hashing algorithm (keep it in sync with what you use in Vanilla Forums), `IConfiguration` and `ILogger`.

```csharp
public void ConfigureServices(IServiceCollection services)
{
   services.AddMvc().AddJsonOptions(options =>
   {
	   // Setup json serializer
	   options.SerializerSettings.NullValueHandling = NullValueHandling.Ignore;
	   // Duplicate the settings in JsonConvert
	   JsonConvert.DefaultSettings = () => options.SerializerSettings;
   });

   services.AddSingleton(Configuration);
   services.AddTransient<HashAlgorithm>(h => SHA512.Create());
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
```
System.Security.Claims.ClaimTypes.NameIdentifier // required
System.Security.Claims.ClaimTypes.Name // required
System.Security.Claims.ClaimTypes.Email // required
"AvatarUrl" // optional
"Roles" // optional

```

**5.** Point Authentication URL of your Vanilla Forums to http://yourapp/jsconnect/authenticate

**6.** You are done!

**7.** Additionally, you can add the `Vanilla:TimestampValidFor` setting (in seconds). By default, it's 30 minutes.

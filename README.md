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

**2.** Configure the DI Container

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
   "ClientSecret": "your_secret"
}
```
**4.** Point Authentication URL of your Vanilla Forums to http://yourapp/jsconnect/authenticate

**5.** You are done!

**6.** Additionally, you can add the `Vanilla:TimestampValidFor` setting (in seconds). By default, it's 30 minutes.

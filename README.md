# jsConnectAspNetCoreMvc

[![Build & Test](https://github.com/petrsvihlik/jsConnectAspNetCoreMvc/actions/workflows/integrate.yml/badge.svg)](https://github.com/petrsvihlik/jsConnectAspNetCoreMvc/actions/workflows/integrate.yml)
[![codecov](https://codecov.io/gh/petrsvihlik/jsConnectAspNetCoreMvc/branch/master/graph/badge.svg)](https://codecov.io/gh/petrsvihlik/jsConnectAspNetCoreMvc)
[![NuGet link](https://img.shields.io/nuget/v/jsConnectAspNetCoreMvc.svg)](https://www.nuget.org/packages/jsConnectAspNetCoreMvc/)
[![Downloads](https://img.shields.io/nuget/dt/jsConnectAspNetCoreMvc.svg)](https://www.nuget.org/packages/jsConnectAspNetCoreMvc/)

Vanilla Forums' [jsConnect v3](https://success.vanillaforums.com/kb/articles/211-implement-the-jsconnect-v3-protocol) for ASP.NET Core. Implements the modern JWT-based SSO flow that works under third-party cookie blocking.

## What's in the package

The single NuGet `jsConnectAspNetCoreMvc` ships three assemblies:

| Assembly | Use it for |
|---|---|
| `jsConnect.Core` | Framework-agnostic v3 primitives (`JsConnectV3`, options, exceptions). No ASP.NET dependency. |
| `jsConnect.AspNetCore` | `AddJsConnect()` + `MapJsConnect()` extension methods, claims-based user resolver. |
| `jsConnect.Vanilla` | Optional helper for talking to the Vanilla REST API (username lookup, sanitization). |

## Quick start

**1.** Install the package:

```sh
dotnet add package jsConnectAspNetCoreMvc
```

**2.** Register and map the endpoint:

```csharp
using jsConnect.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddJsConnect(opts =>
{
    opts.ClientId = builder.Configuration["Vanilla:ClientId"]!;
    opts.Secret   = builder.Configuration["Vanilla:Secret"]!;
});

// Or bind from configuration directly:
// builder.Services.AddJsConnect(builder.Configuration.GetSection("Vanilla"));

var app = builder.Build();

app.UseAuthentication();
app.UseAuthorization();

app.MapJsConnect("/jsconnect");  // pick any route you like

app.Run();
```

**3.** Configure your secret (≥ 32 bytes for `HS256` — the default):

```json
{
  "Vanilla": {
    "ClientId": "your-client-id",
    "Secret":   "your-shared-secret-32-bytes-or-more",
    "Algorithm": "HS256",
    "ResponseLifetime": "00:10:00"
  }
}
```

**4.** Make sure `HttpContext.User` carries the claims the default resolver expects:

| Claim | Maps to JWT field |
|---|---|
| `ClaimTypes.NameIdentifier` | `id` (required) |
| `ClaimTypes.Name` | `name` (required) |
| `ClaimTypes.Email` | `email` (required) |
| `AvatarUrl` (string) | `photo` (optional) |
| `ClaimTypes.Role` (any count) | `roles` (optional) |

**5.** In Vanilla's dashboard, set the **Authentication URL** to `https://yourapp/jsconnect`.

## Customizing user resolution

If your user info doesn't live in claims, implement `IJsConnectUserResolver`:

```csharp
public class MyUserResolver : IJsConnectUserResolver
{
    public Task<JsConnectUser?> ResolveAsync(HttpContext context, CancellationToken ct)
    {
        return Task.FromResult<JsConnectUser?>(new JsConnectUser
        {
            UniqueId = "...",
            Name     = "...",
            Email    = "...",
            PhotoUrl = "...",
            Roles    = new[] { "admin" },
            CustomFields = new Dictionary<string, object?> { ["department"] = "eng" },
        });
    }
}

builder.Services.AddSingleton<IJsConnectUserResolver, MyUserResolver>();
```

Return `null` to emit a guest response.

## Adjusting the default claims mapping

```csharp
builder.Services.Configure<ClaimsJsConnectUserResolverOptions>(opts =>
{
    opts.UniqueIdClaimType = "sub";
    opts.NameClaimType     = "preferred_username";
    opts.PhotoUrlClaimType = "picture";
});
```

## Using the protocol primitives directly

`JsConnectV3` is framework-agnostic. Use it from a controller, a worker, or anywhere else:

```csharp
var jsc = new JsConnectV3(new JsConnectOptions
{
    ClientId = "...",
    Secret   = "...",
});

string redirectUrl = await jsc.GenerateResponseLocationAsync(requestJwt, user);
```

## Opt-in: Vanilla REST API client

The `jsConnect.Vanilla` namespace provides a small client for the legacy `/api/v1/users` endpoint, plus `UsernameSanitizer` for stripping whitespace and accents. These are *not* part of the jsConnect protocol — Vanilla v3 reconciles users via the `id` field — but you may want them if you're populating new usernames yourself.

```csharp
builder.Services.AddHttpClient<VanillaApiClient>(c =>
    c.BaseAddress = new Uri("https://forums.example.com/"));

// ...
var unique = await client.GetUniqueUsernameAsync(
    UsernameSanitizer.Sanitize("Petr Švihlík"));  // -> "PetrSvihlik" or "PetrSvihlik1"
```

## Migrating from 1.x (v2 protocol)

This is a major release. The v2 JSONP+SHA flow has been removed entirely:

| Before (v2) | After (v3) |
|---|---|
| Reference `jsConnect` controller, `AddApplicationPart`, register `HashAlgorithm` | `services.AddJsConnect(...); app.MapJsConnect("/jsconnect");` |
| Hardcoded `/jsconnect/authenticate` route | Any route you pick |
| User claims read inside the controller | `IJsConnectUserResolver` (claims-based by default) |
| `JsConnectResponseModel`, `VanillaApiClient` in core path | Just `JsConnectUser` + opt-in `jsConnect.Vanilla` helper |
| Newtonsoft.Json | `System.Text.Json` + `Microsoft.IdentityModel.JsonWebTokens` |

In Vanilla's dashboard you must also re-issue your jsConnect client as **v3** and update the endpoint URL.

## Target framework

`.NET 10` (LTS).

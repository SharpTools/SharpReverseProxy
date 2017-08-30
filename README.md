SharpReverseProxy
=================

Powerful Reverse Proxy written as OWIN Middleware. Perfect for ASP .NET, Web API, microservices, and more.

Looking for a way to build an API Gateway based on rules, I found the [Asp.Net Proxy repository](https://github.com/aspnet/Proxy).

The problem is that it proxies all request and I would like to have granular control of the proxy rules. So I wrote SharpReverseProxy. 😃


[![Build status](https://ci.appveyor.com/api/projects/status/b8y5k1vxwybsdj1s?svg=true)](https://ci.appveyor.com/project/Andre/sharpreverseproxy)


## How to Use

Install the [SharpReverseProxy package](https://www.nuget.org/packages/SharpReverseProxy/) via Nuget:

```powershell
Install-Package SharpReverseProxy
```

Open your *Startup.cs* and configure your reverse proxy:

```csharp
public void Configure(IApplicationBuilder app, 
    IHostingEnvironment env, 
    ILoggerFactory loggerFactory) {

    app.UseProxy(new List<ProxyRule> {
        new ProxyRule {
            Matcher = uri => uri.AbsoluteUri.Contains("/api/"),
            Modifier = (req, user) => {
                var match = Regex.Match(req.RequestUri.AbsolutePath, "/api/(.+)service");
                req.RequestUri = new Uri(string.Format("http://{0}.{1}/{2}",
                    match.Groups[1].Value,
                    req.RequestUri.Host,
                    req.RequestUri.AbsolutePath.Replace(match.Value, "/api/")
                ));
            },
            RequiresAuthentication = true
        }
    },
    r => {
        _logger.LogDebug($"Proxy: {r.ProxyStatus} Url: {r.OriginalUri} Time: {r.Elapsed}");
        if (r.ProxyStatus == ProxyStatus.Proxied) {
            _logger.LogDebug($"        New Url: {r.ProxiedUri.AbsoluteUri} Status: {r.HttpStatusCode}");
        }
    });
}
```

### Explanation:

Add a proxy rule. You can create as many as you want and the proxy will use the first matched rule to divert the request.
For every rule, define the matcher, the modifier and optinally a response modifier.

#### Matcher

`Func<Uri, bool> Matcher`: responsible for selecting which request will be handled by this rule. Simply analyse the Uri and return true/false.

#### Modifier

`Action<HttpRequestMessage, ClaimsPrincipal> Modifier`: responsible for modifying the proxied request.

In the code below, we are adding the following rule:

1. Find urls with `/api1`.  Ex: `http://www.noplace.com/api/[servicename]service/`
2. Proxy the request to: `http://[servicename].noplace.com/api/`

```csharp
new ProxyRule {
    Matcher = uri => uri.AbsoluteUri.Contains("/api/"),
    Modifier = uri => {
        var match = Regex.Match(uri.Path, "/api/(.+)service");
        uri.Host = match.Groups[1].Value + "." + uri.Host;
        uri.Path = uri.Path.Replace(match.Value, "/api/");
    },
    RequiresAuthentication = true
}
```

##### Authentication

If you set `RequiresAuthentication = true`, the proxy will only act if the user is authenticated; otherwise, a 401 status code will be sent back and the request ends there. Just make sure to add your authentication middleware before adding the proxy one in the pipeline.

You have total control over proxying requests: have fun! 😃


#### Response Modifier

[Version 1.3](https://www.nuget.org/packages/SharpReverseProxy/1.3.0) adds the ability to modify the response providing a **ResponseModifier**. Thank you so much for [@vsimonia](https://github.com/vsimonian) for this PR :)

This is extremely useful when:

- Proxying a service that has its URL hardcoded, which needs to be replaced with the proxy URL so that links and references function properly.
- Modifying the behaviour of an existing application or service in situations where there is no alternative.

Here's an example of usage:


```csharp
new ProxyRule {
    // ...
    Modifier = (req, user) => {
        req.RequestUri = new Uri(
            $"https://www.example.com{req.RequestUri.PathAndQuery}"
        );
    },
    ResponseModifier = async (msg, ctx) =>
    {
        ctx.Response.Headers.Remove("Strict-Transport-Security");
        ctx.Response.Headers.Remove("Content-Security-Policy");
        if (msg.StatusCode == System.Net.HttpStatusCode.OK)
        {
            switch (msg.Content.Headers.ContentType.MediaType)
            {
                case "text/html":
                case "application/xhtml+xml":
                case "application/javascript":
                case "text/css":
                    var body = Regex.Replace(
                        await msg.Content.ReadAsStringAsync(),
                        @"(http(s)?:)?//(?:www\.)?example.com",
                        string.Format(
                            "{0}://{1}",
                            ctx.Request.Scheme,
                            ctx.Request.Host
                        ),
                        RegexOptions.IgnoreCase
                    );
                    byte[] data = Encoding.UTF8.GetBytes(body);
                    ctx.Response.ContentLength = data.Length;
                    await ctx.Response.Body.WriteAsync(data, 0, data.Length);
                    break;
                default:
                    await msg.Content.CopyToAsync(ctx.Response.Body);
                    break;
            }
        }
        else
        {
            await msg.Content.CopyToAsync(ctx.Response.Body);
        }
    }
}
```

##### Skipping the default operations

This version also adds the `PreProcessResponse` boolean, with a default value of `true`. If set to `false`, and a delegate is specified for `ResponseModifier`, default operations that modify the response sent to the user agent (such as copying headers from the originating server) are skipped and you have to do all the work yourself.


#### Reporter

After every request, a `ProxyResult` is returned so you can log/take actions about what happened.

`Action<ProxyResult> Reporter`: returns request information.

In the code below, we show the request URL, if it was proxied, and the time it took. When proxied, we also log the new URL and the status code.

```csharp
proxyOptions.Reporter = r => {
    logger.LogDebug($"Proxy: {r.ProxyStatus} Url: {r.OriginalUri} Time: {r.Elapsed}");
    if (r.ProxyStatus == ProxyStatus.Proxied) {
        logger.LogDebug($"        New Url: {r.ProxiedUri.AbsoluteUri} Status: {r.HttpStatusCode}");
    }
};
```


And that's it!

Heavily inspired by https://github.com/aspnet/Proxy

## Licence

MIT

SharpReverseProxy
=================

Powerful Reverse Proxy written as OWIN Middleware. Perfect for asp.net, web.api, microservices, etc.

Looking for a way to build an API Gateway based on rules, I found the [Asp.Net Proxy repository](https://github.com/aspnet/Proxy).

The problem is that it proxies all request and I would like to have granular control of the proxy rules. So I wrote SharpReverseProxy :)

## How to Use

Add SharpReverseProxy via Nuget

    Install-Package SharpReverseProxy

Open your *Startup.cs* and configure your reverse proxy:

```csharp
    public void Configure(IApplicationBuilder app, 
                          IHostingEnvironment env, 
                          ILoggerFactory loggerFactory) {
                        
            app.UseProxy(new List<ProxyRule> {
                new ProxyRule {
                     Matcher = uri => uri.AbsoluteUri.Contains("/api/"),
                     Modifier = uri => {
                         var match = Regex.Match(uri.Path, "/api/(.+)service");
                         uri.Host = match.Groups[1].Value + "." + uri.Host;
                         uri.Path = uri.Path.Replace(match.Value, "/api/");
                     },
                    RequiresAuthentication = true
                }
            },
            r => {
                _logger.LogDebug($"Proxy: {r.ProxyStatus} Url: {r.OriginalUri} Time: {r.Elipsed}");
                if (r.ProxyStatus == ProxyStatus.Proxied) {
                    _logger.LogDebug($"        New Url: {r.ProxiedUri.AbsoluteUri} Status: {r.HttpStatusCode}");
                }
            });
	}
```

###Explanation:

Add a proxy rule. You can create as many as you want and the proxy will use the first matched rule to divert the request.
For every rule, define the matcher and the modifier:

#### Matcher
```Func<Uri, bool> Matcher```: responsible for selecting which request will be handled by this rule. Simply analyse the Uri and return true/false.

#### Modifier
```Action<HttpRequestMessage, ClaimsPrincipal> Modifier```: responsible for modifying the proxied request.

In the code below, we are adding the following rule:


1 - Find urls with "/api1".  Ex: http<nolink>://www.noplace.com/api/[service name]service/

2 - Proxy the request to: http<nolink>://[service name].noplace.com/api/

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

If you set RequiresAuthentication = true, the proxy will only act if the user is authenticated, otherwise a 401 status code will be sent back and the request ends there. Just make sure to add your authentication middleware before adding the proxy one in the pipeline.

You have total control about how to proxy a request, have fun :)

#### Reporter
After every request, a ProxyResult is returned so you can log/take actions about what happened.

```Action<ProxyResult> Reporter```: returns request information.

In the code below, we show the request URL, if it was proxied and the time it took. When proxied, we also log the new URL and the status code.
```csharp
    proxyOptions.Reporter = r => {
		logger.LogDebug($"Proxy: {r.ProxyStatus} Url: {r.OriginalUri} Time: {r.Elipsed}");
                if (r.ProxyStatus == ProxyStatus.Proxied) {
                    logger.LogDebug($"        New Url: {r.ProxiedUri.AbsoluteUri} Status: {r.HttpStatusCode}");
                }
    };
```

And that's it!

Heavily inspired on https://github.com/aspnet/Proxy

SharpReverseProxy
=================

Powerful Reverse Proxy written as OWIN Middleware. Perfect for asp.net, web.api, microservices, etc.

Looking for a way to build an API Gateway based on rules, I found the [Asp.Net Proxy repository](https://github.com/aspnet/Proxy).

The problem is that it proxies all request and I would like to have granular control of the proxy rules. So I wrote SharpReverseProxy :)

## How to Use

Add SharpReverseProxy via Nuget

    Install-Package SharpReverseProxy

Open your *Startup.cs* and configure your reverse proxy:

    public void Configure(IApplicationBuilder app, 
						  IHostingEnvironment env, 
						  ILoggerFactory loggerFactory) {
            
            loggerFactory.AddConsole(Configuration.GetSection("Logging"));
            loggerFactory.AddDebug(LogLevel.Debug);
            var logger = loggerFactory.CreateLogger("Middleware");

            var proxyOptions = new ProxyOptions();
            proxyOptions.AddProxyRule(new ProxyRule {
                Matcher = uri => uri.AbsoluteUri.Contains("/api/"),
                Modifier = uri => {
                    var match = Regex.Match(uri.Path, "/api/(.+)service");
                    uri.Host = match.Groups[1].Value + "." + uri.Host;
                    uri.Path = uri.Path.Replace(match.Value, "/api/");
                }
            });
            proxyOptions.Reporter = r => {
                logger.LogDebug($"Proxy: {r.Proxied} Url: {r.OriginalUri} Time: {r.Elipsed}");
                if (r.Proxied) {
                    logger.LogDebug($"        New Url: {r.ProxiedUri.AbsoluteUri} Status: {r.StatusCode}");
                }
            };
            app.UseProxy(proxyOptions);

            app.UseMvc();
	}


###Explanation:

Create the options object that will hold all our proxy rules:

    var proxyOptions = new ProxyOptions();

Add a proxy rule. You can create as many as you want and the proxy will use the first matched rule to divert the request.

For every rule, define the matcher and the modifier:
```Func<Uri, bool> Matcher```: responsible for selecting which request will be handled by this rule. Simply analyse the Uri and return true/false.

```Action<UriBuilder> Modifier```: responsible for modifying the final Uri.

In the code below, we are adding the following rule:


1 - Find urls with "/api/".  Ex: http<nolink>://www.noplace.com/api/[service name]service/

2 - Proxy the request to: http<nolink>://[service name].noplace.com/api/


    proxyOptions.AddProxyRule(new ProxyRule {
         Matcher = uri => uri.AbsoluteUri.Contains("/api/"),
         Modifier = uri => {
             var match = Regex.Match(uri.Path, "/api/(.+)service");
             uri.Host = match.Groups[1].Value + "." + uri.Host;
             uri.Path = uri.Path.Replace(match.Value, "/api/");
         }
	});

You have total control about how to proxy a request, have fun :)

After every request, a ProxyResult is returned so you can log/take actions about what happened.

```Action<ProxyResult> Reporter```: returns request information.

In the code below, we show the request URL, if it was proxied and the time it took. When proxied, we also log the new URL and the status code.

    proxyOptions.Reporter = r => {
		logger.LogDebug($"Proxy: {r.Proxied} Url: {r.OriginalUri} Time: {r.Elipsed}");
        if (r.Proxied) {
	        logger.LogDebug($"-> New Url: {r.ProxiedUri.AbsoluteUri} Status: {r.StatusCode}");
		}
    };

Finally, add the proxy to our application pipeline:

    app.UseProxy(proxyOptions);

And that's it!

Heavily inspired on https://github.com/aspnet/Proxy

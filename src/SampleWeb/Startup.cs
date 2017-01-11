using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SharpReverseProxy;

namespace SampleWeb {
    public class Startup {
        // This method gets called by the runtime. Use this method to add services to the container.
        // For more information on how to configure your application, visit http://go.microsoft.com/fwlink/?LinkID=398940
        public void ConfigureServices(IServiceCollection services) {
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env, ILoggerFactory loggerFactory) {
            loggerFactory.AddConsole();
            loggerFactory.AddDebug(LogLevel.Debug);
            var logger = loggerFactory.CreateLogger("Middleware");

            if (env.IsDevelopment()) {
                app.UseDeveloperExceptionPage();
            }

            var proxyOptions = new ProxyOptions();
            proxyOptions.AddProxyRule(new ProxyRule {
                Matcher = uri => uri.AbsoluteUri.Contains("/api1"),
                Modifier = uri => {
                    uri.Port = 5001;
                    uri.Path = "/api/values";
                }
            });
            proxyOptions.AddProxyRule(new ProxyRule {
                Matcher = uri => uri.AbsoluteUri.Contains("/api2"),
                Modifier = uri => {
                    uri.Port = 5002;
                    uri.Path = "/api/values";
                }
            });
            proxyOptions.Reporter = r => {
                logger.LogDebug($"Proxy: {r.Proxied} Url: {r.OriginalUri} Time: {r.Elipsed}");
                if (r.Proxied) {
                    logger.LogDebug($"        New Url: {r.ProxiedUri.AbsoluteUri} Status: {r.StatusCode}" );
                }
            };

            app.UseProxy(proxyOptions);

            app.Run(async (context) => {
                await context.Response.WriteAsync("Hello World!");
            });
        }
    }
}

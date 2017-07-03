using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using SampleWeb.Authentication;
using SharpReverseProxy;

namespace SampleWeb {
    public class Startup {
        // This method gets called by the runtime. Use this method to add services to the container.
        // For more information on how to configure your application, visit http://go.microsoft.com/fwlink/?LinkID=398940
        public void ConfigureServices(IServiceCollection services) {
            services.AddAuthentication();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env, ILoggerFactory loggerFactory) {
            loggerFactory.AddConsole();
            loggerFactory.AddDebug(LogLevel.Debug);
            var logger = loggerFactory.CreateLogger("Middleware");

            if (env.IsDevelopment()) {
                app.UseDeveloperExceptionPage();
            }

            ConfigureAuthentication(app);

            app.UseProxy(new List<ProxyRule> {
                new ProxyRule {
                    Matcher = uri => uri.AbsoluteUri.Contains("/api1"),
                    Modifier = (msg ,user) => {
                        var uri = new UriBuilder(msg.RequestUri) {
                            Port = 5001,
                            Path = "/api/values"
                        };
                        msg.RequestUri = uri.Uri;
                    }
                },
                new ProxyRule {
                    Matcher = uri => uri.AbsoluteUri.Contains("/api2"),
                    Modifier = (msg ,user) => {
                        var uri = new UriBuilder(msg.RequestUri) {
                            Port = 5002,
                            Path = "/api/values" 
                        };
                        msg.RequestUri = uri.Uri;
                    },
                    RequiresAuthentication = true
                },
                new ProxyRule {
                    Matcher = uri => uri.AbsoluteUri.Contains("/authenticate"),
                    Modifier = (msg ,user) => {
                        var uri = new UriBuilder(msg.RequestUri) {
                            Port = 5000
                        };
                        msg.RequestUri = uri.Uri;
                    },
                }
            },
            r => {
                logger.LogDebug($"Proxy: {r.ProxyStatus} Url: {r.OriginalUri} Time: {r.Elapsed}");
                if (r.ProxyStatus == ProxyStatus.Proxied) {
                    logger.LogDebug($"        New Url: {r.ProxiedUri.AbsoluteUri} Status: {r.HttpStatusCode}");
                }
            });

            app.Run(async (context) => {
                await context.Response.WriteAsync("Hello World!");
            });
        }

        private static void ConfigureAuthentication(IApplicationBuilder app) {
            var secretKey = "foofoofoofoofoobar";
            var signingKey = new SymmetricSecurityKey(Encoding.ASCII.GetBytes(secretKey));

            var tokenValidationParameters = new TokenValidationParameters {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = signingKey,
                ValidateIssuer = true,
                ValidIssuer = "someIssuer",
                ValidateAudience = true,
                ValidAudience = "Browser",
                ValidateLifetime = true,
                ClockSkew = TimeSpan.Zero
            };

            app.UseJwtBearerAuthentication(new JwtBearerOptions {
                AutomaticAuthenticate = true,
                AutomaticChallenge = true,
                TokenValidationParameters = tokenValidationParameters
            });

            app.UseCookieAuthentication(new CookieAuthenticationOptions {
                AutomaticAuthenticate = true,
                AutomaticChallenge = true,
                AuthenticationScheme = "Cookie",
                CookieName = "access_token",
                TicketDataFormat = new CustomJwtDataFormat(
                    SecurityAlgorithms.HmacSha256,
                    tokenValidationParameters)
            });
        }
    }
}

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using SampleWeb.Authentication;
using SharpReverseProxy;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace SampleWeb {
    public class Startup {
        public void ConfigureServices(IServiceCollection services) {
            services.AddAuthentication();
        }

        public void Configure(IApplicationBuilder app, IHostingEnvironment env, ILoggerFactory loggerFactory) {
            loggerFactory.AddConsole();
            loggerFactory.AddDebug(LogLevel.Debug);
            var logger = loggerFactory.CreateLogger("Middleware");

            if (env.IsDevelopment()) {
                app.UseDeveloperExceptionPage();
            }

            ConfigureAuthentication(app);

            var proxyOptions = new ProxyOptions {
                ProxyRules = new List<ProxyRule> {
                    new ProxyRule {
                        Matcher = r => MatchBy.Url(r, url => url.Contains("/api1")),
                        RequestModifier = async (msg ,user) => {
                            var uri = new UriBuilder(msg.RequestUri) {
                                Port = 5001,
                                Path = "/api/values"
                            };
                            msg.RequestUri = uri.Uri;
                        }
                    },
                    new ProxyRule {
                        Matcher = r => MatchBy.Url(r, url => url.Contains("/api2")),
                        RequestModifier = async (msg ,user) => {
                            var uri = new UriBuilder(msg.RequestUri) {
                                Port = 5002,
                                Path = "/api/values"
                            };
                            msg.RequestUri = uri.Uri;
                        },
                        RequiresAuthentication = true
                    },
                    new ProxyRule {
                        Matcher = r => MatchBy.Url(r, url => url.Contains("/api3")),
                        CopyResponseBody = false,
                        ResponseModifier = (resp, ctx) => ModifyResponse.ReplaceDomainWhenText(resp, ctx, "example.com")
                    },
                    new ProxyRule {
                        Matcher = r => MatchBy.Header(r, headers => headers.ContainsKey("SomeHeader")),
                        RequestModifier = async (msg ,user) => {
                            var uri = new UriBuilder(msg.RequestUri) {
                                Port = 5002,
                                Path = "/api/values"
                            };
                            msg.RequestUri = uri.Uri;
                        },
                        RequiresAuthentication = true
                    },
                    new ProxyRule {
                        Matcher= r => MatchBy.Url(r, url => url.Contains("/authenticate")),
                        RequestModifier = async (msg ,user) => {
                            var uri = new UriBuilder(msg.RequestUri) {
                                Port = 5000
                            };
                            msg.RequestUri = uri.Uri;
                        }
                    }
                },
                Reporter = async r => {
                    logger.LogDebug($"Proxy: {r.ProxyStatus} Url: {r.OriginalUri} Time: {r.Elapsed}");
                    if (r.ProxyStatus == ProxyStatus.Proxied) {
                        logger.LogDebug($"        New Url: {r.ProxiedUri.AbsoluteUri} Status: {r.HttpStatusCode}");
                    }
                }
            };

            app.UseProxy(proxyOptions);

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

using System;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;

namespace SharpReverseProxy {
    public class ProxyMiddleware {
        private readonly RequestDelegate _next;
        private readonly HttpClient _httpClient;
        private readonly ProxyOptions _options;

        public ProxyMiddleware(RequestDelegate next, IOptions<ProxyOptions> options) {
            _next = next;
            _options = options.Value;
            _httpClient = new HttpClient(_options.BackChannelMessageHandler ?? new HttpClientHandler {
                AllowAutoRedirect = _options.FollowRedirects
            });
        }

        public async Task Invoke(HttpContext context) {
            var uri = GeRequestUri(context);
            var resultBuilder = new ProxyResultBuilder(uri);

            var matchedRule = _options.ProxyRules.FirstOrDefault(r => r.Matcher.Invoke(uri));
            if (matchedRule == null) {
                await _next(context);
                _options.Reporter.Invoke(resultBuilder.NotProxied(context.Response.StatusCode));
                return;
            }

            if (matchedRule.RequiresAuthentication && !UserIsAuthenticated(context)) {
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                _options.Reporter.Invoke(resultBuilder.NotAuthenticated());
                return;
            }

            var proxyRequest = new HttpRequestMessage(new HttpMethod(context.Request.Method), uri);
            SetProxyRequestBody(proxyRequest, context);
            SetProxyRequestHeaders(proxyRequest, context);

            matchedRule.Modifier.Invoke(proxyRequest, context);
            proxyRequest.Headers.Host = proxyRequest.RequestUri.Host;

            try {
                await ProxyTheRequest(context, proxyRequest, matchedRule);
            }
            catch (HttpRequestException) {
                context.Response.StatusCode = StatusCodes.Status503ServiceUnavailable;
            }
            _options.Reporter.Invoke(resultBuilder.Proxied(proxyRequest.RequestUri, context.Response.StatusCode));
        }

        private async Task ProxyTheRequest(HttpContext context, HttpRequestMessage proxyRequest, ProxyRule proxyRule) {
            using (var responseMessage = await _httpClient.SendAsync(proxyRequest,
                                                                     HttpCompletionOption.ResponseHeadersRead,
                                                                     context.RequestAborted)) {

                if(proxyRule.PreProcessResponse || proxyRule.ResponseModifier == null) { 
                    context.Response.StatusCode = (int)responseMessage.StatusCode;
                    context.Response.ContentType = responseMessage.Content?.Headers.ContentType?.MediaType;
                    foreach (var header in responseMessage.Headers) {
                        context.Response.Headers[header.Key] = header.Value.ToArray();
                    }
                    // SendAsync removes chunking from the response. 
                    // This removes the header so it doesn't expect a chunked response.
                    context.Response.Headers.Remove("transfer-encoding");

                    if (responseMessage.Content != null) {
                        foreach (var contentHeader in responseMessage.Content.Headers) {
                            context.Response.Headers[contentHeader.Key] = contentHeader.Value.ToArray();
                        }
                        await responseMessage.Content.CopyToAsync(context.Response.Body);
                    }
                }
                
                if (proxyRule.ResponseModifier != null) {
                    await proxyRule.ResponseModifier.Invoke(responseMessage, context);
                }
            }
        }

        private static Uri GeRequestUri(HttpContext context) {
            var request = context.Request;
            var uriString = $"{request.Scheme}://{request.Host}{request.PathBase}{request.Path}{request.QueryString}";
            return new Uri(uriString);
        }

        private static void SetProxyRequestBody(HttpRequestMessage requestMessage, HttpContext context) {
            var requestMethod = context.Request.Method;
            if (HttpMethods.IsGet(requestMethod) ||
                HttpMethods.IsHead(requestMethod) ||
                HttpMethods.IsDelete(requestMethod) ||
                HttpMethods.IsTrace(requestMethod)) {
                return;
            }
            requestMessage.Content = new StreamContent(context.Request.Body);
        }

        private void SetProxyRequestHeaders(HttpRequestMessage requestMessage, HttpContext context) {
            foreach (var header in context.Request.Headers) {
                if (!requestMessage.Headers.TryAddWithoutValidation(header.Key, header.Value.ToArray())) {
                    requestMessage.Content?.Headers.TryAddWithoutValidation(header.Key, header.Value.ToArray());
                }
            }
        }

        private bool UserIsAuthenticated(HttpContext context) {
            return context.User.Identities.FirstOrDefault()?.IsAuthenticated ?? false;
        }
    }

}

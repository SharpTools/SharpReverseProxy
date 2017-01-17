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
            _httpClient = new HttpClient(_options.BackChannelMessageHandler ?? new HttpClientHandler());
        }

        public async Task Invoke(HttpContext context) {
            var request = context.Request;
            var uriString = $"{request.Scheme}://{request.Host}{request.PathBase}{request.Path}{request.QueryString}";
            var uri = new Uri(uriString);

            var resultBuilder = new ProxyResultBuilder(uri);

            var matchedRule = _options.ProxyRules.FirstOrDefault(r => r.Matcher.Invoke(uri));
            if (matchedRule == null) {
                _options.Reporter.Invoke(resultBuilder.NotProxied());
                await _next(context);
                return;
            }

            var uriBuilder = new UriBuilder(uri);
            matchedRule.Modifier.Invoke(uriBuilder);
            var newUri = uriBuilder.Uri;

            if (matchedRule.RequiresAuthentication && !UserIsAuthenticated(context)) {
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                _options.Reporter.Invoke(resultBuilder.Proxied(newUri, context.Response.StatusCode));
                return;
            }

            var proxyRequest = new HttpRequestMessage();
            SetProxyRequestBody(proxyRequest, context);
            SetProxyRequestHeaders(proxyRequest, context, newUri);

            proxyRequest.RequestUri = newUri;
            proxyRequest.Method = new HttpMethod(request.Method);
            try {
                using (var responseMessage = await _httpClient.SendAsync(proxyRequest,
                                                                     HttpCompletionOption.ResponseHeadersRead,
                                                                     context.RequestAborted)) {
                    context.Response.StatusCode = (int)responseMessage.StatusCode;
                    foreach (var header in responseMessage.Headers) {
                        context.Response.Headers[header.Key] = header.Value.ToArray();
                    }
                    foreach (var header in responseMessage.Content.Headers) {
                        context.Response.Headers[header.Key] = header.Value.ToArray();
                    }
                    // SendAsync removes chunking from the response. 
                    // This removes the header so it doesn't expect a chunked response.
                    context.Response.Headers.Remove("transfer-encoding");
                    await responseMessage.Content.CopyToAsync(context.Response.Body);
                }
            }
            catch (HttpRequestException) {
                context.Response.StatusCode = StatusCodes.Status503ServiceUnavailable;
            }

            _options.Reporter.Invoke(resultBuilder.Proxied(newUri, context.Response.StatusCode));
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

        private void SetProxyRequestHeaders(HttpRequestMessage requestMessage, HttpContext context, Uri newUri) {
            foreach (var header in context.Request.Headers) {
                if (!requestMessage.Headers.TryAddWithoutValidation(header.Key, header.Value.ToArray())) {
                    requestMessage.Content?.Headers.TryAddWithoutValidation(header.Key, header.Value.ToArray());
                }
            }
            requestMessage.Headers.Host = newUri.Host;
        }

        private bool UserIsAuthenticated(HttpContext context) {
            return context.User.Identities.FirstOrDefault()?.IsAuthenticated ?? false;
        }
    }
}

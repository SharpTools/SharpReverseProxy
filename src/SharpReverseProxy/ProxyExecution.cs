using System;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace SharpReverseProxy {
    public class ProxyExecution {

        private RequestDelegate _next;
        private HttpContext _context;
        private ProxyOptions _options;

        private ProxyRule _matchedRule;
        private ProxyResultBuilder _proxyResultBuilder;
        private HttpRequestMessage _request;
        private HttpResponseMessage _response;

        public async Task<bool> Invoke(RequestDelegate next, 
                                       HttpContext context, 
                                       ProxyOptions options) {
            _next = next;
            _context = context;
            _options = options;
            _proxyResultBuilder = new ProxyResultBuilder(_context.Request.GetUri());

            return await ProxyRuleIsMatched() &&
                   await PassesAuthentication() &&
                   await CreateInternalRequest() &&
                   await ProxyTheRequest();
        }

        private async Task<bool> ProxyRuleIsMatched() {
            foreach(var proxyRule in _options.ProxyRules) {
                if(await proxyRule.Matcher(_context.Request)) {
                    _matchedRule = proxyRule;
                }
            }
            if (_matchedRule != null) {
                return true;
            }
            await _options.Reporter.Invoke(_proxyResultBuilder.NotProxied(_context.Response.StatusCode));
            await _next(_context);
            return false;
        }

        private async Task<bool> PassesAuthentication() {
            if (_matchedRule.RequiresAuthentication && UserIsNotAuthenticated(_context)) {
                _context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                await _options.Reporter.Invoke(_proxyResultBuilder.NotAuthenticated());
                return false;
            }
            return true;
        }

        private bool UserIsNotAuthenticated(HttpContext context) {
            return !(context.User.Identities.FirstOrDefault()?.IsAuthenticated ?? false);
        }

        public async Task<bool> CreateInternalRequest() {
            _request = new HttpRequestMessage(new HttpMethod(_context.Request.Method),
                                                             _context.Request.GetUri());
            CopyRequestHeaders();
            AddForwardedHeader();
            CopyRequestBody();
            await ApplyModificationsToRequest();
            SetRequestHost();
            return true;
        }

        protected virtual void CopyRequestHeaders() {
            if (!_matchedRule.CopyRequestHeaders) {
                return;
            }
            foreach (var header in _context.Request.Headers) {
                if (!_request.Headers.TryAddWithoutValidation(header.Key, header.Value.ToArray())) {
                    _request.Content?.Headers.TryAddWithoutValidation(header.Key, header.Value.ToArray());
                }
            }
        }

        protected virtual void AddForwardedHeader() {
            if (!_matchedRule.AddForwardedHeader) {
                return;
            }
            _request.Headers.TryAddWithoutValidation("Forwarded", $"for={_context.Connection.RemoteIpAddress}");
            _request.Headers.TryAddWithoutValidation("Forwarded", $"host={_request.Headers.Host}");
            _request.Headers.TryAddWithoutValidation("Forwarded", $"proto={(_context.Request.IsHttps ? "https" : "http")}");
        }

        protected virtual void CopyRequestBody() {
            if (!_matchedRule.CopyRequestBody) {
                return;
            }
            var requestMethod = _context.Request.Method;
            if (HttpMethods.IsGet(requestMethod) ||
                HttpMethods.IsHead(requestMethod) ||
                HttpMethods.IsDelete(requestMethod) ||
                HttpMethods.IsTrace(requestMethod)) {
                return;
            }
            _request.Content = new StreamContent(_context.Request.Body);
        }

        private async Task ApplyModificationsToRequest() {
            if(_matchedRule.RequestModifier == null) {
                return;
            }
            await _matchedRule.RequestModifier(_request, _context.User);
        }

        private void SetRequestHost() {
            _request.Headers.Host = !_request.RequestUri.IsDefaultPort
                ? $"{_request.RequestUri.Host}:{_request.RequestUri.Port}"
                : _request.RequestUri.Host;
        }

        public async Task<bool> ProxyTheRequest() {
            try {
                var httpClient = GetHttpClient();
                using (_response = await httpClient.SendAsync(_request,
                                                              HttpCompletionOption.ResponseHeadersRead,
                                                              _context.RequestAborted)) {
                    CopyResponseHeaders();
                    RemoveTransferEncodingHeader();
                    await CopyResponseBody();
                    await ApplyModificationsToResponse();
                    await _options.Reporter.Invoke(_proxyResultBuilder.Proxied(_request.RequestUri, 
                                                                               _context.Response.StatusCode));
                }
                return true;
            }
            catch (OperationCanceledException oce) {
                await _options.Reporter(_proxyResultBuilder.OperationCancelled(oce));
                return false;
            }
            catch (HttpRequestException hre) {
                _context.Response.StatusCode = StatusCodes.Status502BadGateway;
                await _options.Reporter(_proxyResultBuilder.DownstreamServerError(hre));
                return false;
            }
            catch (Exception ex) {
                _context.Response.StatusCode = StatusCodes.Status500InternalServerError;
                await _options.Reporter(_proxyResultBuilder.ProxyError(ex));
                return false;
            }
        }
        
        private HttpClient GetHttpClient() {
            return _matchedRule.RuleHttpClient ?? _options.DefaultHttpClient;
        }

        private void CopyResponseHeaders() {
            if (!_matchedRule.CopyResponseHeaders) {
                return;
            }
            _context.Response.StatusCode = (int)_response.StatusCode;
            _context.Response.ContentType = _response.Content?.Headers.ContentType?.MediaType;
            foreach (var header in _response.Headers) {
                _context.Response.Headers[header.Key] = header.Value.ToArray();
            }
        }

        private void RemoveTransferEncodingHeader() {
            _context.Response.Headers.Remove("transfer-encoding");
        }

        private async Task CopyResponseBody() {
            if (!_matchedRule.CopyResponseBody || _response.Content == null) {
                return;
            }
            foreach (var contentHeader in _response.Content.Headers) {
                _context.Response.Headers[contentHeader.Key] = contentHeader.Value.ToArray();
            }
            await _response.Content.CopyToAsync(_context.Response.Body);
        }

        private async Task ApplyModificationsToResponse() {
            if(_matchedRule.ResponseModifier == null) {
                return;
            }
            await _matchedRule.ResponseModifier(_response, _context);
        }
    }
}

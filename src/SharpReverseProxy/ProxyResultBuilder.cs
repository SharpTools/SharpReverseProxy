using System;
using Microsoft.AspNetCore.Http;

namespace SharpReverseProxy {
    public class ProxyResultBuilder {
        private readonly ProxyResult _result;
        private readonly DateTime _start;
        private readonly HttpContext _httpContext;

        public ProxyResultBuilder(HttpContext httpContext) {
            _result = new ProxyResult {
                OriginalUri = httpContext.Request.GetUri()
            };
            _start = DateTime.Now;
            _httpContext = httpContext;
        }

        public ProxyResult Proxied(Uri proxiedUri, int statusCode) {
            _result.ProxiedUri = proxiedUri;
            _result.HttpStatusCode = statusCode;
            Finish(ProxyStatus.Proxied);
            return _result;
        }

        public ProxyResult NotProxied(int statusCode) {
            _result.HttpStatusCode = statusCode;
            Finish(ProxyStatus.NotProxied);
            return _result;
        }

        public ProxyResult OperationCancelled(OperationCanceledException exception) {
            _result.Exception = exception;
            Finish(ProxyStatus.Cancelled);
            return _result;
        }
        
        public ProxyResult DownstreamServerError(Exception exception) {
            _result.Exception = exception;
            Finish(ProxyStatus.DownstreamServerError);
            return _result;
        }

        public ProxyResult ProxyError(Exception exception) {
            _result.Exception = exception;
            Finish(ProxyStatus.ProxyError);
            return _result;
        }

        public ProxyResult NotAuthenticated() {
            _result.HttpStatusCode = StatusCodes.Status401Unauthorized;
            Finish(ProxyStatus.NotAuthenticated);
            return _result;
        }

        private void Finish(ProxyStatus proxyStatus) {
            _result.ProxyStatus = proxyStatus;
            _result.Elapsed = DateTime.Now - _start;
            _result.HttpContext = _httpContext;
        }
    }
}
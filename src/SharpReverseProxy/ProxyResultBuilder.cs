using System;
using Microsoft.AspNetCore.Http;

namespace SharpReverseProxy {
    public class ProxyResultBuilder {
        private ProxyResult _result;
        private DateTime _start;
        public ProxyResultBuilder(Uri originalUri) {
            _result = new ProxyResult {
                OriginalUri = originalUri
            };
            _start = DateTime.Now;
        }

        public ProxyResult Proxied(Uri proxiedUri, int statusCode) {
            Finish(ProxyStatus.Proxied);
            _result.ProxiedUri = proxiedUri;
            _result.HttpStatusCode = statusCode;
            return _result;
        }

        public ProxyResult NotProxied(int statusCode) {
            Finish(ProxyStatus.NotProxied);
            _result.HttpStatusCode = statusCode;
            return _result;
        }

        public ProxyResult NotAuthenticated() {
            Finish(ProxyStatus.NotAuthenticated);
            _result.HttpStatusCode = StatusCodes.Status401Unauthorized;
            return _result;
        }

        private ProxyResult Finish(ProxyStatus proxyStatus) {
            _result.ProxyStatus = proxyStatus;
            _result.Elapsed = DateTime.Now - _start;
            return _result;
        }
    }
}
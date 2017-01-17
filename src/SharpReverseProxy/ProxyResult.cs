using System;

namespace SharpReverseProxy {
    public class ProxyResult {
        public bool Proxied { get; set; }
        public int StatusCode { get; set; }
        public Uri OriginalUri { get; set; }
        public Uri ProxiedUri { get; set; }
        public TimeSpan Elipsed { get; set; }
    }

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
            _result.Proxied = true;
            _result.ProxiedUri = proxiedUri;
            _result.StatusCode = statusCode;
            _result.Elipsed = DateTime.Now - _start;
            return _result;
        }

        public ProxyResult NotProxied() {
            _result.Proxied = false;
            _result.Elipsed = DateTime.Now - _start;
            return _result;
        }
    }
}
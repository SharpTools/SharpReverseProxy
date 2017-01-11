using System;

namespace SharpReverseProxy {
    public class ProxyResult {
        public bool Proxied { get; set; }
        public int StatusCode { get; set; }
        public Uri OriginalUri { get; set; }
        public Uri ProxiedUri { get; set; }
        public TimeSpan Elipsed { get; set; }
    }
}
using System;

namespace SharpReverseProxy {
    public class ProxyRule {
        public Func<Uri, bool> Matcher { get; set; } = uri => false;
        public Action<UriBuilder> Modifier { get; set; } = uri => { };
        public bool RequiresAuthentication { get; set; }
    }
}
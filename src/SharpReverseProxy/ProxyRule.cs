using System;
using System.Net.Http;
using System.Security.Claims;

namespace SharpReverseProxy {
    public class ProxyRule {
        public Func<Uri, bool> Matcher { get; set; } = uri => false;
        public Action<HttpRequestMessage, ClaimsPrincipal> Modifier { get; set; } = (msg, user) => { };
        public bool RequiresAuthentication { get; set; }
    }
}
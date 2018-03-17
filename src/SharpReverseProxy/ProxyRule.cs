using Microsoft.AspNetCore.Http;
using System;
using System.Net.Http;
using System.Security.Claims;
using System.Threading.Tasks;

namespace SharpReverseProxy {
    public class ProxyRule {
        public Func<Uri, bool> Matcher { get; set; } = uri => false;
        public Action<HttpRequestMessage, HttpContext> Modifier { get; set; } = (msg, ctx) => { };
        public Func<HttpResponseMessage, HttpContext, Task> ResponseModifier { get; set; } = null;
        public bool PreProcessResponse { get; set; } = true;
        public bool RequiresAuthentication { get; set; }
    }
}

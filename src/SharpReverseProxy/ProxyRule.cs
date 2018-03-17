using Microsoft.AspNetCore.Http;
using System;
using System.Net.Http;
using System.Security.Claims;
using System.Threading.Tasks;

namespace SharpReverseProxy {
    public class ProxyRule {
        public Func<Uri, bool> Matcher { get; set; } = uri => false;
        public Action<HttpRequestMessage, ClaimsPrincipal> Modifier { get; set; } = (msg, user) => { };
        public Func<HttpResponseMessage, HttpContext, Task> ResponseModifier { get; set; } = null;
        public bool PreProcessResponse { get; set; } = true;
        public bool RequiresAuthentication { get; set; }
        public Action<HttpContext, HttpClient> RequestModifier { get; set; } = (ctx, httpClient) => { };
    }
}

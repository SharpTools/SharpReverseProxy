using Microsoft.Extensions.DependencyInjection;
using System;
using System.Net.Http;
using System.Threading.Tasks;

namespace SharpReverseProxy {

    public class ProxyRule {
        public bool RequiresAuthentication { get; set; }
        public bool AddForwardedHeader { get; set; } = true;
        public HttpClient RuleHttpClient { get; set; }
        public Func<MatcherContext, Task<bool>> Matcher { get; set; } = request => Task.FromResult(false);
        public bool CopyRequestHeaders { get; set; } = true;
        public bool CopyRequestBody { get; set; } = true;
        public Func<RequestModifierContext, Task> RequestModifier { get; set; }

        public bool CopyResponseHeaders { get; set; } = true;
        public bool CopyResponseBody { get; set; } = true;
        public Func<ResponseModifierContext, Task> ResponseModifier { get; set; }
    }
}
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;

namespace SharpReverseProxy {
    public class ProxyMiddleware {
        private readonly RequestDelegate _next;
        private readonly ProxyOptions _options;

        public ProxyMiddleware(RequestDelegate next, IOptions<ProxyOptions> options) {
            _next = next;
            _options = options.Value;
        }

        public async Task Invoke(HttpContext context) {
            await new ProxyExecution().Invoke(_next, context, _options);
        }
    }
}

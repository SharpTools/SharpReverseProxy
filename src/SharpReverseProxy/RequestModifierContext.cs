using System;
using System.Net.Http;
using System.Security.Claims;

namespace SharpReverseProxy {
    public class RequestModifierContext {
        public HttpRequestMessage HttpRequestMessage { get; set; }
        public ClaimsPrincipal LoggerUser { get; set; }
        public IServiceProvider ServiceProvider { get; set; }
    }
}
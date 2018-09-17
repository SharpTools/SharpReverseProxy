using Microsoft.AspNetCore.Http;
using System;
using System.Security.Claims;

namespace SharpReverseProxy {
    public class RequestBlockerContext {
        public HttpRequest HttpRequest { get; set; }
        public ClaimsPrincipal LoggerUser { get; set; }
        public IServiceProvider ServiceProvider { get; set; }
    }
}
using Microsoft.AspNetCore.Http;
using System;

namespace SharpReverseProxy {
    public class MatcherContext {
        public string Url => HttpRequest.GetUrl();
        public HttpRequest HttpRequest { get; set; }
        public IServiceProvider ServiceProvider { get; set; }
    }
}
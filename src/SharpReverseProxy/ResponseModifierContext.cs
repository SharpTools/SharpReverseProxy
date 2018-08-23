using Microsoft.AspNetCore.Http;
using System;
using System.Net.Http;

namespace SharpReverseProxy {
    public class ResponseModifierContext {
        public HttpResponseMessage HttpResponseMessage { get; set; }
        public HttpContext HttpContext { get; set; }
        public IServiceProvider ServiceProvider { get; set; }
    }
}
using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace SharpReverseProxy.Tests.HttpContextFakes {
    public class HttpResponseFake : HttpResponse {
        private HttpContext _httpContext;

        public void SetHttpContext(HttpContext context) {
            _httpContext = context;
        }
        public override HttpContext HttpContext => _httpContext;

        public override void OnStarting(Func<object, Task> callback, object state) {
            
        }

        public override void OnCompleted(Func<object, Task> callback, object state) {
            
        }

        public override void Redirect(string location, bool permanent) {
            
        }

        public override int StatusCode { get; set; }
        public override IHeaderDictionary Headers { get; }
        public override Stream Body { get; set; } = new MemoryStream();
        public override long? ContentLength { get; set; }
        public override string ContentType { get; set; }
        public override IResponseCookies Cookies { get; }
        public override bool HasStarted { get; }
    }
}
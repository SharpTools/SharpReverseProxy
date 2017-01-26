using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.Http.Internal;
using Microsoft.Extensions.Primitives;

namespace SharpReverseProxy.Tests.HttpContextFakes {
    public class HttpRequestFake : HttpRequest {
        private HttpContext _httpContext;
        private string _scheme;
        private Uri _uri;

        public HttpRequestFake() {

        }

        public HttpRequestFake(Uri uri) {
            SetUrl(uri);
        }

        public void SetUrl(Uri uri) {
            string scheme;
            HostString hostString;
            PathString pathString;
            QueryString queryString;
            FragmentString fragmentString;
            UriHelper.FromAbsolute(uri.ToString(), out scheme, out hostString, out pathString, out queryString,
                out fragmentString);
            
            Host = hostString;
            Path = pathString;
            QueryString = queryString;
            Scheme = scheme;
            Uri = uri;
        }

        public Uri Uri { get; private set; }

        public override Task<IFormCollection> ReadFormAsync(
            CancellationToken cancellationToken = new CancellationToken()) {
            return Task.FromResult(Form);
        }

        public void SetHttpContext(HttpContext context) {
            _httpContext = context;
        }

        public override HttpContext HttpContext => _httpContext;
        public override string Method { get; set; } = "GET";

        public override string Scheme {
            get { return _scheme; }
            set {
                _scheme = value;
                IsHttps = _scheme.ToLower() == "https";
            }
        }

        public override bool IsHttps { get; set; } 
        public override HostString Host { get; set; } = new HostString("myserver", 80);
        public override PathString PathBase { get; set; }
        public override PathString Path { get; set; }
        public override QueryString QueryString { get; set; }
        public override IQueryCollection Query { get; set; } = new QueryCollection();
        public override string Protocol { get; set; }
        public override IHeaderDictionary Headers { get; } = new HeaderDictionary();
        public override IRequestCookieCollection Cookies { get; set; } = new RequestCookieCollection();
        public override long? ContentLength { get; set; }
        public override string ContentType { get; set; }
        public override Stream Body { get; set; }= new MemoryStream();
        public override bool HasFormContentType { get; }
        public override IFormCollection Form { get; set; } = new FormCollection(new Dictionary<string, StringValues>());
    }
}
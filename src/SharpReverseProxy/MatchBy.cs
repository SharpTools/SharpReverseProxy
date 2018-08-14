using Microsoft.AspNetCore.Http;
using System;
using System.Threading.Tasks;

namespace SharpReverseProxy {
    public static class MatchBy {

        public static Task<bool> Url(HttpRequest request, Func<string, bool> urlTest) {
            return Task.FromResult(urlTest(request.GetUri().AbsoluteUri));
        }

        public static Task<bool> Header(HttpRequest request, Func<IHeaderDictionary, bool> headerTest) {
            return Task.FromResult(headerTest(request.Headers));
        }
    }
}
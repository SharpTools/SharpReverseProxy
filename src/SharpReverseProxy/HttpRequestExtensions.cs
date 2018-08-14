using Microsoft.AspNetCore.Http;
using System;

namespace SharpReverseProxy {
    public static class HttpRequestExtensions {

        public static Uri GetUri(this HttpRequest request) {
            var uriString = $"{request.Scheme}://{request.Host}{request.PathBase}{request.Path}{request.QueryString}";
            return new Uri(uriString);
        }

        public static string GetUrl(this HttpRequest request) {
            return $"{request.Scheme}://{request.Host}{request.PathBase}{request.Path}{request.QueryString}";
        }
    }
}
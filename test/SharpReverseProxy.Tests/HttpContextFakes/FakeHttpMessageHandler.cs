using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;

namespace SharpReverseProxy.Tests.HttpContextFakes {
    public class FakeHttpMessageHandler : HttpMessageHandler {
        public HttpRequestMessage RequestMessage { get; private set; }

        public HttpResponseMessage ResponseMessageToReturn { get; set; } = new HttpResponseMessage { 
            Content = new MultipartFormDataContent {
                Headers = {
                    ContentType = MediaTypeHeaderValue.Parse("application/json")
                }
            },
            StatusCode = HttpStatusCode.OK
        };

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) {
            RequestMessage = request;
            return Task.FromResult(ResponseMessageToReturn);
        }
    }
}
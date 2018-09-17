using System.Net.Http;

namespace SharpReverseProxy {
    public class RequestBlockerResult {

        private RequestBlockerResult() {

        }

        public static RequestBlockerResult NotBlocked() {
            return new RequestBlockerResult();
        }

        public static RequestBlockerResult BlockAndRespondWith(HttpResponseMessage message) {
            return new RequestBlockerResult {
                Response = message
            };
        }

        internal HttpResponseMessage Response { get; set; }
    }
}
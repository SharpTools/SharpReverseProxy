using System;
using System.Net;

namespace SharpReverseProxy {
    public class ProxyResult {
        public ProxyStatus ProxyStatus { get; set; }
        public int HttpStatusCode { get; set; }
        public Uri OriginalUri { get; set; }
        public Uri ProxiedUri { get; set; }
        public TimeSpan Elapsed { get; set; }
        [Obsolete("Elipsed property is deprecated, please use Elapsed instead.")]
        public TimeSpan Elipsed {
            get { return Elapsed; }
            set { Elapsed = value; }
        }
    }
}
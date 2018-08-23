using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;

namespace SharpReverseProxy {
    public class ProxyOptions {
        private HttpClient _defaultHttpClient;
        public HttpClient DefaultHttpClient {
            get {
                return _defaultHttpClient ?? (_defaultHttpClient = new HttpClient(new HttpClientHandler {
                    AllowAutoRedirect = false,
                    UseCookies = false
                }));
            }
            set {
                _defaultHttpClient = value;
            }
        }
        public List<ProxyRule> ProxyRules { get; set; } = new List<ProxyRule>();
        public Func<ProxyResult, Task> Reporter { get; set; } = result => Task.CompletedTask;
        public ProxyOptions() { }

        public ProxyOptions(List<ProxyRule> rules, Func<ProxyResult, Task> reporter = null) {
            ProxyRules = rules;
            if (reporter != null) {
                Reporter = reporter;
            }
        }

        public void AddProxyRule(ProxyRule rule) {
            ProxyRules.Add(rule);
        }
    }
}
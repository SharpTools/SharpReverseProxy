using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;

namespace SharpReverseProxy {
    public class ProxyOptions {
        public List<ProxyRule> ProxyRules { get; } = new List<ProxyRule>();
        public HttpMessageHandler BackChannelMessageHandler { get; set; }
        public Action<ProxyResult> Reporter { get; set; } = result => { };

        public ProxyOptions() {}

        public ProxyOptions(IEnumerable<ProxyRule> rules, Action<ProxyResult> reporter = null) {
            ProxyRules = rules.ToList();
            if (reporter != null) {
                Reporter = reporter;
            }
        }

        public void AddProxyRule(ProxyRule rule) {
            ProxyRules.Add(rule);
        }
    }
}
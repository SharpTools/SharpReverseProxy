using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using NUnit.Framework;
using SharpReverseProxy.Tests.HttpContextFakes;

namespace SharpReverseProxy.Tests {
    public class ProxyTests {
        private HttpContextFake _context;
        private FakeHttpMessageHandler _fakeHttpMessageHandler;
        private ProxyMiddleware _proxy;
        private ProxyOptions _proxyOptions;
        private HttpRequestFake _request;
        private List<ProxyRule> _rules;

        [SetUp]
        public void SetUp() {
            _rules = new List<ProxyRule>();
            _fakeHttpMessageHandler = new FakeHttpMessageHandler();
            _request = new HttpRequestFake(new Uri("http://myserver.com/api/user"));
            _context = new HttpContextFake(_request);
            _proxyOptions = new ProxyOptions(_rules);
            _proxyOptions.BackChannelMessageHandler = _fakeHttpMessageHandler;

            var options = Options.Create(_proxyOptions);
            _proxy = new ProxyMiddleware(next => Task.FromResult(_request), options);
        }

        [Test]
        public async Task Should_match_rule() {
            var matched = false;
            _rules.Add(new ProxyRule {
                Matcher = uri => uri.AbsolutePath.Contains("api"),
                Modifier = (msg, user) => { matched = true; }
            });
            await _proxy.Invoke(_context);
            Assert.IsTrue(matched);
        }

        [Test]
        public async Task Should_not_match_any_rule() {
            var matched = false;
            ProxyResult result = null;
            _proxyOptions.Reporter = r => result = r;
            _rules.Add(new ProxyRule {
                Matcher = uri => false,
                Modifier = (msg, user) => { matched = true; }
            });
            await _proxy.Invoke(_context);
            Assert.IsFalse(matched);
            Assert.IsNotNull(result);
            Assert.AreEqual(ProxyStatus.NotProxied, result.ProxyStatus);
            Assert.AreEqual(_request.Uri, result.OriginalUri);
        }

        [Test]
        public async Task Should_call_reporter_when_request_is_proxied() {
            var targetUri = new Uri("http://myotherserver.com/api/user");
            ProxyResult result = null;
            _rules.Add(new ProxyRule {
                Matcher = uri => uri.AbsolutePath.Contains("api"),
                Modifier = (msg, user) => { msg.RequestUri = targetUri; }
            });
            _proxyOptions.Reporter = r => result = r;
            await _proxy.Invoke(_context);

            Assert.IsNotNull(result);
            Assert.AreEqual(ProxyStatus.Proxied, result.ProxyStatus);
            Assert.AreEqual(_request.Uri, result.OriginalUri);
            Assert.AreEqual(targetUri, result.ProxiedUri);
        }
    }
}
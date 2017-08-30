using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using NUnit.Framework;
using SharpReverseProxy.Tests.HttpContextFakes;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Net.Http.Headers;

namespace SharpReverseProxy.Tests {
    public class ProxyTests {
        private HttpContextFake _context;
        private FakeHttpMessageHandler _fakeHttpMessageHandler;
        private ProxyMiddleware _proxy;
        private ProxyOptions _proxyOptions;
        private HttpRequestFake _request;
        private HttpResponseFake _response;
        private List<ProxyRule> _rules;

        [SetUp]
        public void SetUp() {
            _rules = new List<ProxyRule>();
            _fakeHttpMessageHandler = new FakeHttpMessageHandler();
            _request = new HttpRequestFake(new Uri("http://myserver.com/api/user"));
            _response = new HttpResponseFake();
            _context = new HttpContextFake(_request, _response);
            _proxyOptions = new ProxyOptions(_rules);
            _proxyOptions.BackChannelMessageHandler = _fakeHttpMessageHandler;
            
            var options = Options.Create(_proxyOptions);
            _proxy = new ProxyMiddleware(next => Task.FromResult(_request), options);
        }

        [Test]
        public async Task Should_match_simple_rule() {
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

        [Test]
        public async Task Should_pass_all_headers() {
            _rules.Add(new ProxyRule {
                Matcher = uri => uri.AbsolutePath.Contains("api"),
                Modifier = (msg, user) => { }
            });
            await _proxy.Invoke(_context);

            _fakeHttpMessageHandler.ResponseMessageToReturn.Headers.Add("SomeHeader", "SomeValue");

            foreach (var header in _request.Headers) {
                var respHeader = _response.Headers[header.Key];
                Assert.AreEqual(header.Value.ToString(), respHeader.ToString());
            }
        }

        [Test]
        public async Task Should_pass_contentType() {
            _rules.Add(new ProxyRule {
                Matcher = uri => uri.AbsolutePath.Contains("api")
            });
            _fakeHttpMessageHandler.ResponseMessageToReturn.Content =
                new MultipartFormDataContent {
                    Headers = {
                        ContentType = MediaTypeHeaderValue.Parse("application/json")
                    }
                };
            await _proxy.Invoke(_context);
            Assert.AreEqual("application/json", _context.Response.ContentType);
        }

        [Test]
        public async Task Should_call_responseModifier_if_set() {
            _rules.Add(new ProxyRule {
                Matcher = uri => uri.AbsolutePath.Contains("api"),
                Modifier = (msg, user) => { },
                ResponseModifier = async (res, ctx) => {
                    var bytes = Encoding.UTF8.GetBytes("Hello, world!");
                    await ctx.Response.Body.WriteAsync(bytes, 0, bytes.Length);
                }
            });
            await _proxy.Invoke(_context);

            var body = (MemoryStream)_response.Body;
            var bodyText = Encoding.UTF8.GetString(body.ToArray());
            Assert.AreEqual("Hello, world!", bodyText);
        }

        [Test]
        public async Task Should_provide_clean_response_if_preProcessResponse_is_false() {
            _fakeHttpMessageHandler.ResponseMessageToReturn
                                   .Headers.Add("foo", "bar");

            _rules.Add(new ProxyRule {
                Matcher = uri => uri.AbsolutePath.Contains("api"),
                PreProcessResponse = false,
                ResponseModifier = async (res, ctx) => {
                    var bytes = Encoding.UTF8.GetBytes("Hello, world!");
                    await ctx.Response.Body.WriteAsync(bytes, 0, bytes.Length);
                }
            });
            await _proxy.Invoke(_context);

            Assert.AreEqual(0, _context.Response.Headers.Count);
        }

        [Test]
        public async Task Should_pass_full_response_if_preProcessResponse_is_false_and_no_responseModifier_is_provided() {
            _fakeHttpMessageHandler.ResponseMessageToReturn
                                   .Headers.Add("foo", "bar");

            _rules.Add(new ProxyRule {
                Matcher = uri => uri.AbsolutePath.Contains("api"),
                PreProcessResponse = false
            });
            await _proxy.Invoke(_context);

            Assert.AreEqual(1, _context.Response.Headers.Count);
        }
    }
}
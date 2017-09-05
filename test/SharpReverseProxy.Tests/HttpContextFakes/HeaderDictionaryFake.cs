using System;
using System.Collections;
using System.Collections.Generic;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;

namespace SharpReverseProxy.Tests.HttpContextFakes {
    public class HeaderDictionaryFake : IHeaderDictionary {

        private Dictionary<string, StringValues> _headers = new Dictionary<string, StringValues>();
        private HttpResponseFake _parentResponse;

        public HeaderDictionaryFake(HttpResponseFake parentResponse) {
            _parentResponse = parentResponse;
        }

        public StringValues this[string key] {
            get => ContainsKey(key) ? _headers[key] : StringValues.Empty;
            set => _headers[key] = value;
        }

        public bool IsReadOnly => false;

        public ICollection<string> Keys => _headers.Keys;

        public ICollection<StringValues> Values => _headers.Values;

        public int Count => _headers.Count;

        public void Add(string key, StringValues value) {
            if(_parentResponse.HasStarted) {
                ThrowReponseAlreadyStartedException();
            }
            _headers.Add(key, value);
        }

        public void Add(KeyValuePair<string, StringValues> item) {
            Add(item.Key, item.Value);
        }

        public void Clear() {
            if (_parentResponse.HasStarted) {
                ThrowReponseAlreadyStartedException();
            }
            _headers.Clear();
        }

        public bool Contains(KeyValuePair<string, StringValues> item) {
            var hasKey = _headers.ContainsKey(item.Key);
            if (!hasKey) {
                return false;
            }
            return _headers[item.Key].Equals(item.Value);
        }

        public bool ContainsKey(string key) {
            return _headers.ContainsKey(key);
        }

        public void CopyTo(KeyValuePair<string, StringValues>[] array, int arrayIndex) {
            throw new NotImplementedException();
        }

        public IEnumerator<KeyValuePair<string, StringValues>> GetEnumerator() {
            return _headers.GetEnumerator();
        }

        public bool Remove(string key) {
            if (_parentResponse.HasStarted) {
                ThrowReponseAlreadyStartedException();
            }
            return _headers.Remove(key);
        }

        public bool Remove(KeyValuePair<string, StringValues> item) {
            return Remove(item.Key);
        }

        public bool TryGetValue(string key, out StringValues value) => _headers.TryGetValue(key, out value);

        IEnumerator IEnumerable.GetEnumerator() => _headers.GetEnumerator();

        private void ThrowReponseAlreadyStartedException() {
            throw new InvalidOperationException("Headers are read - only, response has already started.");
        }
    }
}
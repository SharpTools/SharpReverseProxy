using System;

namespace SampleApiAuthentication.Authentication {
    public class TokenOptions {
        public string Issuer { get; set; } = "Application";
        public string Audience { get; set; } = "DefaultClient";
        public TimeSpan Expiration { get; set; } = TimeSpan.FromMinutes(15);
    }
}
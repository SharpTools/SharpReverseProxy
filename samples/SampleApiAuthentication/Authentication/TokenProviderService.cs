using System;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using Microsoft.IdentityModel.Tokens;

namespace SampleApiAuthentication.Authentication {
    public class TokenProviderService {
        public static readonly string AccessTokenName = "access_token";
        public SecurityKey SecurityKey { get; }
        private SigningCredentials _signingCredentials;

        public TokenProviderService(string secretKey) {
            var key = Encoding.ASCII.GetBytes(secretKey);
            if (key.Length < 16) {
                throw new ArgumentException(
                    $"The secret key for the algorithm: 'HS256' cannot have less than: '128' bits. KeySize is: '{key.Length * 8}'.",
                    nameof(secretKey));
            }
            SecurityKey = new SymmetricSecurityKey(key);
            _signingCredentials = new SigningCredentials(SecurityKey, SecurityAlgorithms.HmacSha256);
        }

        public async Task<string> GenerateToken(string username, string role, TokenOptions options) {
            var now = DateTime.UtcNow;
            // Specifically add the jti (nonce), iat (issued timestamp), and sub (subject/user) claims.
            // You can add other claims here, if you want:
            var claims = new[] {
                new Claim(JwtRegisteredClaimNames.Sub, username),
                new Claim(JwtRegisteredClaimNames.Jti, await GenerateNonce()),
                new Claim(JwtRegisteredClaimNames.Iat, ToUnixEpochDate(now).ToString(), ClaimValueTypes.Integer64),
                new Claim("roles", role),
            };
            // Create the JWT and write it to a string
            var jwt = new JwtSecurityToken(
                options.Issuer,
                options.Audience,
                claims,
                now,
                now.Add(options.Expiration),
                _signingCredentials);
            return new JwtSecurityTokenHandler().WriteToken(jwt);
        }

        public virtual async Task<string> GenerateNonce() {
            return await Task.FromResult(Guid.NewGuid().ToString());
        }

        public static long ToUnixEpochDate(DateTime date)
            =>
            (long)
            Math.Round((date.ToUniversalTime() - new DateTimeOffset(1970, 1, 1, 0, 0, 0, TimeSpan.Zero)).TotalSeconds);
    }
}
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
//using Microsoft.AspNetCore.Http.Authentication;
using Microsoft.IdentityModel.Tokens;

namespace SampleWeb.Authentication
{
    public class CustomJwtDataFormat : ISecureDataFormat<AuthenticationTicket> {
        private readonly string _algorithm;
        private readonly TokenValidationParameters _validationParameters;

        public CustomJwtDataFormat(string algorithm, TokenValidationParameters validationParameters) {
            _algorithm = algorithm;
            _validationParameters = validationParameters;
        }

        public AuthenticationTicket Unprotect(string protectedText)
            => Unprotect(protectedText, null);

        public AuthenticationTicket Unprotect(string protectedText, string purpose) {
            var handler = new JwtSecurityTokenHandler();
            handler.InboundClaimTypeMap[JwtRegisteredClaimNames.Sub] = ClaimTypes.Name;
            ClaimsPrincipal principal;
            try {
                SecurityToken validToken;
                principal = handler.ValidateToken(protectedText, _validationParameters, out validToken);
                var validJwt = validToken as JwtSecurityToken;
                if (validJwt == null) {
                    throw new ArgumentException("Invalid JWT");
                }
                if (!validJwt.Header.Alg.Equals(_algorithm, StringComparison.Ordinal)) {
                    throw new ArgumentException($"Algorithm must be '{_algorithm}'");
                }
            }
            catch (SecurityTokenValidationException) {
                return null;
            }
            catch (ArgumentException) {
                return null;
            }
            return new AuthenticationTicket(principal, new AuthenticationProperties(), "Cookie");
        }

        public string Protect(AuthenticationTicket data) {
            throw new NotImplementedException();
        }

        public string Protect(AuthenticationTicket data, string purpose) {
            throw new NotImplementedException();
        }
    }
}

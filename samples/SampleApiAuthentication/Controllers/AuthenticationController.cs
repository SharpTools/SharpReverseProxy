using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using SampleApiAuthentication.Authentication;

namespace SampleApiAuthentication.Controllers
{
    [Route("api/[controller]")]
    public class AuthenticationController : Controller {
        private readonly ILogger _logger;
        private readonly TokenOptions _tokenOptions;
        private readonly TokenProviderService _tokenProviderService;

        public AuthenticationController(TokenOptions tokenOptions,
                                        TokenProviderService tokenProviderService) {
            _tokenOptions = tokenOptions;
            _tokenProviderService = tokenProviderService;
        }

        [HttpGet] //I know, it's for testing
        [Route("login")]
        public async Task<IActionResult> Login(string username, string password, string returnurl) {
            //always true, it's for testing
            var token = await _tokenProviderService.GenerateToken(username, "admin", _tokenOptions);
            Response.Cookies.Append(TokenProviderService.AccessTokenName, token);
            if (!string.IsNullOrEmpty(returnurl)) {
                return Redirect(returnurl);
            }
            return new JsonResult(new {
                access_token = token,
                expires_in = (int)TimeSpan.FromDays(365).TotalSeconds
            });
        }

        [HttpGet] //I know, it's for testing
        [Route("logout")]
        public IActionResult Logout(string returnurl) {
            Response.Cookies.Delete(TokenProviderService.AccessTokenName);
            if (!string.IsNullOrEmpty(returnurl)) {
                return Redirect(returnurl);
            }
            return Ok();
        }
    }
}

using Microsoft.AspNetCore.Http;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace SharpReverseProxy {
    public static class ModifyResponse {

        public static async Task ReplaceDomainWhenText(HttpResponseMessage resp, HttpContext ctx, string domain) {
            switch (resp.Content.Headers.ContentType.MediaType) {
                case "text/html":
                case "application/xhtml+xml":
                case "application/javascript":
                case "text/css":
                    var body = await resp.Content.ReadAsStringAsync();
                    body = Regex.Replace(body,
                                         @"(http(s)?:)?//(?:www\.)?example.com",
                                         $"{ctx.Request.Scheme}://{ctx.Request.Host}",
                                         RegexOptions.IgnoreCase);
                    var data = Encoding.UTF8.GetBytes(body);
                    ctx.Response.ContentLength = data.Length;
                    
                    await ctx.Response.Body.WriteAsync(data, 0, data.Length);
                    break;
                default:
                    await resp.Content.CopyToAsync(ctx.Response.Body);
                    break;
            }
        }
    }
}
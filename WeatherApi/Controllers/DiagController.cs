//using Microsoft.AspNetCore.Mvc;

using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace WeatherApi.Controllers
{
    [ApiController]
    [ApiVersion("1.0")]
    [Route("api/v{version:apiVersion}/[controller]")]
    [Authorize]
    [ApiExplorerSettings(IgnoreApi = true)]
    public class DiagController : ControllerBase
    {
        public record RequestDump(Dictionary<string, string> headers, Dictionary<string, string> cookies, Dictionary<string, string> claims, Dictionary<string, KeyValuePair<string, object?>> routes);
        [HttpGet(Name = "GetDiag")]
        [Authorize]

        public RequestDump Diag()
        {
            var req = this.Request;
            var user = User;
            Dictionary<string, string> headers = new();
            Dictionary<string, string> cookies = new();
            Dictionary<string, string> claims = new();
            Dictionary<string, KeyValuePair<string, object?>> routes = new();

            foreach (var header in req.Headers)
            {
                headers.Add(header.Key, header.Value!);
            }
            foreach (var cookie in req.Cookies)
            {
                cookies.Add(cookie.Key, cookie.Value);
            }
            foreach (var route in req.RouteValues)
            {
                routes.Add(route.Key, route);
            }
            if (user.Identity!.IsAuthenticated)
                foreach (var claim in user.Claims)
                {
                    claims.Add(claim.Type, claim.Value);
                }

            return new RequestDump(headers, cookies, claims, routes);

        }
    }
}

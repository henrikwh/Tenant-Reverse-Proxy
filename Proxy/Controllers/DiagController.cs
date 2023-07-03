//using Microsoft.AspNetCore.Mvc;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Microsoft.FeatureManagement.Mvc;
using Shared.Models;

namespace SaaS.Proxy.Controllers
{
    [ApiController]
    [Route("[controller]")]
    [FeatureGate("ShowDebugView")]
    public class DiagController : ControllerBase
    {
        private readonly ILogger<DiagController> _logger;
        private readonly IOptionsMonitor<TenantSettings> _tenants;

        public DiagController(ILogger<DiagController> logger, IOptionsMonitor<TenantSettings> tenants)
        {
            _logger = logger;
            _tenants = tenants;
        }

        public record RequestDump(Dictionary<string, string> headers, Dictionary<string, string> cookies, Dictionary<string, string> claims, 
            Dictionary<string, KeyValuePair<string, object?>> routes, 
            List<Tenant> tenants);

        [HttpGet(Name = "GetDiag")]
        
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

            return new RequestDump(headers, cookies, claims, routes,_tenants.CurrentValue.Tenants);

        }
    }
}

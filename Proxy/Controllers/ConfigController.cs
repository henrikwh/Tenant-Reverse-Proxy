using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Microsoft.FeatureManagement;
using Microsoft.FeatureManagement.Mvc;

using System.Runtime;
using System.Text;
using System.Text.Json;


using Yarp.ReverseProxy.Configuration;

namespace SaaS.Proxy.Controllers
{
    [Route("[controller]")]
    //[Route("api/v{version:apiVersion}/{icao}/{sensor}/[controller]")]
    [ApiController]
    [FeatureGate("ShowDebugView")]
    public class ConfigController : ControllerBase
    {

        private readonly ILogger<ConfigController> _logger;
        private readonly IConfigurationRoot _root;
        private readonly IProxyConfigProvider _proxyConfig;
        private readonly InMemoryConfigProvider _memConfigProvider;
        private readonly IOptions<List<RouteConfig>> _routes;

        public ConfigController(ILogger<ConfigController> logger, IConfigurationRoot root, IProxyConfigProvider configProvider, InMemoryConfigProvider memConfigProvider, IOptions<List<RouteConfig>> r)
        {
            _logger = logger;
            _root = root;
            _proxyConfig = configProvider;
            _memConfigProvider = memConfigProvider;
            _routes = r;
        }

        /// <summary>
        /// Shows the entire configuration. Enable featureflag to enable the endpoint.
        /// </summary>
        [HttpGet("GetConfigDump")]
        public string GetConfig()
        {
            return _root.GetDebugView();
        }
        public record ProxyConfig(IProxyConfig memory, IProxyConfig config);
        [HttpGet("GetproxyConfigDump")]

        public ProxyConfig GetProxyConfig()
        {
            var cfg = _proxyConfig.GetConfig();
            var memCfg = _memConfigProvider.GetConfig();
            return new ProxyConfig(memCfg, cfg);
        }

    }
}


using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Microsoft.FeatureManagement.Mvc;
using Yarp.ReverseProxy.Configuration;

namespace WeatherApi.Controllers
{
    [FeatureGate("ShowDebugView")]
    public class ConfigController : ControllerBase
    {

        private readonly ILogger<ConfigController> _logger;
        private readonly IConfigurationRoot _root;

        public ConfigController(ILogger<ConfigController> logger, IConfigurationRoot root)
        {
            _logger = logger;
            _root = root;
        }

        /// <summary>
        /// Shows the entire configuration. Enable featureflag to enable the endpoint.
        /// </summary>
        [HttpGet("GetConfigDump")]
        public string GetConfig()
        {
            return _root.GetDebugView();
        }
      

    }
}

//using Microsoft.AspNetCore.Mvc;
//using Microsoft.FeatureManagement.Mvc;
//using Yarp.ReverseProxy.Configuration;

//namespace SaaS.Proxy.Controllers
//{
//    [ApiController]
//    [Route("[controller]")]
//    //[FeatureGate("ShowDebugView")]
//    public class ProxyController : ControllerBase
//    {

//        private readonly ILogger<ProxyController> _logger;
//        private readonly InMemoryConfigProvider _memConfigProvider;
//        private readonly IProxyConfigProvider _configProvider;

//        public ProxyController(ILogger<ProxyController> logger, InMemoryConfigProvider memConfigProvider, IProxyConfigProvider configProvider)
//        {
//            _logger = logger;
//            _memConfigProvider = memConfigProvider;
//            _configProvider = configProvider;
//        }

//        /// <summary>
//        /// 
//        /// </summary>

//        /// <returns></returns>
//        [HttpGet(Name = "GetProxyConfig")]
//        public IProxyConfig Get()
//        {
//            var cfg = _configProvider.GetConfig();


//            return cfg;
//        }

//        /// <summary>
//        /// 
//        /// </summary>
//        /// <param name="tenantId" example="t1"></param>
//        /// <param name="path" example="/blue/{**remainder}"></param>
//        /// <param name="address" example=" https://cphwh-blue.azurewebsites.net/"></param>
//        /// <returns></returns>

//        [HttpPost(Name = "PostProxyConfig")]
//        public IActionResult Post(string tenantId, string path, Uri address)
//        {
//            var cfg = _memConfigProvider.GetConfig();
//            var clusters = cfg.Clusters.ToList();


//            var routes = cfg.Routes.ToList();

//            if (routes.Count(c => c.RouteId == tenantId) != 0)
//            {
//                return new BadRequestObjectResult("Tenant route already exists");
//            }

//            clusters.Add(new ClusterConfig()
//            {
//                ClusterId = tenantId,
//                Destinations = new Dictionary<string, DestinationConfig>() {
//                { "1", new DestinationConfig() { Address = address.ToString() } } }
//            }); ;

//            Dictionary<string, string> d = new Dictionary<string, string>();
//            routes.Add(new RouteConfig()
//            {
//                RouteId = tenantId,
//                ClusterId = tenantId,
//                Match = new RouteMatch() { Path = path },
//                Transforms = new List<Dictionary<string, string>>() {
//                    new Dictionary<string, string>(){
//                        { "PathRemovePrefix",  path.Split('/')[1] }
//                    }

//                }
//            });
//            _memConfigProvider.Update(routes, clusters);
//            return new OkResult();
//        }
//        [HttpDelete(Name = "DeleteProxyConfig")]
//        public IActionResult Delete(string tenantId)
//        {
//            var cfg = _memConfigProvider.GetConfig();
//            var clusters = cfg.Clusters.Where(s => s.ClusterId != tenantId).ToList();
//            var routes = cfg.Routes.Where(s => s.RouteId != tenantId).ToList();
//            _memConfigProvider.Update(routes, clusters);
//            return new OkResult();
//        }
//    }
//}
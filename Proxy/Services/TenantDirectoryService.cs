using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using System.Runtime.CompilerServices;

using Yarp.ReverseProxy.Configuration;

using Shared.Models;
using Shared.Repositories;

namespace SaaS.Proxy.Services
{
    public class TenantDirectoryService : ITenantDirectoryService
    {
        private readonly ILogger<TenantDirectoryService> _logger;
        private readonly List<Tenant>? _tenants;
        private readonly InMemoryConfigProvider _proxyConfig;
        private readonly ITenantRepository _tenantRepo;
        //public static Dictionary<string, Tenant> _tds = new();
        private static object? _singleton;
        public TenantDirectoryService(ILogger<TenantDirectoryService> logger, ITenantRepository tenantRepo, InMemoryConfigProvider memConfig)
        {
            _logger = logger;
            _proxyConfig = memConfig;
            _tenantRepo = tenantRepo;
            //singleton ensures that the tenant directory service is initialised.
            if (_singleton is null)
            {
                _singleton = new object();
                _tenants = tenantRepo.GetAllTenants();
                //on startup the tenants are loaded and populated.
                this.AddTenantsToRouting(_tenants);
            }
        }
        public Tenant TenantLookup(string tenantId)
        {
            return _tenantRepo.GetTenant(tenantId);
        }
        public void AddTenantsToRouting(List<Tenant> tenants)
        {
            var cfg = _proxyConfig.GetConfig();

            var clusters = new List<ClusterConfig>();
            var routes = new List<RouteConfig>();
            if (tenants == null)
            {
                _logger.LogWarning("No tenants in collection");
            }
            else
                foreach (Tenant tenant in tenants!.Where(w=>w.State==TenantState.Enabled))
                {
                    clusters.Add(new ClusterConfig()
                    {
                        ClusterId = tenant.Tid!,
                        Destinations = new Dictionary<string, DestinationConfig>() {
                            { tenant.Tid!, new DestinationConfig() { Address = tenant.Destination!} }
                    }
                    });

                    Dictionary<string, string> d = new Dictionary<string, string>();
                    routes.Add(new RouteConfig()
                    {
                        //add proxyrule, where tenantid is in the route
                        RouteId = tenant.Tid + "-route",
                        ClusterId = tenant.Tid,
                        Order = 1,
                        Match = new RouteMatch() { Path = "/" + tenant.Tid + "/{**remainder}" },
                        AuthorizationPolicy = "customPolicy",
                        Transforms = new List<Dictionary<string, string>>() {
                            new Dictionary<string, string>(){
                                { "PathRemovePrefix",  "/" +tenant.Tid }
                            }
                        }
                    });

                    routes.Add(new RouteConfig()
                    {
                        //add proxyrule, where tenantid is in the header
                        RouteId = tenant.Tid + "-header",
                        ClusterId = tenant.Tid,
                        Order = 100,
                        Match = new RouteMatch()
                        {
                            Path = "{**catch-all}",
                            Headers = new[] {
                                new RouteHeader()
                                {
                                    Name = "TenantId",
                                    Values = new[] { tenant.Tid! },
                                    Mode = HeaderMatchMode.ExactHeader
                                }
                            }
                        },
                        AuthorizationPolicy = "customPolicy"
                    });
                }

            //generel rules that catches all api requests. 
            
            var tokenRoute = cfg.Routes.Where(w => w.RouteId == "token").First();
            var clusterDefault = cfg.Clusters.Where(w => w.ClusterId == "root").First();
            if (tokenRoute == null || clusterDefault == null)
            {
                _logger.LogCritical("Token routing rules not set");
                throw new Exception("Token routing rules not set");

            }
            routes.Add(tokenRoute);
            clusters.Add(clusterDefault);
            _proxyConfig.Update(routes, clusters);
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Yarp.ReverseProxy.Configuration;

namespace Shared.Models
{
    public class ProxyConfig
    {
        public List<RouteConfig> Routes { get; set; } = new List<RouteConfig>();
        public List<ClusterConfig> Clusters { get; set; } = new List<ClusterConfig>();
    }
    //public class MyRouteConfig
    //{
    //    public class RouteConfig
    //    {



    //        //
    //        // Summary:
    //        //     Globally unique identifier of the route. This field is required.
    //        public string RouteId { get; set; }

    //        //
    //        // Summary:
    //        //     Parameters used to match requests. This field is required.
    //        public RouteMatch Match { get; set; }

    //        //
    //        // Summary:
    //        //     Optionally, an order value for this route. Routes with lower numbers take precedence
    //        //     over higher numbers.
    //        public int? Order { get; set; }

    //        //
    //        // Summary:
    //        //     Gets or sets the cluster that requests matching this route should be proxied
    //        //     to.
    //        public string? ClusterId { get; set; }

    //        //
    //        // Summary:
    //        //     The name of the AuthorizationPolicy to apply to this route. If not set then only
    //        //     the FallbackPolicy will apply. Set to "Default" to enable authorization with
    //        //     the applications default policy. Set to "Anonymous" to disable all authorization
    //        //     checks for this route.
    //        public string? AuthorizationPolicy { get; set; }

    //        //
    //        // Summary:
    //        //     The name of the RateLimiterPolicy to apply to this route. If not set then only
    //        //     the GlobalLimiter will apply. Set to "Disable" to disable rate limiting for this
    //        //     route. Set to "Default" or leave empty to use the global rate limits, if any.
    //        public string? RateLimiterPolicy { get; set; }

    //        //
    //        // Summary:
    //        //     The name of the CorsPolicy to apply to this route. If not set then the route
    //        //     won't be automatically matched for cors preflight requests. Set to "Default"
    //        //     to enable cors with the default policy. Set to "Disable" to refuses cors requests
    //        //     for this route.
    //        public string? CorsPolicy { get; set; }

    //        //
    //        // Summary:
    //        //     An optional override for how large request bodies can be in bytes. If set, this
    //        //     overrides the server's default (30MB) per request. Set to '-1' to disable the
    //        //     limit for this route.
    //        public long? MaxRequestBodySize { get; set; }

    //        //
    //        // Summary:
    //        //     Arbitrary key-value pairs that further describe this route.
    //        public IReadOnlyDictionary<string, string>? Metadata { get; set; }

    //        //
    //        // Summary:
    //        //     Parameters used to transform the request and response. See Yarp.ReverseProxy.Transforms.Builder.ITransformBuilder.
    //        public IReadOnlyList<IReadOnlyDictionary<string, string>>? Transforms { get; set; }

    //    }
    //}
}

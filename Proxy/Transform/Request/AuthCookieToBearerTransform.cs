using Yarp.ReverseProxy.Transforms;

namespace SaaS.Proxy.Transform.Request
{
    //public class AuthCookieToBearerTransform : RequestTransform
    //{
    //    public override async ValueTask ApplyAsync(RequestTransformContext context)
    //    {
    //        var user = context.HttpContext.User;
    //        if (user.Identity?.IsAuthenticated ?? false)
    //        {
    //            context.ProxyRequest.Headers.Add("isauth", "true");
    //        }else           
    //            context.ProxyRequest.Headers.Add("isauth", "false");
    //        await Task.CompletedTask;
    //    }
    //}
}

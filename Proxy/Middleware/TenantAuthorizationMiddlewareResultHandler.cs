//using Microsoft.AspNetCore.Authorization.Policy;
//using Microsoft.AspNetCore.Authorization;
//using Microsoft.Azure.Amqp.Sasl;

//namespace SaaS.Proxy.Middleware
//{


//    public class TenantAuthorizationMiddlewareResultHandlers : IAuthorizationMiddlewareResultHandler
//    {
//        private readonly ILogger<TenantAuthorizationMiddlewareResultHandlers> _logger;
//        private readonly Microsoft.AspNetCore.Authorization.Policy.AuthorizationMiddlewareResultHandler _defaultHandler = new();

//        public TenantAuthorizationMiddlewareResultHandlers(ILogger<TenantAuthorizationMiddlewareResultHandlers> logger)
//        {
//            _logger = logger;
//        }

//        public async Task HandleAsync(
//            RequestDelegate next,
//            HttpContext context,
//            AuthorizationPolicy policy,
//            PolicyAuthorizationResult authorizeResult)
//        {
//            var authorizationFailureReason = authorizeResult.AuthorizationFailure?.FailureReasons.FirstOrDefault();
//            var message = authorizationFailureReason?.Message;
//            _logger.LogInformation("Authorization Result says {Message}",
//                message
//            );

//            await _defaultHandler.HandleAsync(next, context, policy, authorizeResult);

//        }
//    }
//}

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Shared.Models;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Net;
using System.Security.Claims;
using System.Security.Principal;
using System.Text;
using System.Threading.Tasks;

namespace Shared.Attributes
{
    
    public class AuthRequirement : IAuthorizationRequirement
    {
    }
    public class AuthRequirementHandler : AuthorizationHandler<AuthRequirement>
    {
        private readonly IHttpContextAccessor _httpContextAccessor;

        public AuthRequirementHandler(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }



        protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, AuthRequirement requirement)
        {
            if (_httpContextAccessor.HttpContext?.Request.Headers.Authorization.FirstOrDefault() is null)
                context.Fail();
            else {
                var auth = _httpContextAccessor.HttpContext.Request.Headers.Authorization.FirstOrDefault()!.Replace("Bearer ", string.Empty);
                var handler = new JwtSecurityTokenHandler();
                var token = handler.ReadJwtToken(auth);
                context.Succeed(requirement);
            }


            return Task.FromResult(0);
        }
    }
}

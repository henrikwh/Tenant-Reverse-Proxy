using Microsoft.IdentityModel.Tokens;
using Shared.Models;
using System.Diagnostics;
using System.IdentityModel.Tokens.Jwt;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text;
using Yarp.ReverseProxy.Configuration;
using Yarp.ReverseProxy.Forwarder;
using Yarp.ReverseProxy.Transforms;
using Shared.Repositories;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using System.Security.Cryptography;
using Proxy.Services;
using Microsoft.AspNetCore.Mvc;


namespace SaaS.Proxy.Configuration
{
    public static class Gateway
    {
        public static string CreateHash(string input)
        {
            byte[] inputBytes = Encoding.ASCII.GetBytes(input);
            byte[] hashBytes = System.Security.Cryptography.SHA256.HashData(inputBytes);
            StringBuilder sb = new();
            for (int i = 0; i < hashBytes.Length; i++)
            {
                sb.Append(hashBytes[i].ToString("X2"));
            }
            return sb.ToString();
        }

        public static async Task<string> GetTokenAsync(List<Claim> authClaims, JwtSettings jwt, IPermissionService permissions)
        {
            try
            {
                var mySecret = jwt.Secret;
                var mySecurityKey = new SymmetricSecurityKey(Encoding.ASCII.GetBytes(mySecret));

                var myIssuer = jwt.ValidIssuer;
                var myAudience = jwt.ValidAudience;

                var tokenHandler = new JwtSecurityTokenHandler();

                var exp = int.Parse(authClaims.Where(w => w.Type == "exp").Select(s => s.Value).First());
                var expiration = DateTime.Now - new DateTime(1970, 1, 1).AddSeconds(exp);


                var tokenDescriptor = new SecurityTokenDescriptor
                {
                    Subject = new ClaimsIdentity(authClaims.Where(w => w.Type != "aud"))
                    ,
                    Expires = DateTime.UtcNow.AddDays(7),
                    Issuer = myIssuer,
                    Audience = myAudience,
                    SigningCredentials = new SigningCredentials(mySecurityKey, SecurityAlgorithms.HmacSha256Signature)
                    ,
                    Claims = permissions.GetPermissions(authClaims.Where(w => w.Type == "http://schemas.microsoft.com/identity/claims/tenantid").Select(s => s.Value).First()) ?? null
                };

                var token = tokenHandler.CreateToken(tokenDescriptor);

                return await Task.FromResult(tokenHandler.WriteToken(token));
            }
            catch (Exception)
            {
                throw;
            }

        }
 

        public static void AddGateway(this WebApplicationBuilder builder, IServiceProvider serviceProvider, string configKey = "ReverseProxy:Settings:ReverseProxy")
        {
            ITenantRepository tenantRepository = (ITenantRepository?)serviceProvider.GetService(typeof(ITenantRepository)) ?? throw new Exception("TenantRepository not found");
            IMemoryCache cache = (IMemoryCache?)serviceProvider.GetService(typeof(IMemoryCache)) ?? throw new Exception("IMemoryCache not found");
            IPermissionService permissionService = (IPermissionService?)serviceProvider.GetService(typeof(IPermissionService)) ?? throw new Exception("IPermissionService not found");
            
            JwtSettings jwt = new();
            builder.Configuration.GetSection("Jwt").Bind(jwt); ;

            builder.Services.AddReverseProxy()
                    .LoadFromMemory(new List<RouteConfig>() {
                        new RouteConfig(){ RouteId = "root", ClusterId = "root", Match = new RouteMatch(){ Path="/root" } }.WithTransformPathRemovePrefix("/root"),
                        new RouteConfig(){
                RouteId = "token",
                //Note: clusterid does not matter! At runtime, its changed dynamically
                ClusterId = "root",
                Order = 100,
                Match = new RouteMatch()
                {
                    Path = "/api/{**catch-all}",
                    Headers = new[] {
                                new RouteHeader()
                                {
                                    Name = "TenantId",
                                    Mode = HeaderMatchMode.NotExists
                                }
                            }
                },
                AuthorizationPolicy = "tokenPolicy"
            }
                    },
                        new List<ClusterConfig>() {
                                        new ClusterConfig(){ ClusterId = "root", Destinations = new Dictionary<string,DestinationConfig>(){
                                            { "d1", new DestinationConfig(){ Address = "https://cphwh-signup.azurewebsites.net" } }
                                        }
                                        }

                    })
                    .LoadFromConfig(builder.Configuration.GetSection(configKey))
                    .AddTransforms(async ctx =>
                    {
                        ctx.RequestTransforms.Add(new RequestHeaderRemoveTransform("Cookie"));
                        var authPolicy = ctx.Route.AuthorizationPolicy;
                        if (!string.IsNullOrEmpty(authPolicy))
                        {
                            ctx.RequestTransforms.Add(new RequestHeaderValueTransform("yarp-AuthZPolicy", authPolicy ?? string.Empty, true));
                        }
                        await Task.CompletedTask;
                    })
                        .AddTransforms(async transformBuilderContext =>  // Add transforms inline
                        {
                            List<Claim> claims = new();
                            string tenantId = "NA";

                            if (!string.IsNullOrEmpty(transformBuilderContext.Route.AuthorizationPolicy))
                                transformBuilderContext.AddRequestTransform(async transformContext =>
                                {
                                    claims = transformContext.HttpContext.User.Claims.ToList();
                                    tenantId = claims.Where(w => w.Type == "http://schemas.microsoft.com/identity/claims/tenantid").Select(s => s.Value).First();
                                    var uid = claims.Where(w => w.Type == "http://schemas.microsoft.com/identity/claims/objectidentifier").Select(s => s.Value).First();

                                    Activity.Current?.AddTag("d-TenantId", tenantId);
                                    Activity.Current?.AddTag("d-UserId", uid);

                                    var authheader = CreateHash(transformContext.HttpContext.Request.Headers.Authorization.ToString());
                                    //cache the token mapping, to ensure that token is not created on every request.
                                    if (!cache.TryGetValue(authheader, out string? header))
                                    {
                                        jwt.ValidAudience = transformContext.DestinationPrefix ?? "NA";
                                        var token = await GetTokenAsync(claims, jwt, permissionService);
                                        transformContext.ProxyRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

                                        //cache the token, based on the token expiration issued from IDP
                                        var exp = int.Parse(claims.Where(w => w.Type == "exp").Select(s => s.Value).First());
                                        TimeSpan expiration = DateTime.Now - new DateTime(1970, 1, 1).AddSeconds(exp);
                                        cache.Set(authheader, token, new MemoryCacheEntryOptions() { AbsoluteExpiration = DateTimeOffset.Now.AddMinutes(expiration.TotalMinutes)});
                                    }
                                    else
                                    {
                                        transformContext.ProxyRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", header);
                                    }
                                });

                            if (string.Equals("tokenPolicy", transformBuilderContext.Route.AuthorizationPolicy))
                            {
                                transformBuilderContext.AddRequestTransform(async transformContext =>
                                {
                                    var t = tenantRepository!.GetTenant(tenantId);

                                    transformContext.ProxyRequest.RequestUri = RequestUtilities.MakeDestinationAddress(t!.Destination!, transformContext.HttpContext.Request.Path, transformContext.HttpContext.Request.QueryString);
                                    await Task.CompletedTask;
                                });
                            }
                            await Task.CompletedTask;
                        })
                        .Services.AddOptions<TenantSettings>()
                        
            ;



            //builder.Services.AddAuthorization(options =>
            //{
            //    options.AddPolicy("customPolicy", policy =>
            //    {

            //        policy.RequireAuthenticatedUser();
            //    });
            //});


        }
        private static void UseYarp(this WebApplication app)
        {
#pragma warning disable ASP0014
            app.UseEndpoints(endpoints =>
            {
                endpoints.MapReverseProxy(proxyPipeline =>
                {
                    proxyPipeline.Use((context, next) =>
                    {
                        return next();
                    });
                    proxyPipeline.UseSessionAffinity();
                    //proxyPipeline.UseLoadBalancing();
                    //proxyPipeline.UsePassiveHealthChecks();
                    proxyPipeline.Use(async (context, next) =>
                    {
                        LogRequest(context);
                        await next();
                        LogResponse(context);
                    });
                });
            });
#pragma warning restore ASP0014
        }

        private static void LogResponse(HttpContext context)
        {
            Console.WriteLine(context.Request.Path);
        }

        private static void LogRequest(HttpContext context)
        {
            Console.WriteLine(context.Response.ContentLength);
        }

        public static void UseGateway(this WebApplication app)
        {
            app.UseRouting();

            //app.UseSession();
            app.UseCookiePolicy();

            //app.UseGatewayEndpoints();
            app.UseYarp();
        }
    }

}

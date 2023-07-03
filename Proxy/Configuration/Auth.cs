using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Identity.Web;



namespace SaaS.Proxy.Configuration
{
    public static class Auth
    {

        public static void AddAuth(this WebApplicationBuilder builder)
        {

            //var initialScopes = builder.Configuration["DownstreamApi:Scopes"]?.Split(' ') ?? builder.Configuration["MicrosoftGraph:Scopes"]?.Split(' ');

            

            // Add services to the container.
            builder.Services.AddAuthentication(OpenIdConnectDefaults.AuthenticationScheme)
                .AddMicrosoftIdentityWebApp(builder.Configuration.GetSection("AzureAd"))
                //.AddMicrosoftIdentityWebApi(builder.Configuration.GetSection("AzureAd"))
                .EnableTokenAcquisitionToCallDownstreamApi()
             //   .AddMicrosoftGraph(builder.Configuration.GetSection("MicrosoftGraph"))
                 .AddInMemoryTokenCaches()
                ;

            builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                //.AddMicrosoftIdentityWebApp(builder.Configuration.GetSection("AzureAd"))
                .AddMicrosoftIdentityWebApi(builder.Configuration.GetSection("AzureAd"))
                .EnableTokenAcquisitionToCallDownstreamApi()
                 //   .AddMicrosoftGraph(builder.Configuration.GetSection("MicrosoftGraph"))
                 .AddInMemoryTokenCaches()
                ;

            builder.Services.AddAuthorization(options =>
            {

                //options.FallbackPolicy = options.DefaultPolicy;
                options.AddPolicy("customPolicy", policy =>
                    { 
                        policy.RequireAuthenticatedUser();
                  });
                options.AddPolicy("tokenPolicy", policy =>
                {
                    policy.AuthenticationSchemes.Add(JwtBearerDefaults.AuthenticationScheme);
                    policy.RequireAuthenticatedUser();
                });

            });

        }
        public static void UseAuth(this WebApplication app)
        {
            //app.UseSession();
            app.UseCookiePolicy();
            //app.UseXsrfCookie();
            
            app.UseRouting();
            
            app.UseAuthentication();
            app.UseAuthorization();

        }
    }
}

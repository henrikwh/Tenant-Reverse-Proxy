using Azure.Identity;
using Microsoft.Extensions.Configuration.AzureAppConfiguration;
using Shared.Models;
using Shared.Services.EventGrid;
using Shared.Services.Token;


namespace SaaS.Proxy.Configuration
{
    public static class ExternalConfigurationStore
    {
        public static void AddExternalConfigurationStore(this WebApplicationBuilder builder)
        {
            string env = String.IsNullOrEmpty(Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT")) ? "Development" : Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Unknown";
            builder.Configuration.AddAzureAppConfiguration(opts =>
            {
                var uami = builder.Configuration.GetSection("AppConfiguration:UserAssignedManagedIdentityClientId").Value ?? "Unknown";
                var tenantId = builder.Configuration.GetSection("Development:ServicePrincipal:TenantId").Value ?? "Unknown";
                var clientId = builder.Configuration.GetSection("Development:ServicePrincipal:ClientId").Value ?? "Unknown";
                var secret = builder.Configuration.GetSection("Development:ServicePrincipal:Secret").Value ?? "Unknown";

                var managedCredential = new ManagedIdentityCredential(uami);
                var credential = new ChainedTokenCredential(managedCredential, new ClientSecretCredential(tenantId, clientId, secret));
                string c = builder.Configuration.GetSection("AppConfiguration:Uri").Value ?? throw new Exception("AppConfiguration not set");
                opts.Connect(new Uri(c), credential).ConfigureKeyVault(opts => opts.SetCredential(credential));
                opts
                          .Select("API:*") //load all settings from the API section
                          .Select("API:*",env) // load all settings from the API section, for the specific environment
                          .Select("AzureAd:*")
                          .Select("ChangeSubscription:*")
                          .Select("ReverseProxy:*",env)
                          .Select("Jwt:*")
                          .Select("TenantDirectory:Tenants", env) // configure the tenants for the environment
                          .ConfigureRefresh(refresh =>
                          {
                              refresh //set refresh for select keys
                                .Register("API:Settings", env, refreshAll: true)
                                .Register("TenantDirectory:Tenants", env, refreshAll: false) 
                                .Register("Jwt:*",  refreshAll: false)
                                .SetCacheExpiration(TimeSpan.FromDays(1));
                          })
                .UseFeatureFlags(featureFlagOptions =>
                {
                    featureFlagOptions.CacheExpirationInterval = TimeSpan.FromDays(1);
                    featureFlagOptions.Select(KeyFilter.Any, LabelFilter.Null).Select(KeyFilter.Any, env);
                });
            }, optional: false);

            
            builder.Services
                .AddAzureAppConfiguration()
                .AddHostedService<EventGridSubscriber>()
                .Configure<ChangeSubscriptionSettings>(builder.Configuration.GetSection("ChangeSubscription"))
                .Configure<ServicePrincipalCredentials>(builder.Configuration.GetSection("Development:ServicePrincipal"))
                .AddSingleton<ITokenService, TokenService>()
                .AddSingleton<IConfigurationRoot>(builder.Configuration)
                ;

            builder.Services.AddOptions();
            
            //    builder.Services.Configure<TenantDirectoryData>(options => builder.Configuration.GetSection("TenantDirectory").Bind(options));
            builder.Services.Configure<TenantSettings>(builder.Configuration.GetSection("TenantDirectory"));

//            builder.Services.Configure<TenantSettings>(options => builder.Configuration.GetSection("TenantDirectory").Bind(options));
            builder.Services.Configure<JwtSettings>(options => builder.Configuration.GetSection("Jwt").Bind(options));
            //var d = builder.Configuration.GetSection("TenantDirectory:Tenants").Get<Tenant[]>();
            //var t = builder.Services.Configure<TenantDirectoryData>(builder.Configuration.GetSection("TenantDirectory:Tenants"));

        }

        public static void UseExternalConfigurationStore(this WebApplication app)
        {
            app.UseAzureAppConfiguration();
        }
    }

}

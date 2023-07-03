using Azure.Identity;

using Microsoft.Extensions.Configuration.AzureAppConfiguration;
using Shared.Models;
using Shared.Services.EventGrid;
using Shared.Services.Token;
using System.Runtime;


namespace WeatherApi.Configuration
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
                .Select("WeatherApi:*")
                .Select("ChangeSubscription:*")
                .Select("Jwt:*")
                          .ConfigureRefresh(refresh =>
                          {
                              refresh
                                .Register("Jwt:*", refreshAll: true)
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
                .AddSingleton<IConfigurationRoot>(builder.Configuration);



            //builder.Services.Configure<TenantDirectoryData>(options => builder.Configuration.GetSection("TenantDirectory").Bind(options));

            //builder.Services.Configure<TenantSettings>(options => builder.Configuration.GetSection("TenantDirectory").Bind(options));

            //var d = builder.Configuration.GetSection("TenantDirectory:Tenants").Get<Tenant[]>();
            //var t = builder.Services.Configure<TenantDirectoryData>(builder.Configuration.GetSection("TenantDirectory:Tenants"));
        }

        public static void UseExternalConfigurationStore(this WebApplication app)
        {
            app.UseAzureAppConfiguration();
        }
    }

}

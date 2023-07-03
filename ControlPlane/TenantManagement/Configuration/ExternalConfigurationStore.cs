using Azure.Identity;
using Microsoft.Extensions.Configuration.AzureAppConfiguration;
using Shared.Models;

namespace ControlPlane.Configuration
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
                .Select("API:*")
                          .Select($"TenantDirectory", env)
                          .ConfigureRefresh(refresh =>
                          {
                              refresh
                                .Register("TenantDirectory", env, refreshAll: true)
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
                .AddSingleton<IConfigurationRoot>(builder.Configuration);
        }

        public static void UseExternalConfigurationStore(this WebApplication app)
        {
            app.UseAzureAppConfiguration();
        }
    }

}

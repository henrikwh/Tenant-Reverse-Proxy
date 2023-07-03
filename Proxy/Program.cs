using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.FeatureManagement.FeatureFilters;
using Microsoft.FeatureManagement;
using Shared.Models;
using SaaS.Proxy.Configuration;
using Microsoft.AspNetCore.ResponseCompression;
using System.Security.Claims;
using SaaS.Proxy.Services;
using Microsoft.Extensions.DependencyInjection;


using System.Reflection;
using Proxy.Services;

using Shared.Repositories;
using Microsoft.Extensions.Caching.Memory;

namespace SaaS.Proxy
{
    public class Program
    {
        
        private static readonly bool UseExternalConfigStore = true;
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);
            string env = String.IsNullOrEmpty(Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT")) ? "Development" : Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Unknown";
            if(env=="Development")
                builder.Configuration.AddJsonFile($"{Environment.CurrentDirectory}/../Scripts/config.json", optional: false, reloadOnChange: true);

            builder.Configuration.AddUserSecrets(Assembly.GetExecutingAssembly(), true);
            

            builder.Services.AddResponseCompression(options =>
            {
                options.EnableForHttps = true;
                options.Providers.Add<BrotliCompressionProvider>();
                options.Providers.Add<GzipCompressionProvider>();
            });

            builder.Services.AddProblemDetails();
            builder.Services.AddControllers();

            builder.Services.AddHttpContextAccessor();
            if(Program.UseExternalConfigStore)
                builder.AddExternalConfigurationStore();
            builder.Services.AddSingleton<IConfiguration>(builder.Configuration);
            builder.Services.AddSingleton<IConfigurationRoot>((IConfigurationRoot)builder.Configuration);

            builder.Logging.AddConfiguration(builder.Configuration.GetSection("Logging"));

            builder.AddApplicationInsightsLogging();
            //builder.Services.AddApplicationInsightsTelemetry();

            builder.Services.AddFeatureManagement().AddFeatureFilter<PercentageFilter>().AddFeatureFilter<TimeWindowFilter>();

            builder.AddAuth();
            builder.AddOpenApi();
            builder.Services.AddSingleton<ITenantRepository, TenantRepository>();
            builder.Services.AddSingleton<IPermissionService, PermissionService>();
            builder.Services.AddSingleton<ITenantDirectoryService, TenantDirectoryService>();

            builder.Services.AddHostedService<TenantRoutingUpdaterService>();

            builder.Services.AddMemoryCache(opts =>
            {


            });

            builder.Services.Configure<ForwardedHeadersOptions>(options =>
            {
                options.ForwardedHeaders =
                    ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
            });

            builder.AddGateway(builder.Services.BuildServiceProvider(), "ReverseProxy");


            var app = builder.Build();
            
            app.UseResponseCompression();
            
            app.UseDeveloperExceptionPage();
            app.UseHttpLogging();
            
            app.UseOpenApi();

            if (Program.UseExternalConfigStore)
                app.UseExternalConfigurationStore();
            
            app.UseHttpsRedirection();

            app.UseAuth();

            app.UseGateway();

            app.MapGet("/", () => "Tenant Proxy");
            //app.MapGet("/proxy/has-user", (ClaimsPrincipal user) => user.Identity.Name)
            //    .RequireAuthorization();
            
            app.UseForwardedHeaders();

            app.MapControllers();

            app.Run();
        }
    }
}
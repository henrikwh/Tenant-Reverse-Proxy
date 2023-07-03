using Microsoft.ApplicationInsights.Extensibility;
using Shared.Middelware.ApplicationInsights.TelemetryInitializers;
using System.Reflection;

namespace SaaS.Proxy.Configuration
{
    public static class ApplicationInsightsLogging
    {
        public static void AddApplicationInsightsLogging(this WebApplicationBuilder builder, string configKey= "API:Settings:Logging")
        {
            

            builder.Services.AddApplicationInsightsTelemetry(opts =>
            {
                opts.ConnectionString = builder.Configuration.GetSection("ApplicationInsights:ConnectionString").Value;
                opts.EnableDependencyTrackingTelemetryModule = bool.Parse(builder.Configuration.GetSection("ApplicationInsights:EnableDependencyTrackingTelemetryModule").Value ?? "true");
                opts.EnablePerformanceCounterCollectionModule = bool.Parse(builder.Configuration.GetSection("ApplicationInsights:EnablePerformanceCounterCollectionModule").Value ?? "false");
                opts.EnableAdaptiveSampling = bool.Parse(builder.Configuration.GetSection("ApplicationInsights:EnableAdaptiveSampling").Value ?? "true");
                opts.EnableHeartbeat = bool.Parse(builder.Configuration.GetSection("ApplicationInsights:EnableHeartbeat").Value ?? "false");
                opts.EnableAppServicesHeartbeatTelemetryModule = bool.Parse(builder.Configuration.GetSection("ApplicationInsights:EnableAppServicesHeartbeatTelemetryModule").Value ?? "false");
                opts.EnableRequestTrackingTelemetryModule = bool.Parse(builder.Configuration.GetSection("ApplicationInsights:EnableRequestTrackingTelemetryModule").Value ?? "true");
                opts.DeveloperMode = bool.Parse(builder.Configuration.GetSection("ApplicationInsights:DeveloperMode").Value ?? "false");
                
            });
            builder.Services.AddSingleton<ITelemetryInitializer, DimensionTagsTelemetryInitializer>();
            builder.Services.AddSingleton<ITelemetryInitializer, MetricTagsTelemetryInitializer>();
            
        }

    
    }
}

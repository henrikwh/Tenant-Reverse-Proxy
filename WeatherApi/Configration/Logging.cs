using System.Reflection;

namespace WeatherApi.Configuration
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
                opts.EnableHeartbeat = bool.Parse(builder.Configuration.GetSection("ApplicationInsights:EnableHeartbeat").Value ?? "true");
                opts.EnableAppServicesHeartbeatTelemetryModule = bool.Parse(builder.Configuration.GetSection("ApplicationInsights:EnableAppServicesHeartbeatTelemetryModule").Value ?? "true");
                opts.EnableRequestTrackingTelemetryModule = bool.Parse(builder.Configuration.GetSection("ApplicationInsights:EnableRequestTrackingTelemetryModule").Value ?? "true");
                opts.DeveloperMode = bool.Parse(builder.Configuration.GetSection("ApplicationInsights:DeveloperMode").Value ?? "true");
            });

        }

        //public static void UseLogging(this WebApplication app)
        //{

        //}
    }
}

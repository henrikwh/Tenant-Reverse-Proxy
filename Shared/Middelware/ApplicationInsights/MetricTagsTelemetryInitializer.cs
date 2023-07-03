using Microsoft.ApplicationInsights.Channel;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.ApplicationInsights.Extensibility;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shared.Middelware.ApplicationInsights.TelemetryInitializers
{
    public class MetricTagsTelemetryInitializer : ITelemetryInitializer
    {
        /*To add to telemety customdimensions use:
         * Activity.Current?.AddTag("m-TagName", "TagValue");
         */
        public void Initialize(ITelemetry telemetry)
        {
            var activity = Activity.Current;
            if (telemetry is ISupportMetrics requestTelemetry)
            {
                if (activity == null)
                    return;

                foreach (var tag in activity.Tags)
                {
                    if (tag.Key.StartsWith("m-"))
                        requestTelemetry.Metrics[tag.Key.Remove(0, 2)] = double.Parse(tag.Value ?? "0");
                }
            }
        }
    }
}

using Microsoft.ApplicationInsights.Channel;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.ApplicationInsights.Extensibility;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection.Metadata.Ecma335;
using System.Text;
using System.Threading.Tasks;

namespace Shared.Middelware.ApplicationInsights.TelemetryInitializers
{
    public class DimensionTagsTelemetryInitializer : ITelemetryInitializer
    {
        /*To add to telemety customdimensions use:
         * Activity.Current?.AddTag("d-TagName", "TagValue");
         */
        public void Initialize(ITelemetry telemetry)
        {
            var activity = Activity.Current;
            
            if (telemetry is ISupportProperties requestTelemetry)
            {
                if (activity == null) return;

                foreach (var tag in activity.Tags)
                {
                    if (tag.Key.StartsWith("d-"))
                        requestTelemetry.Properties[tag.Key.Remove(0, 2)] = tag.Value;
                }
            }
        }
    }
}

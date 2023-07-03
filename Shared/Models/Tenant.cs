using System.Text.Json.Serialization;

namespace Shared.Models
{

    public enum TenantState
    {
        Enabled,
        Disabled,
    }
    public class Tenant
    {
        public string Tid { get; set; }  = string.Empty;
        public string Alias { get; set; } = string.Empty;
        public string Destination { get; set; } = string.Empty;
        [JsonConverter(typeof(JsonStringEnumConverter))]
        public TenantState State { get; set; } = TenantState.Enabled;
    }



    public class TenantSettings
    {
        public List<Tenant> Tenants { get; set; } = new List<Tenant>();
    }

}

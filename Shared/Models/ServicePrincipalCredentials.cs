namespace Shared.Models
{
    public class ServicePrincipalCredentials
    {
        public string TenantId { get; set; } = string.Empty;
        public string ClientId { get; set; } = string.Empty;
        public string Secret { get; set; } = string.Empty;
    }
}
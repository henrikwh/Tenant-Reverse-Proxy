using Shared.Models;

namespace SaaS.Proxy.Services
{
    public interface ITenantDirectoryService
    {
        //public bool IsKnownTenant(string tenantId);
        //public List<Tenant> GetTenants();
        //public Tenant TenantLookup(string tenantId);
        public void AddTenantsToRouting(List<Tenant> tenant);
    }
}

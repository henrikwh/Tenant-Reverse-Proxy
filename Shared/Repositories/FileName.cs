using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Shared.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Shared.Repositories
{
    public interface ITenantRepository
    {
        List<Tenant> GetAllTenants();
        Tenant GetTenant(string tenantId);
    }
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum ResponseCodes{
        TenantNotFound,
        TenantDeleted,
        TenantCreated,
        TenantUpdated,
        TenantIsActive,
    }
    
    public record TenantRepositoryResponse(ResponseCodes ResponseCode, Dictionary<string,string>? Properties= null);
    public class TenantRepository : ITenantRepository
    {
        private readonly ILogger<TenantRepository> _logger;
        private readonly IOptionsMonitor<TenantSettings> _repo;

        public TenantRepository(IOptionsMonitor<TenantSettings> tenants, ILogger<TenantRepository> logger)
        {
            _logger = logger;
            _repo = tenants;
        }
        public List<Tenant> GetAllTenants()
        {
            return _repo.CurrentValue.Tenants;
        }

        public Tenant GetTenant(string tenantId)
        {
            var r = _repo.CurrentValue.Tenants.Where(w => w.Tid == tenantId).FirstOrDefault();
            return r ?? throw new Exception("Tenant does not exist");
        }
    }
}

using Azure.Data.AppConfiguration;
using Azure.Identity;
using Microsoft.AspNetCore.Authentication;
using Shared.Models;
using Shared.Repositories;
using Shared.Services.Environment;
using System.Security.Cryptography;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;

namespace TenantManagement.Repositories
{

    public interface ITenantRepositoryReadWrite : Shared.Repositories.ITenantRepository
    {
        TenantRepositoryResponse UpdateTenant(Tenant t);
        TenantRepositoryResponse CreateTenant(Tenant t);
        TenantRepositoryResponse DeleteTenant(string tenantId);
    }
    
    public class TenantRepositoryReadWrite : ITenantRepository, ITenantRepositoryReadWrite
    {



        private readonly ConfigurationClient configurationClient;
        private readonly string _env;
        private IConfiguration _config;
        private static readonly string tenantDirectoryKey = "TenantDirectory:Tenants";
        public TenantRepositoryReadWrite(IConfiguration config, IEnvironmentService environment)
        {
            _env = environment.GetEnvironmentName();
            _config = config;
            string endpoint = config.GetValue<string>("AppConfiguration:Uri") ?? "";

            var uami = config.GetSection("AppConfiguration:UserAssignedManagedIdentityClientId").Value ?? "Unknown";
            var tenantId = config.GetSection("AppConfiguration:TenantId").Value ?? "Unknown";
            var clientId = config.GetSection("AppConfiguration:ClientId").Value ?? "Unknown";
            var secret = config.GetSection("AppConfiguration:Secret").Value ?? "Unknown";

            var managedCredential = new ManagedIdentityCredential(uami);
            var credential = new ChainedTokenCredential(managedCredential, new ClientSecretCredential(tenantId, clientId, secret));
            
            configurationClient = new ConfigurationClient(new Uri(endpoint), credential);
            
        }

        public List<Tenant> GetAllTenants()
        {
            var r = configurationClient.GetConfigurationSetting(tenantDirectoryKey, _env);
            var json = JsonNode.Parse(r.Value.Value)!;

            List<Tenant>? res = JsonSerializer.Deserialize<List<Tenant>>(json);
            return res!;

        }

        public Tenant GetTenant(string tenantId)
        {
            var r = configurationClient.GetConfigurationSetting(tenantDirectoryKey, _env);
            var json = JsonNode.Parse(r.Value.Value)!;

            List<Tenant>? res =
           JsonSerializer.Deserialize<List<Tenant>>(json);
            return res!.Where(w => w.Tid == tenantId).FirstOrDefault()!; ;
        }

        public TenantRepositoryResponse UpdateTenant(Tenant t)
        {
            var allTenants = GetAllTenants();
            var old = allTenants.Where(w => w.Tid == t.Tid).First();
            allTenants.Remove(old);
            allTenants.Add(t);
            string jsonString = JsonSerializer.Serialize(allTenants);

            
            ConfigurationSetting setting = new ConfigurationSetting(tenantDirectoryKey, jsonString, _env) {  ContentType = "application/json"};
            
            var resp = configurationClient.SetConfigurationSetting(setting);
            Dictionary<string, string> d = new();
            d.Add("status", resp.GetRawResponse().Status.ToString());
            d.Add("isError", resp.GetRawResponse().IsError.ToString());
            return new TenantRepositoryResponse(ResponseCodes.TenantUpdated,d);
        }
        public TenantRepositoryResponse CreateTenant(Tenant t)
        {
            var allTenants = GetAllTenants();
            allTenants.Add(t);
            string jsonString = JsonSerializer.Serialize(allTenants);

            ConfigurationSetting setting = new ConfigurationSetting(tenantDirectoryKey, jsonString, _env) { ContentType = "application/json" };
            configurationClient.SetConfigurationSetting(setting);
            return new TenantRepositoryResponse(ResponseCodes.TenantCreated);
        }

        public TenantRepositoryResponse DeleteTenant(string tenantId)
        {
            
            var allTenants = GetAllTenants();
            if(!allTenants.Any(w=>w.Tid == tenantId))
                return new TenantRepositoryResponse(ResponseCodes.TenantNotFound);
            if (allTenants.Where(w => w.Tid == tenantId).First().State == TenantState.Enabled)
                return new TenantRepositoryResponse(ResponseCodes.TenantIsActive);
            string jsonString = JsonSerializer.Serialize(allTenants.Where(w=>w.Tid!=tenantId));
            configurationClient.SetConfigurationSetting(tenantDirectoryKey, jsonString, _env);

            return new TenantRepositoryResponse(ResponseCodes.TenantDeleted);
        }


    }
}

using Asp.Versioning;
using Azure.Data.AppConfiguration;
using Azure.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.OpenApi.Validations.Rules;
using Shared.Models;
using Shared.Repositories;
using System.ComponentModel;
using TenantManagement.Repositories;

namespace TenantManagement.Controllers
{
    [ApiController]
    [ApiVersion("1.0")]
    [ApiVersion("1.0-debug")]

[Route("api/v{version:apiVersion}/[controller]")]

    public class TenantManagerController : ControllerBase
    {
        private readonly IConfigurationRoot _root;
        private readonly ITenantRepositoryReadWrite _repo;
        
        private readonly ILogger<TenantManagerController> _logger;

        public TenantManagerController(ILogger<TenantManagerController> logger, ITenantRepositoryReadWrite repo, IConfigurationRoot root)
        {
            _root = root;
            _repo = repo;
            _logger = logger;
        }
        [MapToApiVersion("1.0-debug")]
        [HttpGet("GetConfigDump")]
        public string GetConfig()
        {
            return _root.GetDebugView();
        }
        
        [HttpGet("Tenant")]
        [Produces("application/json")]
        [ProducesResponseType(typeof(Tenant), 200)]
        [ProducesResponseType(404)]
        [MapToApiVersion("1.0")]
        public IActionResult GetTenant(string id)
        {
            var r = _repo.GetTenant(id);
            return Ok(r);
        }
        [HttpGet("Tenants")]
        [Produces("application/json")]
        [ProducesResponseType(typeof(List<Tenant>), 200)]
        [ProducesResponseType(404)]
        [MapToApiVersion("1.0")]
        public IActionResult GetTenants()
        {
            var r = _repo.GetAllTenants();
            return Ok(r);
        }

        [HttpPatch("Tenant")]
        [Produces("application/json")]
        [ProducesResponseType(typeof(TenantRepositoryResponse), 200)]

        [ProducesResponseType(404)]
        [MapToApiVersion("1.0")]
        public IActionResult PatchTenant(Tenant t)
        {
            var resp = _repo.UpdateTenant(t);
            return Ok(resp);
        }


        /// <summary>
        /// Create a new tenant
        /// </summary>

        [HttpPost("CreateTenant")]
        [Produces("application/json")]
        [ProducesResponseType(typeof(TenantRepositoryResponse),200)]

        [ProducesResponseType(404)]
        [MapToApiVersion("1.0")]

        public IActionResult CreateTenant(Tenant t)
        {
            var resp = _repo.CreateTenant(t);
            return Ok(resp);
        }
        /// <summary>
        /// Deprovisions a tenant
        /// </summary>
        /// /<param name="tenantId">Id of the tenant to be deprovisioned</param>
        /// <returns>Ok</returns>
        [HttpDelete("Tenant")]
        [Produces("application/json")]
        [ProducesResponseType(typeof(TenantRepositoryResponse), 200)]
        [ProducesResponseType(404)]
        [MapToApiVersion("1.0")]
        public IActionResult DeleteTenant(string tenantId)
        {
            var resp = _repo.DeleteTenant(tenantId);
            return Ok(resp);
        }

        /// <summary>
        /// Deprovisions a tenant
        /// </summary>
        /// /<param name="tenantId">Id of the tenant to be deprovisioned</param>
        /// <returns>Ok</returns>
        [HttpPatch("DisableTenant")]
        [Produces("application/json")]
        [ProducesResponseType(typeof(TenantRepositoryResponse), 200)]
        [ProducesResponseType(404)]
        [MapToApiVersion("1.0")]
        public IActionResult DisableTenant(string tenantId)
        {
            var resp = _repo.GetTenant(tenantId);
            resp.State = TenantState.Disabled;
            var result= _repo.UpdateTenant(resp);
            return Ok(result);
        }

        [HttpPatch("EnableTenant")]
        [Produces("application/json")]
        [ProducesResponseType(typeof(TenantRepositoryResponse), 200)]
        [ProducesResponseType(404)]
        [MapToApiVersion("1.0")]
        public IActionResult EnableTenant(string tenantId)
        {
            var resp = _repo.GetTenant(tenantId);
            resp.State = TenantState.Enabled ;
            var result = _repo.UpdateTenant(resp);
            return Ok(result);
        }

    }
}
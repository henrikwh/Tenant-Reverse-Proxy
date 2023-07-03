namespace Proxy.Services
{
    public interface IPermissionService
    {
        Dictionary<string, object> GetPermissions(string tenantId);
    }

    public class PermissionService : IPermissionService
    {
        private readonly ILogger<PermissionService> _logger;

        public PermissionService(ILogger<PermissionService> logger)
        {
            _logger = logger;
            _permissions = new Dictionary<string, object> {
                {"SomePermision","Limited" }
            };
        }
        private Dictionary<string, object> _permissions { get; set; }

        public Dictionary<string, object> GetPermissions(string tenantId)
        {
            return _permissions;
        }
    }
}

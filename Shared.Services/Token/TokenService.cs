using Azure.Core;
using Azure.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Shared.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shared.Services.Token
{
    public class TokenService : ITokenService
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<TokenService> _logger;
        private readonly IOptions<ServicePrincipalCredentials> _sp;

        public TokenService(IConfiguration conf, IOptions<ServicePrincipalCredentials> sp, ILogger<TokenService> logger)
        {
            _configuration = conf;
            _logger = logger;
            IConfigurationRoot cr = (IConfigurationRoot)conf;
            _sp = sp;
        }
        public TokenCredential GetTokenCredential()
        {
            _logger.LogInformation($"Getting token for: {_sp.Value.ClientId}");
            if (string.IsNullOrEmpty(_sp.Value.TenantId))
                return new ClientSecretCredential(Guid.NewGuid().ToString(), _sp.Value.ClientId, _sp.Value.Secret ?? "NOT SET"); ; //not running in production. Tenant id is not set.
            return new ClientSecretCredential(_sp.Value.TenantId,_sp.Value.ClientId, _sp.Value.Secret ?? "NOT SET");
        }
    }
    public interface ITokenService
    {
        TokenCredential GetTokenCredential();
    }
}

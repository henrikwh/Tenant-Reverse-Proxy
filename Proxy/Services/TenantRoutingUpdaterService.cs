using Microsoft.Extensions.Options;

using SaaS.Proxy.Services;
using Shared.Models;

namespace Proxy.Services
{
    public class TenantRoutingUpdaterService : IHostedService
    {
        private IDisposable _optionsChangedListener;
        //IOptionsMonitor<TenantSettings>
        private TenantSettings _myCurrentOptions;
        private readonly IOptionsMonitor<TenantSettings> _options;
        private readonly ITenantDirectoryService _tds;
        private readonly ILogger<TenantRoutingUpdaterService> _logger;

        public TenantRoutingUpdaterService(IOptionsMonitor<TenantSettings> optionsMonitor, ITenantDirectoryService tds, ILogger<TenantRoutingUpdaterService> logger)
        {
            _optionsChangedListener = optionsMonitor.OnChange(OptionsChanged!)!;
            _myCurrentOptions = optionsMonitor.CurrentValue;
            _options = optionsMonitor;
            _tds = tds;
            _logger = logger;
        }

        private void OptionsChanged(TenantSettings newOptions, string arg2)
        {
            _myCurrentOptions = newOptions;
            
            _tds.AddTenantsToRouting(newOptions.Tenants!);
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            _optionsChangedListener = _options.OnChange(OptionsChanged!)!;
            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
            //throw new NotImplementedException();
        }



        //protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        //{
        //    while (!stoppingToken.IsCancellationRequested)
        //    {
        //        //Console.WriteLine(_myCurrentOptions.Tenants.Count);
        //        await Task.Delay(10000, stoppingToken);
        //    }
        //}

        //public override void Dispose()
        //{
        //    _optionsChangedListener.Dispose();
        //    base.Dispose();
        //}
    }
}

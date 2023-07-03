using Azure.Identity;
using Azure.Messaging.ServiceBus;
using Azure.Messaging.ServiceBus.Administration;
using Microsoft.AspNetCore.Mvc.Diagnostics;

using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Shared.Models;
using Shared.Services.Token;
using System.Text.Json;
using Azure.Messaging.EventGrid;
using Microsoft.Extensions.Configuration.AzureAppConfiguration.Extensions;
using Microsoft.Extensions.Configuration.AzureAppConfiguration;
using Microsoft.Extensions.Configuration;
using Yarp.ReverseProxy.Configuration;
using System.Collections.Generic;
using System;

using Azure.Data.AppConfiguration;

namespace Shared.Services.EventGrid
{
    public class EventGridSubscriber : IHostedService
    {
        private readonly IOptions<ChangeSubscriptionSettings> _changeSubscriptionSettings;
        private readonly ILogger<EventGridSubscriber> _logger;
        private readonly ITokenService _tokenService;
        private readonly IConfigurationRefresher? _refresher;
        private readonly IConfigurationRoot _configurationRoot;
        //private readonly IEnumerable<IModelProcessor> _processors;


        public EventGridSubscriber(IOptions<ChangeSubscriptionSettings> ChangeSubscriptionSettings, ITokenService tokenService, IConfigurationRefresherProvider refreshProvider, IConfigurationRoot configuration, ILogger<EventGridSubscriber> logger)
        {
            _changeSubscriptionSettings = ChangeSubscriptionSettings;
            _logger = logger;
            _tokenService = tokenService;
            _refresher = refreshProvider.Refreshers.FirstOrDefault();
            _configurationRoot = configuration;
//            _processors = p;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {

            _logger.LogInformation("Starting eventsubscription");
            ConfigurationChangeSubscriber().GetAwaiter();
            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }
        private ServiceBusClient GetServiceBusClient()
        {
            var clientOptions = new ServiceBusClientOptions() { TransportType = ServiceBusTransportType.AmqpWebSockets };
            ServiceBusClient client;

            var managedCredential = new ManagedIdentityCredential(_changeSubscriptionSettings.Value.UserAssignedManagedIdentityClientId);
            var t = _tokenService.GetTokenCredential();
  //          _logger.LogInformation($"Using MI for EventGridsubscriber, GetServiceBusClient: {_changeSubscriptionSettings.Value.UserAssignedManagedIdentityClientId ?? "NONE"}");
            var credential = new ChainedTokenCredential(managedCredential, t);
            client = new ServiceBusClient(_changeSubscriptionSettings.Value.ServiceBusNamespace, credential);
            return client;
        }
        private ServiceBusAdministrationClient GetServiceBusAdminClient()
        {
            ServiceBusAdministrationClient client;
            var managedCredential = new ManagedIdentityCredential(_changeSubscriptionSettings.Value.UserAssignedManagedIdentityClientId);
// _logger.LogInformation($"Using MI for EventGridsubscriber, GetServiceBusAdminClient: {_changeSubscriptionSettings.Value.UserAssignedManagedIdentityClientId ?? "NONE"}");
            var credential = new ChainedTokenCredential(managedCredential, _tokenService.GetTokenCredential());

            client = new ServiceBusAdministrationClient(_changeSubscriptionSettings.Value.ServiceBusNamespace, credential);
            return client;
        }
        private async Task ConfigurationChangeSubscriber()
        {
            try
            {
                var client = GetServiceBusAdminClient();
                if (!client.SubscriptionExistsAsync(_changeSubscriptionSettings.Value.ServiceBusTopic, _changeSubscriptionSettings.Value.ServiceBusSubscriptionPrefix).Result)
                {
                    var so = new CreateSubscriptionOptions(_changeSubscriptionSettings.Value.ServiceBusTopic, _changeSubscriptionSettings.Value.ServiceBusSubscriptionPrefix);
                    so.AutoDeleteOnIdle = TimeSpan.FromHours(_changeSubscriptionSettings.Value.AutoDeleteOnIdleInHours);
                    await client.CreateSubscriptionAsync(so);
                    _logger.LogInformation("Change subscription created");
                }

                var servicebusClient = GetServiceBusClient();
                var processor = servicebusClient.CreateProcessor(_changeSubscriptionSettings.Value.ServiceBusTopic, _changeSubscriptionSettings.Value.ServiceBusSubscriptionPrefix, new ServiceBusProcessorOptions() { });

                processor.ProcessMessageAsync += MessageHandler;

                processor.ProcessErrorAsync += Processor_ProcessErrorAsync;
                //+= ErrorHandler;

                await processor.StartProcessingAsync();
            }
            catch (Exception ex) when (ex.Message.Contains("already exists"))
            {
                _logger.LogTrace(ex, ex.Message);
            }
            catch (Exception e)
            {
                _logger.LogError("Error registering subscription: " + _changeSubscriptionSettings.Value.ServiceBusTopic);
                _logger.LogError(e, e.Message);
                throw;
            }
        }

        private Task Processor_ProcessErrorAsync(ProcessErrorEventArgs arg)
        {
            _logger.LogError(arg.Exception, "Error subscribing to topic");
            throw new Exception("what!");
        }

        private record EventData(string ObjectType, string VaultName, string ObjectName);
        private async Task MessageHandler(ProcessMessageEventArgs args)
        {
            try
            {
                EventGridEvent eventGridEvent = EventGridEvent.Parse(BinaryData.FromBytes(args.Message.Body));
                _logger.LogTrace($"Received: {eventGridEvent.Data}");

                eventGridEvent.TryCreatePushNotification(out PushNotification pushNotification);

                var d = System.Text.Json.JsonSerializer.Deserialize<EventData>(eventGridEvent.Data, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                
                if (!string.IsNullOrEmpty(d?.ObjectName) && (d.ObjectType.ToLower() == "secret" || d.ObjectType.ToLower() == "certificate"))
                {
                        if (eventGridEvent.EventType == "Microsoft.KeyVault.SecretNewVersionCreated")
                        {
                            _logger.LogTrace($"Refreshing all, triggered by secret: " + eventGridEvent.Subject);
                            _configurationRoot.Reload();
                        }
                    await args.CompleteMessageAsync(args.Message);
                }
                else if (pushNotification != null)
                {
                    string env = String.IsNullOrEmpty(System.Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT")) ? "Development" : System.Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Unknown";
                    var data = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, string>>(eventGridEvent.Data);

                    data!.TryGetValue("label", out string? label);
                    if (label != env && !string.IsNullOrEmpty(label))
                    {
                        await args.CompleteMessageAsync(args.Message);
                        return;
                    }
                    
                    //_refresher.ProcessPushNotification(pushNotification, TimeSpan.FromSeconds(5));
                    _refresher!.ProcessPushNotification(pushNotification);


                    await args.CompleteMessageAsync(args.Message);
                }
                else
                    throw new Exception("Unknown message");
            }
            catch (Exception e)
            {
                _logger.LogError(e, e.Message);
                throw;
            }
        }


    }
}

namespace Shared.Models
{
    public class ChangeSubscriptionSettings
    {
        public string? ServiceBusConnectionString { get; set; }

        public string? ServiceBusTopic { get; set; }
        string? _serviceBusSubscription;
        public string? ServiceBusSubscriptionPrefix
        {
            get { return _serviceBusSubscription; }
            set { _serviceBusSubscription = $"{value}-{Environment.MachineName.ToString()}"; }
        }
        public int AutoDeleteOnIdleInHours { get; set; }
        private string? _serviceBusNamespace;
        public string? ServiceBusNamespace
        {
            get { return _serviceBusNamespace; }
            set
            {
                this._serviceBusNamespace = value?.Replace(@"https://", "").Replace(@":443/", "") + ".servicebus.windows.net";
                
            }
        }
        public int? MaxDelayBeforeCacheIsMarkedDirtyInSeconds { get; set; }
        public string? UserAssignedManagedIdentityClientId { get; set; }
    }
}
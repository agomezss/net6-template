namespace NET6_Template.Broker
{
    public static class BrokerModels
    {
        public const string QUEUE_ENDPOINT = "queueEndPoint";
        public const string QUEUE_EXCHANGE_NAME = "exchangeName";
        public const string QUEUE_ROUTING_KEY = "routingKey";
        public const string QUEUE_NETWORK_RECOVERY_INTERVAL = "networkRecoveryInterval";
        public const string QUEUE_NETWORK_REQUESTED_HEARTBEAT = "requestedHeartbeat";
        public const string QUEUE_WAIT_FOR_CONFIRMS = "WaitForConfirms";
        public const string QUEUE_ROUTING_KEY_DISTRIBUTION = "routingKeyDistribution";
        public const string QUEUE_REPLICATION_THREADS = "queueReplicationThreads";
        public const string BROKER_TYPE_SQS_DETERMINATOR = "https://sqs.";
    }

    public enum BrokerType
    {
        RabbitMQ = 1,
        AWSSQS = 2
    }
}

using log4net;
using RabbitMQ.Client.Events;
using System;

namespace NET6_Template.Broker
{
    public static class BrokerConnector
    {
        public static bool HasConnection { get { return _brokerType == BrokerType.RabbitMQ ? RabbitConnector.HasConnection : SQSConnector.HasConnection; } }
        public static bool ConnectionIsOpen { get { return HasConnection && _brokerType == BrokerType.RabbitMQ ? RabbitConnector.ConnectionIsOpen : SQSConnector.HasConnection; } }
        public static int TotalSessions { get { return _brokerType == BrokerType.RabbitMQ ? RabbitConnector.TotalSessions : SQSConnector.TotalSessions; } }
        public static int Connections { get { return _brokerType == BrokerType.RabbitMQ ? RabbitConnector.Connections : SQSConnector.Connections; } }

        static BrokerType _brokerType;
        private const string BROKER_TYPE_SQS_DETERMINATOR = "https://sqs.";

        public static void Init(string queueEndpoint)
        {
            _brokerType = queueEndpoint.Contains(BROKER_TYPE_SQS_DETERMINATOR) ? BrokerType.AWSSQS : BrokerType.RabbitMQ;

            if (_brokerType == BrokerType.RabbitMQ)
                RabbitConnector.Init(queueEndpoint, false);
            else if (_brokerType == BrokerType.AWSSQS)
                SQSConnector.Init(queueEndpoint);
        }

        public static void Close()
        {
            if (_brokerType == BrokerType.RabbitMQ)
                RabbitConnector.Close();
            else if (_brokerType == BrokerType.AWSSQS)
                SQSConnector.Close();
        }

        public static BrokerContext GetModel(BrokerContext context)
        {
            context.channel = _brokerType == BrokerType.RabbitMQ ? RabbitConnector.GetModel(context.logger) : null;
            return context;
        }

        public static BrokerContext GetControlChannel(string controlQueue, bool durable, bool exclusive, bool autoDelete, ILog logger)
        {
            var context = new BrokerContext(controlQueue, logger, _brokerType)
            {
                channel = _brokerType == BrokerType.RabbitMQ ? RabbitConnector.GetControlChannel(controlQueue, durable, exclusive, autoDelete, logger) : null
            };

            return context;
        }

        public static string TryGetIdFromHeader(BrokerContext context, string fallbackId)
        {
            if (_brokerType == BrokerType.RabbitMQ)
                return RabbitConnector.TryGetIdFromHeader(context.eventArgs, fallbackId);

            return fallbackId;
        }

        public static BrokerContext CreateConsumer(BrokerContext context)
        {
            context.consumer = _brokerType == BrokerType.RabbitMQ ? RabbitConnector.CreateConsumer(context.channel) : null;
            return context;
        }

        public static void StartConsume(BrokerContext context)
        {
            context.IsActive = true;

            if (_brokerType == BrokerType.RabbitMQ)
                RabbitConnector.StartConsume(context.channel, context.consumer, context.queue);
            else if (_brokerType == BrokerType.AWSSQS)
                SQSConnector.StartConsume(context);
        }

        public static void TryCancelConsumiption(BrokerContext context)
        {
            context.IsActive = false;

            if (_brokerType == BrokerType.RabbitMQ)
                RabbitConnector.TryCancelAllConsumerTags(context.channel, context.consumer, context.logger);
            else if (_brokerType == BrokerType.AWSSQS)
                SQSConnector.TryCancelAllConsumerTags(context);
        }

        public static void TryAckMessage(BrokerContext context, BasicDeliverEventArgs deliveryEventArgs)
        {
            if (_brokerType == BrokerType.RabbitMQ)
                RabbitConnector.TryAckMessage(context.channel, deliveryEventArgs, context.logger);
            else if (_brokerType == BrokerType.AWSSQS)
                SQSConnector.TryAckMessage(context.sqsmessage, context.queue, context.logger);
        }

        public static void TryPublishMessage(BrokerContext context, string message, string queue)
        {
            if (_brokerType == BrokerType.RabbitMQ)
                throw new NotImplementedException();
            else if (_brokerType == BrokerType.AWSSQS)
                SQSConnector.TryPublishMessage(message, queue, context.logger);
        }

        public static void TryNackAndRequeueMessage(BrokerContext context, BasicDeliverEventArgs deliveryEventArgs)
        {
            if (_brokerType == BrokerType.RabbitMQ)
                RabbitConnector.TryNackAndRequeueMessage(context.channel, deliveryEventArgs, context.logger);
            else if (_brokerType == BrokerType.AWSSQS)
                SQSConnector.TryNackAndRequeueMessage(context.sqsmessage, context.queue, context.logger);
        }

        public static void CloseChannel(BrokerContext context)
        {
            context.IsActive = false;

            if (_brokerType == BrokerType.RabbitMQ)
                RabbitConnector.CloseChannel(context.channel);
        }

        public static bool IsConnected()
        {
            if (_brokerType == BrokerType.RabbitMQ)
                return RabbitConnector.IsConnected();
            else if (_brokerType == BrokerType.AWSSQS)
                return SQSConnector.IsConnected();

            return false;
        }
    }
}

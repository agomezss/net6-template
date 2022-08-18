using log4net;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System;
using System.Text;
using System.Threading;

namespace NET6_Template.Broker
{
    public class BrokerContext
    {
        internal bool IsActive;
        internal BrokerType brokerType;
        internal string queue;
        internal IModel channel;
        internal ILog logger;
        internal EventingBasicConsumer consumer;
        internal Amazon.SQS.Model.Message sqsmessage;
        internal BasicDeliverEventArgs eventArgs;
        public event EventHandler<BasicDeliverEventArgs> Received;
        public bool ContextIsDisconnected { get { return channel == null || channel.IsClosed; } }

        private Timer timerRefreshVisibility;

        public BrokerContext(string queueName, ILog loggerMgr, BrokerType broker)
        {
            brokerType = broker;
            queue = queueName;
            logger = loggerMgr;
        }

        public string GetMessage(BasicDeliverEventArgs args)
        {
            if (brokerType == BrokerType.AWSSQS)
            {
                try
                {
                    return Util.ParseCompressedOrUncompressedMessage(Encoding.UTF8.GetBytes((sqsmessage.Body)), true);
                }
                catch (Exception ex)
                {
                    logger.Error($"GetMessage [SQS] - ParseMessage Error - ex:{ex.Message}, message:{sqsmessage.Body}");
                }
            }

            if (brokerType == BrokerType.RabbitMQ)
            {
                try
                {
                    eventArgs = args;
                    var body = eventArgs.Body.ToArray();
                    return Util.ParseCompressedOrUncompressedMessage(body);
                }
                catch (Exception ex)
                {
                    logger.Error($"GetMessage [RabbitMQ] - ParseMessage Error - ex:{ex.Message}, message:{Encoding.UTF8.GetString(eventArgs.Body.ToArray())}");
                }
            }

            return null;
        }

        public string TryGetMessageReceipt()
        {
            try
            {
                return sqsmessage?.ReceiptHandle;
            }
            catch
            {
                return null;
            }
        }

        public byte[] GetMessageBytes(BasicDeliverEventArgs args)
        {
            if (brokerType == BrokerType.AWSSQS)
                return Encoding.UTF8.GetBytes(sqsmessage.Body);

            if (brokerType == BrokerType.RabbitMQ)
            {
                eventArgs = args;
                return eventArgs.Body.ToArray();
            }

            return null;
        }

        public void HandleSQSMessage(Amazon.SQS.Model.Message msg, int refreshVisibilitySeconds = 60)
        {
            sqsmessage = msg;

            if (Received != null)
            {
                timerRefreshVisibility = new Timer(new TimerCallback(SQSConnector.TryChangeMessageVisibility), this, TimeSpan.FromSeconds(refreshVisibilitySeconds), TimeSpan.FromSeconds(refreshVisibilitySeconds));

                Received.Invoke(null, null);

                TryClearTimerRefresh();
            }
        }

        public void TryClearTimerRefresh()
        {
            try
            {
                if (timerRefreshVisibility != null)
                {
                    timerRefreshVisibility?.Change(Timeout.Infinite, Timeout.Infinite);
                    timerRefreshVisibility?.Dispose();
                }
            }
            catch
            {

            }
            finally
            {
                sqsmessage = null;
            }
        }

        public void SetReceivedEvent(EventHandler<BasicDeliverEventArgs> eventHandler)
        {
            if (brokerType == BrokerType.RabbitMQ)
                consumer.Received += eventHandler;
            else if (brokerType == BrokerType.AWSSQS)
                Received += eventHandler;
        }
    }
}

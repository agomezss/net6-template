using log4net;
using NET6_Template.Broker;
using System;
using System.Text.Json;

namespace NET6_Template
{
    public class SampleConsumerBroker : BaseConsumerBroker
    {
        public SampleConsumerBroker(ILog loggerObj) : base()
        {
            logger = loggerObj;
        }

        public override void ConsumeQueue(string queueName)
        {
            try
            {
                _brokerContext = BrokerConnector.GetControlChannel(queueName, true, false, false, logger);
                _brokerContext = BrokerConnector.CreateConsumer(_brokerContext);

                _brokerContext.SetReceivedEvent((model, deliveryEventArgs) =>
                {
                    var message = _brokerContext.GetMessage(deliveryEventArgs);
                    var (stopConsuming, ack) = HandleMessageWorkingQueue(message);

                    if (ack)
                        BrokerConnector.TryAckMessage(_brokerContext, deliveryEventArgs);
                    else
                        BrokerConnector.TryNackAndRequeueMessage(_brokerContext, deliveryEventArgs);

                    if (stopConsuming)
                    {
                        BrokerConnector.TryCancelConsumiption(_brokerContext);
                        return;
                    }
                });

                logger.Info("Listening...");
                BrokerConnector.StartConsume(_brokerContext);
            }
            catch (Exception ex)
            {
                logger.Error(ex.StackTrace, ex);
            }
        }

        private (bool stop, bool ack) HandleMessageWorkingQueue(string msg)
        {
            try
            {
                var message = JsonSerializer.Deserialize<BrokerMessage>(msg);
            }
            catch (Exception ex)
            {
                logger.Error("[Broker] [WorkingMessage] [Exception] [StackTrace=" + ex.StackTrace + "]", ex);
                return (false, true);
            }

            return (false, true);
        }
    }
}

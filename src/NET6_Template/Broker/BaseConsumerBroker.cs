using log4net;
using NET6_Template.App;

namespace NET6_Template.Broker
{
    public abstract class BaseConsumerBroker
    {
        protected ILog logger = null;
        protected BrokerContext _brokerContext;

        public BaseConsumerBroker()
        {
            AppLifeCycle.Register(CloseBrokerConnection);
        }

        public abstract void ConsumeQueue(string queueName);

        public virtual void CloseBrokerConnection()
        {
            BrokerConnector.TryCancelConsumiption(_brokerContext);
        }
    }
}

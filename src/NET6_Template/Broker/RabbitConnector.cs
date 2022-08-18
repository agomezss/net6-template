using System;
using System.Reflection;
using log4net;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace NET6_Template.Broker
{
    public static class RabbitConnector
    {
        private static readonly int _recoveryIntervalTicks = 20000; // 1 minute
        private static readonly int _requestedHeartbeat = 600;
        private static IConnection Connection;

        public static bool HasConnection { get { return Connection != null; } }
        public static bool ConnectionIsOpen { get { return HasConnection && Connection.IsOpen; } }
        public static int TotalSessions { get; set; }
        public static int Connections { get; private set; }

        public static void Init(string queueEndPointUri, bool async)
        {
            try
            {
                Connection = InternalConnect(queueEndPointUri, async);
                Connections++;
            }
            catch
            {
                throw;
            }
        }

        public static void Close()
        {
            try
            {
                Connection.Close();
            }
            catch
            {
                throw;
            }
            finally
            {
                if (Connections > 0)
                    Connections--;
            }
        }

        public static IModel GetModel(ILog logger)
        {
            try
            {
                var model = Connection.CreateModel();
                TotalSessions++;
                return model;
            }
            catch (Exception ex)
            {
                logger.Fatal($"{MethodBase.GetCurrentMethod()}, {ex.Message}", ex);
                Environment.Exit(-1);
            }

            return null;
        }

        public static IModel GetControlChannel(string controlQueue, bool durable, bool exclusive, bool autoDelete, ILog logger)
        {
            try
            {
                var channelControlQueue = GetModel(logger);
                channelControlQueue.QueueDeclare(controlQueue, durable, exclusive, autoDelete);
                channelControlQueue.BasicQos(0, 1, false);
                return channelControlQueue;
            }
            catch (Exception ex)
            {
                logger.Fatal($"{MethodBase.GetCurrentMethod()}, {ex.Message}", ex);
                Environment.Exit(-1);
            }

            return null;
        }
        public static string TryGetIdFromHeader(BasicDeliverEventArgs deliveryEventArgsControl, string cId)
        {
            object auxCid = null;
            deliveryEventArgsControl.BasicProperties?.Headers?.TryGetValue(cId, out auxCid);
            return auxCid != null ? (string)auxCid : cId;
        }

        public static EventingBasicConsumer CreateConsumer(IModel channel)
        {
            return new EventingBasicConsumer(channel);
        }

        public static void StartConsume(IModel channel, EventingBasicConsumer consumer, string queue)
        {
            channel.BasicConsume(consumer, queue, false);
        }

        public static void TryCancelAllConsumerTags(IModel channel, EventingBasicConsumer consumer, ILog logger)
        {
            foreach (var consumeTag in consumer.ConsumerTags)
            {
                try
                {
                    channel.BasicCancel(consumeTag);
                }
                catch (Exception ex)
                {
                    logger.Error($"{MethodBase.GetCurrentMethod()}, {ex.Message}", ex);
                }
            }
        }

        public static void TryAckMessage(IModel channel, BasicDeliverEventArgs deliveryEventArgs, ILog logger)
        {
            try
            {
                channel.BasicAck(deliveryEventArgs.DeliveryTag, false);
            }
            catch (Exception ex)
            {
                logger.Error($"{MethodBase.GetCurrentMethod()}, {ex.Message}", ex);
            }
        }

        public static void TryNackAndRequeueMessage(IModel channel, BasicDeliverEventArgs deliveryEventArgs, ILog logger)
        {
            try
            {
                channel.BasicNack(deliveryEventArgs.DeliveryTag, false, true);
            }
            catch (Exception ex)
            {
                logger.Error($"{MethodBase.GetCurrentMethod()}, {ex.Message}", ex);
            }
        }

        public static void CloseChannel(IModel channel)
        {
            try
            {
                if (channel != null && channel.IsOpen)
                {
                    channel.Close();
                }
            }
            catch
            {
                throw;
            }
            finally
            {
                if (TotalSessions > 0)
                    TotalSessions--;
            }
        }

        public static bool IsConnected()
        {
            try
            {
                return ConnectionIsOpen;
            }
            catch
            {
                return false;
            }
        }

        private static IConnection InternalConnect(string queueEndPointUri, bool async)
        {
            ConnectionFactory factory = new ConnectionFactory()
            {
                Uri = new Uri(queueEndPointUri),
                AutomaticRecoveryEnabled = true,
                NetworkRecoveryInterval = new TimeSpan(_recoveryIntervalTicks),
                RequestedHeartbeat = TimeSpan.FromSeconds(_requestedHeartbeat)
            };

            if (async)
            {
                factory.DispatchConsumersAsync = true;
            }

            factory.ClientProvidedName = "app:consumer ip:" + "3123123";//Utils.GetLocalIPAddress();
            return factory.CreateConnection();
        }
    }
}

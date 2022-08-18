using Amazon.SQS;
using Amazon.SQS.Model;
using log4net;
using NET6_Template.Broker;
using System;
using System.Reflection;

namespace NET6_Template
{
    public static class SQSConnector
    {
        private static AmazonSQSClient Client;
        public static bool HasConnection => Client != null;
        public static int Connections { get; private set; }
        public static int TotalSessions { get; private set; }
        public static string _queueNamePrefix { get; private set; }

        public static void Init(string sqsConnectionString)
        {
            try
            {
                string[] connectionComponents = sqsConnectionString.Split('|');
                _queueNamePrefix = connectionComponents[3];
                Client = new AmazonSQSClient(connectionComponents[0], connectionComponents[1], Amazon.RegionEndpoint.GetBySystemName(connectionComponents[2]));
                Connections++;
            }
            catch
            {
                throw;
            }
        }

        public static void TryPublishMessage(string messageBody, string qUrl, ILog logger)
        {
            try
            {
                SendMessageRequest sendMessageRequest = new SendMessageRequest()
                {
                    MessageBody = messageBody,
                    QueueUrl = _queueNamePrefix + qUrl
                };

                SendMessageResponse result = Client.SendMessageAsync(sendMessageRequest).GetAwaiter().GetResult();

                if (result.HttpStatusCode != System.Net.HttpStatusCode.OK)
                {
                    throw new Exception($"Unexpected error ocurred. Code: {result.HttpStatusCode}. Metadata: {result?.ResponseMetadata?.Metadata}");
                }
            }
            catch (Exception ex)
            {
                logger.Error($"{MethodBase.GetCurrentMethod()}, {ex.Message}", ex);
                throw;
            }
        }

        public static void TryCreateQueue(string qUrl, ILog logger)
        {
            try
            {
                CreateQueueResponse result = Client.CreateQueueAsync(qUrl).GetAwaiter().GetResult();

                if (result.HttpStatusCode != System.Net.HttpStatusCode.OK)
                {
                    throw new Exception($"Unexpected error ocurred. Code: {result.HttpStatusCode}. Metadata: {result?.ResponseMetadata?.Metadata}");
                }
            }
            catch (Exception ex)
            {
                logger.Error($"{MethodBase.GetCurrentMethod()}, {ex.Message}", ex);
                throw;
            }
        }

        public static Message GetMessage(string qUrl, ILog logger)
        {
            try
            {
                var result = Client.ReceiveMessageAsync(new ReceiveMessageRequest
                {
                    QueueUrl = _queueNamePrefix + qUrl,
                    MaxNumberOfMessages = 1,
                    WaitTimeSeconds = (int)TimeSpan.FromSeconds(20).TotalSeconds,
                    VisibilityTimeout = (int)TimeSpan.FromMinutes(90).TotalSeconds
                }).GetAwaiter().GetResult();

                if (result.HttpStatusCode != System.Net.HttpStatusCode.OK)
                {
                    throw new Exception($"Unexpected error ocurred. Code: {result.HttpStatusCode}. Metadata: {result?.ResponseMetadata?.Metadata}");
                }

                return result.Messages != null && result.Messages.Count > 0 ?
                       result.Messages[0] :
                       null;
            }
            catch (Exception ex)
            {
                logger.Error($"{MethodBase.GetCurrentMethod()}, {ex.Message}", ex);
                throw;
            }
        }

        public static void TryAckMessage(Message message, string qUrl, ILog logger)
        {
            try
            {
                var result = Client.DeleteMessageAsync(_queueNamePrefix + qUrl, message.ReceiptHandle).GetAwaiter().GetResult();

                if (result.HttpStatusCode != System.Net.HttpStatusCode.OK)
                {
                    throw new Exception($"Unexpected error ocurred. Code: {result.HttpStatusCode}. Metadata: {result?.ResponseMetadata?.Metadata}");
                }
            }
            catch (Exception ex)
            {
                logger.Error($"{MethodBase.GetCurrentMethod()}, {ex.Message}", ex);
            }
        }

        public static void TryNackAndRequeueMessage(Message message, string qUrl, ILog logger)
        {
            try
            {
                var result = Client.ChangeMessageVisibilityAsync(_queueNamePrefix + qUrl, message.ReceiptHandle, (int)TimeSpan.FromMinutes(1).TotalSeconds).GetAwaiter().GetResult();

                if (result.HttpStatusCode != System.Net.HttpStatusCode.OK)
                {
                    throw new Exception($"Unexpected error ocurred. Code: {result.HttpStatusCode}. Metadata: {result?.ResponseMetadata?.Metadata}");
                }
            }
            catch (Exception ex)
            {
                logger.Error($"{MethodBase.GetCurrentMethod()}, {ex.Message}", ex);
            }
        }

        public static void StartConsume(BrokerContext context)
        {
            TotalSessions++;

            do
            {
                var message = GetMessage(context.queue, context.logger);

                if (context.IsActive && message != null)
                {
                    context.HandleSQSMessage(message);
                }

            } while (context.IsActive && HasConnection);
        }

        public static void TryCancelAllConsumerTags(BrokerContext context)
        {
            context.IsActive = false;
            TotalSessions--;
        }


        public static void TryChangeMessageVisibility(object context)
        {
            var brokerContext = context as BrokerContext;

            try
            {
                ChangeMessageVisibilityResponse result = Client.ChangeMessageVisibilityAsync(_queueNamePrefix + brokerContext.queue, brokerContext.TryGetMessageReceipt(), (int)TimeSpan.FromMinutes(1).TotalSeconds).GetAwaiter().GetResult();

                if (result.HttpStatusCode != System.Net.HttpStatusCode.OK)
                {
                    throw new Exception($"Unexpected error ocurred. Code: {result.HttpStatusCode}. Metadata: {result?.ResponseMetadata?.Metadata}");
                }
            }
            catch (Exception ex)
            {
                brokerContext.logger.Error($"{MethodBase.GetCurrentMethod()}, {ex.Message}", ex);
            }
        }

        public static bool IsConnected()
        {
            try
            {
                return HasConnection;
            }
            catch
            {
                return false;
            }
        }

        public static void Close()
        {
            try
            {
                Client.Dispose();
            }
            catch
            {
                throw;
            }
            finally
            {
                if (Connections > 0)
                {
                    Connections--;
                }
            }
        }
    }
}

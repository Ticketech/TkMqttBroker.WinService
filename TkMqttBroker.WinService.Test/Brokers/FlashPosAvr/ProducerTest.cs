using Microsoft.VisualStudio.TestTools.UnitTesting;
using MQTTnet;
using System;
using System.Threading;
using System.Threading.Tasks;
using TkMqttBroker.WinService.Brokers.FlashPosAvr;
using TkMqttBroker.WinService.Test.Mockers;

namespace TkMqttBroker.WinService.Test.Brokers.FlashPosAvr
{
    [TestClass]
    public class ProducerTest
    {
        [TestMethod]
        public void TestMethod1()
        {
            FlashAvrProducerConfiguration configuration = null;
            MqttClientMock mock = new MqttClientMock();
            var producer = new FlashAvrProducer(configuration, mock);


            string clientId = "";

            MqttApplicationMessage message = new MqttApplicationMessage
            {
            };
            MqttApplicationMessageReceivedEventArgs avrdata = new MqttApplicationMessageReceivedEventArgs(
                clientId, message);

            Task.Run(async () =>
            {
                var data = new MqttApplicationMessageBuilder()
                 .WithTopic("test/topic")
                 .WithPayload("¡Hola desde MQTT - Alvaro!")
                 .WithExactlyOnceQoS()  // QoS 2 para entrega exacta
                 .WithRetainFlag()  // Conservar el mensaje en el broker
                 .Build();
                await mock.PublishAsync(data, CancellationToken.None);
            });

        }
    }
}

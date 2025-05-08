using Microsoft.VisualStudio.TestTools.UnitTesting;
using MQTTnet;
using System;
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
                clientId, message) ;
            mock.UseApplicationMessageReceivedHandler.Invoke(avrdata);

        }
    }
}

using Microsoft.VisualStudio.TestTools.UnitTesting;
using MQTTnet;
using Newtonsoft.Json;
using System;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Tk.ConfigurationManager;
using Tk.NetTiers;
using Tk.Utilities.Log4Net;
using TkMqttBroker.WinService.Brokers.FlashPosAvr;
using TkMqttBroker.WinService.Test.Mockers;
using TkMqttBroker.WinService.Test.Proxies;

namespace TkMqttBroker.WinService.Test.Brokers.FlashPosAvr
{
    [TestClass]
    public class ProducerTest
    {
        public static log4net.ITktLog logger = log4net.TktLogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);


        [TestInitialize]
        public void InitializeTest()
        {
            FlashPosAvrInitializer.Initialize();
        }


        [TestMethod]
        public void TestPublish()
        {
            //tests publishing is captured by producer and processed accordingly

            FlashPosAvrCameraConfiguration configuration = null;
            MqttClientMock mock = new MqttClientMock();

            var producer = new FlashPosAvrProducer(configuration, mock);

            string clientId = $"{TkConfigurationManager.CurrentLocationId}-ClientId";

            //MqttApplicationMessage message = new MqttApplicationMessage
            //{
            //};
            //MqttApplicationMessageReceivedEventArgs avrdata = new MqttApplicationMessageReceivedEventArgs(
            //    clientId, message);

            string testType = $"{DateTime.Now:yyyyMMddHHmmssffffff}";

            int count1 = Proxies.PosProxy.SyncQueue.Count();

            Task.Run(async () =>
            {
                var data = new MqttApplicationMessageBuilder()
                 .WithTopic("detection")
                 .WithPayload(JsonConvert.SerializeObject(new FVRPayload
                 {
                     eventType = testType,
                 }))
                 .WithExactlyOnceQoS()  // QoS 2 para entrega exacta
                 .WithRetainFlag()  // Conservar el mensaje en el broker
                 .Build();

                await mock.PublishAsync(data, CancellationToken.None);
            }).Wait();

            //sync queue
            int count2 = Proxies.PosProxy.SyncQueue.Count();

            Assert.AreEqual(count1 + 1, count2);

            //pos avr


        }
    }
}

using Microsoft.VisualStudio.TestTools.UnitTesting;
using MQTTnet;
using Newtonsoft.Json;
using System;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Tk.ConfigurationManager;
using Tk.Utilities.Log4Net;
using TkMqttBroker.WinService.Brokers.FlashPosAvr;
using TkMqttBroker.WinService.Test.Mockers;

namespace TkMqttBroker.WinService.Test.Brokers.FlashPosAvr
{
    [TestClass]
    public class ProducerTest
    {
        public static log4net.ITktLog logger = log4net.TktLogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);


        [TestInitialize]
        public void InitializeTest()
        {
            //log4net
            Log4NetHelper.Init();
            Tk.NetTiers.DataAccessLayer.TransactionManager trmgr = Tk.NetTiers.DataAccessLayer.DataRepository.Provider.CreateTransaction();
            Log4NetHelper.setAdoNetAppenderConnection(trmgr.ConnectionString);
            ConfigFileSections configFileSections = (ConfigFileSections)System.Configuration.ConfigurationManager.GetSection("currentLocationGUID");
            Log4NetHelper.setLocationGuid(new Guid(configFileSections.CurrentLocationGUID));
            Log4NetHelper.setSoftwareVersion(Assembly.GetAssembly(typeof(FlashPosAvrService)).GetName().Version.ToString());
            trmgr.Dispose();
        }


        [TestMethod]
        public void TestMethod1()
        {
            FlashAvrProducerConfiguration configuration = null;
            MqttClientMock mock = new MqttClientMock();

            var producer = new FlashAvrProducer(configuration, mock);

            string clientId = "FlashPosAvr-Test-ClientId";

            MqttApplicationMessage message = new MqttApplicationMessage
            {
            };
            MqttApplicationMessageReceivedEventArgs avrdata = new MqttApplicationMessageReceivedEventArgs(
                clientId, message);

            Task.Run(async () =>
            {
                var data = new MqttApplicationMessageBuilder()
                 .WithTopic("test/topic")
                 .WithPayload(JsonConvert.SerializeObject(new FVRFlashAvrData
                 {
                 }))
                 .WithExactlyOnceQoS()  // QoS 2 para entrega exacta
                 .WithRetainFlag()  // Conservar el mensaje en el broker
                 .Build();
                await mock.PublishAsync(data, CancellationToken.None);
            }).Wait();

        }
    }
}

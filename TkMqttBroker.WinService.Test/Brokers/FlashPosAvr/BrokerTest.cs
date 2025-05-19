using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Tk.ConfigurationManager;
using Tk.Utilities.Log4Net;
using TkMqttBroker.WinService.Brokers.FlashPosAvr;
using TkMqttBroker.WinService.Test.Mockers;
using TkMqttBroker.WinService.Test.Proxies;

namespace TkMqttBroker.WinService.Test.Brokers.FlashPosAvr
{
    [TestClass]
    public class BrokerTest
    {

        public static log4net.ITktLog logger = log4net.TktLogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);


        [TestInitialize]
        public void InitializeTest()
        {
            FlashPosAvrInitializer.Initialize();
        }



        [TestMethod]
        public void TestBroker()
        {
            //A.
            var mqttClient = new MqttClientMock();
            var broker = new FlashPosAvrBroker(mqttClient);

            PosProxy.SyncQueue.Clear();
            PosProxy.Workstations.AddAVRFlash();

            Task.Run(async () =>
            {
                await broker.Start();

                FVRPayload data = await mqttClient.PublishDetection();

                Thread.Sleep(30000);

                await broker.Stop();
            }).Wait();

            var sync = Proxies.PosProxy.SyncQueue.GetLatest();

            Assert.IsNotNull(sync.SynqSyncDate);
            Assert.AreEqual(sync.SynqCount, 1);
        }
    }
}

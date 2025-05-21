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
        public void TestBrokerDetection()
        {
            //tests broker with detection events from camera


            //A. all ok
            var mqttClient = new MqttClientMock();
            var pos = new PosProxyMock(true);
            var ng = new NGProxyMock(true);
            var broker = new FlashPosAvrBroker(mqttClient, pos, ng);

            PosProxy.SyncQueue.Clear();
            PosProxy.Workstations.AddAVRFlash();

            Task.Run(async () =>
            {
                await broker.Start();

                FVRPayload data = await mqttClient.PublishDetection();

                Thread.Sleep(20000);

                await broker.Stop();
            }).Wait();

            var sync = Proxies.PosProxy.SyncQueue.GetLatest();

            Assert.IsNotNull(sync.SynqSyncDate);
            Assert.AreEqual(sync.SynqCount, 1);



            //A. pos fails
            mqttClient = new MqttClientMock();
            pos = new PosProxyMock(false);
            ng = new NGProxyMock(true);
            broker = new FlashPosAvrBroker(mqttClient, pos, ng);

            PosProxy.SyncQueue.Clear();
            PosProxy.Workstations.AddAVRFlash();

            Task.Run(async () =>
            {
                await broker.Start();

                FVRPayload data = await mqttClient.PublishDetection();

                Thread.Sleep(20000);

                await broker.Stop();
            }).Wait();

            sync = Proxies.PosProxy.SyncQueue.GetLatest();

            Assert.IsNotNull(sync.SynqSyncDate);
            Assert.AreEqual(sync.SynqCount, 1);





            //A. ng fails
            mqttClient = new MqttClientMock();
            pos = new PosProxyMock(true);
            ng = new NGProxyMock(false);
            broker = new FlashPosAvrBroker(mqttClient, pos, ng);

            PosProxy.SyncQueue.Clear();
            PosProxy.Workstations.AddAVRFlash();

            Task.Run(async () =>
            {
                await broker.Start();

                FVRPayload data = await mqttClient.PublishDetection();

                Thread.Sleep(20000);

                //first fails
                sync = Proxies.PosProxy.SyncQueue.GetLatest();
                Assert.IsNull(sync.SynqSyncDate);


                //second works
                ng.SetSendResult(true);

                Thread.Sleep(20000);

                //first fails
                sync = Proxies.PosProxy.SyncQueue.GetLatest();
                Assert.IsNotNull(sync.SynqSyncDate);


                await broker.Stop();
            }).Wait();

        
          
        }
    }
}

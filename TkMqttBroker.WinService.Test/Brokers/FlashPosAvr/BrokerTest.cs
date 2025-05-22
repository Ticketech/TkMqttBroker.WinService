using Microsoft.VisualStudio.TestTools.UnitTesting;
using MQTTnet;
using Newtonsoft.Json;
using System;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Tk.ConfigurationManager;
using Tk.Services.REST.Models.Stays;
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
        public void TestPosSync()
        {
            //tests broker does pos sync correctly


            //A. all ok
            var mqttClient = new MqttClientMock();
            var pos = new PosProxyMock(true, null);
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
            pos = new PosProxyMock(false, null);
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





            //A. ng fails, then ok
            mqttClient = new MqttClientMock();
            pos = new PosProxyMock(true, null);
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



        [TestMethod]
        public void TestConsume()
        {
            //test events are properly consumed


            //A. checkin ok
            StayInfo stay = new StayInfo
            {
                checkin_time = DateTime.Now,
                checkin_wsid = "070",
                stay_guid = Guid.NewGuid(),
                stay_type = Tk.Services.REST.Models.Service.EStayTypes.Transient,
                ticket_number = 200001
            };

            FVRPayload payload = new FVRPayload
            {
                eventData = new FVREventData
                {
                    color = "RED",
                    colorConfidence = 0.9,
                    make = "FORD",
                    makeConfidence = 0.9,
                    licensePlate = "1234567",
                    licensePlateConfidence = 0.9,
                    encounterId = Guid.NewGuid().ToString(),
                    state = "new york",
                    stateConfidence = 0.9,
                }
            };

            MqttApplicationMessage appMess = new MqttApplicationMessage
            {
                Topic = "detection",
                Payload = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(payload))
            };

            var mqttClient = new MqttClientMock();
            var pos = new PosProxyMock(true, stay);
            var ng = new NGProxyMock(true);
            var broker = new FlashPosAvrBroker(mqttClient, pos, ng);

            PosProxy.SyncQueue.Clear();
            PosProxy.Workstations.AddAVRFlash();

            Task.Run(async () =>
            {
                await broker.Start();

                await mqttClient.PublishAsync(appMess, CancellationToken.None);

                Thread.Sleep(10000);

                await broker.Stop();
            }).Wait();

            Assert.IsNotNull(mqttClient.LastAck);
            Assert.IsTrue(mqttClient.LastAck.Contains(payload.eventData.encounterId));

            Assert.IsTrue(mqttClient.LastOutcome.Contains(payload.eventData.encounterId));
            Assert.IsTrue(mqttClient.LastOutcome.Contains("CheckIn"));
            Assert.IsTrue(mqttClient.LastOutcome.Contains(stay.ticket_number.ToString()));
            Assert.IsTrue(mqttClient.LastOutcome.Contains(stay.stay_type.ToString()));

            Assert.AreEqual(pos.LastRequest.infoplate.colour, payload.eventData.color);
            Assert.AreEqual(pos.LastRequest.infoplate.make, payload.eventData.make);
            Assert.IsTrue(pos.LastRequest.infoplate.plate.Contains(payload.eventData.licensePlate));
            Assert.AreEqual(pos.LastRequest.infoplate.confidence, payload.eventData.licensePlateConfidence);
            Assert.AreEqual(pos.LastRequest.infoplate.region_confidence, payload.eventData.stateConfidence);
            Assert.AreEqual(pos.LastRequest.infoplate.region, payload.eventData.state);




            //A. checkout ok




            //A. confidence wrong





            //A. checkin/out failed



        }
    }
}

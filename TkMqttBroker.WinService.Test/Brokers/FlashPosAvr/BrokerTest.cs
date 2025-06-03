using Microsoft.VisualStudio.TestTools.UnitTesting;
using MQTTnet;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Tk.ConfigurationManager;
using Tk.NetTiers;
using Tk.NetTiers.DataAccessLayer;
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
        }



        [TestMethod]
        public void TestInOut()
        {
            //test two cameras, one in, one out

            FPAInitializer.Initialize();


            //A. all ok
            var inMock = new MqttClientMock();
            var outMock = new MqttClientMock();

            var pos = new FPAPosProxy();
            var ng = new FPANGProxy();

            string inWkid = "AVR079";
            string outWkid = "AVR073";

            var broker = new FPABroker(new Dictionary<string, IMqttClientMock>
                {
                    { inWkid, inMock },
                    { outWkid, outMock }
                }, pos, ng);

            PosProxy.SyncQueue.Clear();


            //A. monthly in/out
            MPS tag = PosProxy.MPSProxy.NewTag();
            PosProxy.MPSProxy.AddTagToPOS(tag);

            Task.Run(async () =>
            {
                await broker.Start();

                FVRPayload payloadIn = MapPayload(tag);
                await inMock.PublishDetection(payloadIn);

                Thread.Sleep(10000);

                FVRPayload payloadOut = MapPayload(tag);
                await outMock.PublishDetection(payloadOut);

                Thread.Sleep(210000);

                await broker.Stop();
            }).Wait();

            Stays stay = PosProxy.StaysProxy.GetLatest();
            Assert.IsNotNull(stay.StayDateOut);
            Assert.AreEqual(stay.StayType, 1);

            Vehicles vehicle = DataRepository.VehiclesProvider.GetByVehicleGUID(stay.VehicleGUID);
            Assert.AreEqual(tag.Plate, vehicle.VehiclePlate);
        }



        [TestMethod]
        public void TestPosSync()
        {
            //tests broker does pos sync correctly

            FPAInitializer.Initialize();

            string wkid = PosProxy.Workstations.SetAVRFlash();


            //A. all ok
            var mqttClient = new MqttClientMock();
            var mqttMocks = new Dictionary<string, IMqttClientMock> { { wkid, mqttClient } };
            var pos = new PosProxyMock(true, null);
            var ng = new NGProxyMock(true);
            var broker = new FPABroker(mqttMocks, pos, ng);

            PosProxy.SyncQueue.Clear();

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
            mqttMocks = new Dictionary<string, IMqttClientMock> { { wkid, mqttClient } };
            pos = new PosProxyMock(false, null);
            ng = new NGProxyMock(true);
            broker = new FPABroker(mqttMocks, pos, ng);

            PosProxy.SyncQueue.Clear();

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
            mqttMocks = new Dictionary<string, IMqttClientMock> { { wkid, mqttClient } };
            pos = new PosProxyMock(true, null);
            ng = new NGProxyMock(false);
            broker = new FPABroker(mqttMocks, pos, ng);

            PosProxy.SyncQueue.Clear();
            PosProxy.Workstations.SetAVRFlash();

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
        public void TestCheckInConsume()
        {
            //test events are properly consumed for camera on the ENTRY line
            //only one flash camera must be in devices for this test to work

            //pos camera device
            string direction = "ENTRY";
            string workstationId = PosProxy.Workstations.SetAVRFlash(direction);

            FPAInitializer.Initialize();


            //A. checkin ok
            StayInfo posResponse = new StayInfo
            {
                checkin_time = DateTime.Now,
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
            var mqttMocks = new Dictionary<string, IMqttClientMock> { { workstationId, mqttClient } };
            var pos = new PosProxyMock(true, posResponse);
            var ng = new NGProxyMock(true);
            var broker = new FPABroker(mqttMocks, pos, ng);

            PosProxy.SyncQueue.Clear();

            Task.Run(async () =>
            {
                await broker.Start();

                await mqttClient.PublishAsync(appMess, CancellationToken.None);

                Thread.Sleep(10000);

                await broker.Stop();
            }).Wait();

            //ack
            Assert.IsNotNull(mqttClient.LastAck);
            Assert.IsTrue(mqttClient.LastAck.Contains(payload.eventData.encounterId));

            //consume
            Assert.IsTrue(mqttClient.LastOutcome.Contains(payload.eventData.encounterId));
            Assert.IsTrue(mqttClient.LastOutcome.Contains("CheckIn"));
            Assert.IsTrue(mqttClient.LastOutcome.Contains(posResponse.ticket_number.ToString()));
            Assert.IsTrue(mqttClient.LastOutcome.Contains(posResponse.stay_type.ToString()));

            //pos call
            Assert.AreEqual(pos.LastRequest.infoplate.colour, payload.eventData.color);
            Assert.AreEqual(pos.LastRequest.infoplate.make, payload.eventData.make);
            Assert.IsTrue(pos.LastRequest.infoplate.plate.Contains(payload.eventData.licensePlate));
            Assert.IsTrue(pos.LastRequest.infoplate.plate.Contains(direction));
            Assert.AreEqual(pos.LastRequest.infoplate.confidence, PosConfidence(payload.eventData.licensePlateConfidence));
            Assert.AreEqual(pos.LastRequest.infoplate.region_confidence, PosConfidence(payload.eventData.stateConfidence));
            Assert.IsTrue(pos.LastRequest.infoplate.region.Contains(FPAPolicy.StateCode(payload.eventData.state).ToLower()));
            Assert.IsTrue(pos.LastRequest.infoplate.workstation_id.Contains(workstationId));




            //A. make, color, state confidence not enough
            posResponse = new StayInfo
            {
                checkin_time = DateTime.Now,
                stay_guid = Guid.NewGuid(),
                stay_type = Tk.Services.REST.Models.Service.EStayTypes.Transient,
                ticket_number = 200001
            };

            payload = new FVRPayload
            {
                eventData = new FVREventData
                {
                    color = "RED",
                    colorConfidence = 0.1,
                    make = "FORD",
                    makeConfidence = 0.1,
                    licensePlate = "1234567",
                    licensePlateConfidence = 0.9,
                    encounterId = Guid.NewGuid().ToString(),
                    state = "oregon",
                    stateConfidence = 0.1,
                }
            };

            appMess = new MqttApplicationMessage
            {
                Topic = "detection",
                Payload = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(payload))
            };

            mqttClient = new MqttClientMock();
            mqttMocks = new Dictionary<string, IMqttClientMock> { { workstationId, mqttClient } };
            pos = new PosProxyMock(true, posResponse);
            ng = new NGProxyMock(true);
            broker = new FPABroker(mqttMocks, pos, ng);

            PosProxy.SyncQueue.Clear();

            Task.Run(async () =>
            {
                await broker.Start();

                await mqttClient.PublishAsync(appMess, CancellationToken.None);

                Thread.Sleep(10000);

                await broker.Stop();
            }).Wait();

            //ack
            Assert.IsNotNull(mqttClient.LastAck);
            Assert.IsTrue(mqttClient.LastAck.Contains(payload.eventData.encounterId));

            //consume
            Assert.IsTrue(mqttClient.LastOutcome.Contains(payload.eventData.encounterId));
            Assert.IsTrue(mqttClient.LastOutcome.Contains("CheckIn"));
            Assert.IsTrue(mqttClient.LastOutcome.Contains(posResponse.ticket_number.ToString()));
            Assert.IsTrue(mqttClient.LastOutcome.Contains(posResponse.stay_type.ToString()));

            //pos call
            Assert.AreEqual(pos.LastRequest.infoplate.colour, "OTHER");
            Assert.AreEqual(pos.LastRequest.infoplate.make, "OTHER");
            Assert.IsTrue(pos.LastRequest.infoplate.plate.Contains(payload.eventData.licensePlate));
            Assert.IsTrue(pos.LastRequest.infoplate.plate.Contains(direction));
            Assert.AreEqual(pos.LastRequest.infoplate.confidence, PosConfidence(payload.eventData.licensePlateConfidence));
            Assert.AreEqual(pos.LastRequest.infoplate.region_confidence, 100); //100% default state
            Assert.IsTrue(pos.LastRequest.infoplate.region.Contains("ny")); //default state = location state
            Assert.IsTrue(pos.LastRequest.infoplate.workstation_id.Contains(workstationId));




            //A. plate confidence not enough
            posResponse = null;

            payload = new FVRPayload
            {
                eventData = new FVREventData
                {
                    color = "RED",
                    colorConfidence = 0.9,
                    make = "FORD",
                    makeConfidence = 0.9,
                    licensePlate = "1234567",
                    licensePlateConfidence = 0.1,
                    encounterId = Guid.NewGuid().ToString(),
                    state = "new york",
                    stateConfidence = 0.9,
                }
            };

            appMess = new MqttApplicationMessage
            {
                Topic = "detection",
                Payload = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(payload))
            };

            mqttClient = new MqttClientMock();
            mqttMocks = new Dictionary<string, IMqttClientMock> { { workstationId, mqttClient } };
            pos = new PosProxyMock(true, posResponse);
            ng = new NGProxyMock(true);
            broker = new FPABroker(mqttMocks, pos, ng);

            PosProxy.SyncQueue.Clear();

            Task.Run(async () =>
            {
                await broker.Start();

                await mqttClient.PublishAsync(appMess, CancellationToken.None);

                Thread.Sleep(10000);

                await broker.Stop();
            }).Wait();

            //ack
            Assert.IsNotNull(mqttClient.LastAck);
            Assert.IsTrue(mqttClient.LastAck.Contains(payload.eventData.encounterId));

            //consume
            Assert.IsTrue(mqttClient.LastOutcome.Contains(payload.eventData.encounterId));
            Assert.IsTrue(mqttClient.LastOutcome.Contains("Confidence below threshold--not processed"));

            //pos call
            Assert.IsNull(pos.LastRequest);




            //A. checkin failed
            posResponse = null;

            payload = new FVRPayload
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

            appMess = new MqttApplicationMessage
            {
                Topic = "detection",
                Payload = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(payload))
            };

            mqttClient = new MqttClientMock();
            mqttMocks = new Dictionary<string, IMqttClientMock> { { workstationId, mqttClient } };
            pos = new PosProxyMock(false, posResponse);
            ng = new NGProxyMock(true);
            broker = new FPABroker(mqttMocks, pos, ng);

            PosProxy.SyncQueue.Clear();

            Task.Run(async () =>
            {
                await broker.Start();

                await mqttClient.PublishAsync(appMess, CancellationToken.None);

                Thread.Sleep(10000);

                await broker.Stop();
            }).Wait();

            //ack
            Assert.IsNotNull(mqttClient.LastAck);
            Assert.IsTrue(mqttClient.LastAck.Contains(payload.eventData.encounterId));

            //consume
            Assert.IsTrue(mqttClient.LastOutcome.Contains(payload.eventData.encounterId));
            Assert.IsTrue(mqttClient.LastOutcome.Contains("Received by POS--check in/out failed"));

            //pos call
            Assert.IsNotNull(pos.LastRequest);


        }



        [TestMethod]
        public void TestCheckOutConsume()
        {
            //test events are properly consumed for camera on the EXIT line
            //only one flash camera must be in devices for this test to work


            //pos camera device
            string direction = "EXIT";
            string workstationId = PosProxy.Workstations.SetAVRFlash(direction);

            FPAInitializer.Initialize();


            //A. checkout ok
            var posResponse = new StayInfo
            {
                checkin_time = DateTime.Now,
                checkin_wsid = $"{workstationId}",
                stay_guid = Guid.NewGuid(),
                stay_type = Tk.Services.REST.Models.Service.EStayTypes.Transient,
                ticket_number = 200001,
                checkout_time = DateTime.Now,
            };

            var payload = new FVRPayload
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

            var appMess = new MqttApplicationMessage
            {
                Topic = "detection",
                Payload = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(payload))
            };

            var mqttClient = new MqttClientMock();
            var mqttMocks = new Dictionary<string, IMqttClientMock> { { workstationId, mqttClient } };
            var pos = new PosProxyMock(true, posResponse);
            var ng = new NGProxyMock(true);
            var broker = new FPABroker(mqttMocks, pos, ng);

            PosProxy.SyncQueue.Clear();

            Task.Run(async () =>
            {
                await broker.Start();

                await mqttClient.PublishAsync(appMess, CancellationToken.None);

                Thread.Sleep(10000);

                await broker.Stop();
            }).Wait();

            //ack
            Assert.IsNotNull(mqttClient.LastAck);
            Assert.IsTrue(mqttClient.LastAck.Contains(payload.eventData.encounterId));

            //consume
            Assert.IsTrue(mqttClient.LastOutcome.Contains(payload.eventData.encounterId));
            Assert.IsTrue(mqttClient.LastOutcome.Contains("CheckOut"));
            Assert.IsTrue(mqttClient.LastOutcome.Contains(posResponse.ticket_number.ToString()));
            Assert.IsTrue(mqttClient.LastOutcome.Contains(posResponse.stay_type.ToString()));

            //pos call
            Assert.IsTrue(pos.LastRequest.infoplate.plate.Contains(payload.eventData.licensePlate));
            Assert.IsTrue(pos.LastRequest.infoplate.plate.Contains(direction));
            Assert.AreEqual(pos.LastRequest.infoplate.confidence, PosConfidence(payload.eventData.licensePlateConfidence));
            Assert.AreEqual(pos.LastRequest.infoplate.workstation_id, workstationId);
        }




        // PRIVATE ///////////////////////////////////////////

        private FVRPayload MapPayload(MPS tag)
        {
            return new FVRPayload
            {
                deviceMxId = "abc-1234",
                eventId = Guid.NewGuid().ToString(),
                mainImagePath = "g://abc/main.jpg",
                lpCropPath = "g://abc/crop.jpg",
                lpCropPaths = new List<string> { { "g://abc/evidence__IR__LPC.jpg" } },
                eventDate = DateTime.Now,

                eventData = new FVREventData
                {
                    laneId = "1",
                    color = tag.Color,
                    colorConfidence = 99,
                    encounterId = Guid.NewGuid().ToString(),
                    licensePlate = tag.Plate,
                    licensePlateConfidence  = 99,
                    make = tag.Make,
                    makeConfidence = 99,
                    model = "CAR",
                    modelConfidence = 99,
                    state = "new york",
                    stateConfidence = 99,
                    type = "CAR",
                    typeConfidence = 99,
                }
            };
        }


        public static int PosConfidence(double fvrConfidence)
        {
            return (int)Math.Round(fvrConfidence * 100.0);
        }

    }
}

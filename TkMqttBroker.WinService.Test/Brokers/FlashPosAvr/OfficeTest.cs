using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Threading;
using System.Threading.Tasks;
using TkMqttBroker.WinService.Brokers.FlashPosAvr;
using TkMqttBroker.WinService.Test.Mockers;
using TkMqttBroker.WinService.Test.Proxies;

namespace TkMqttBroker.WinService.Test.Brokers.FlashPosAvr
{
    [TestClass]
    public class OfficeTest
    {
        //test fvr camera in ticketech office

        [TestInitialize]
        public void InitializeTest()
        {
            
        }


        [TestMethod]
        public void TestLive()
        {
            //tests fpz with live camera, pos and ng

            FlashPosAvrInitializer.Initialize();

            var pospolicy = FlashPosAvrPolicy.GetPosPolicies();
            pospolicy.TicketechNG.NGService.ServiceUrl.Value = "https://mpk.ticketech.app";
            pospolicy.TicketechNG.CoreApiKey.Value = "_HD1.0_KtCiFm5CzX1bpwaU35It4pDBq0YB4TuWFVUS9DxQWNSxog+q/vL02P2U38nccSvBuA3N/LrIYKO4x9sWgMNpLvwgWbxhn6Y+M4Y/z+rJgnw7AaBI8F+ChzhJ1m7PIfHrsixm9gHtWw3UMKjnZpr13QiEvvIXv0cj6G4cEj13oz8OZmlw6uraxA==";
            FlashPosAvrPolicy.SetPosPolicies(pospolicy);


            string cameraIP = "10.30.50.106";
            string direction = "ENTRY";

            string workstationId = PosProxy.Workstations.SetAVRFlash(direction, cameraIP);

            
            PosProxy.SyncQueue.Clear();

            var broker = new FlashPosAvrBroker();

            Task.Run(async () => {
                await broker.Start();

                Thread.Sleep(120000);

                await broker.Stop();
            }).Wait();
        }


        [TestMethod]
        public void TestReadOfficeCamera()
        {
            //test reading events from camera installed in office

            FlashPosAvrInitializer.Initialize();

            //A. read camera
            FlashPosAvrCameraConfiguration configuration = new FlashPosAvrCameraConfiguration
            {
                IP = "10.30.50.106", // "broker.hivemq.com"
                Port = 1884,
                Direction = FPADirection.ENTRY,
                WorkstationId = "FPA077",
            };
            var reader = new FPAProducerMock(configuration);

            Task.Run(async () => 
            {
                await reader.Start();

                Thread.Sleep(300000);

                await reader.Stop();
            }).Wait();

        }
    }
}

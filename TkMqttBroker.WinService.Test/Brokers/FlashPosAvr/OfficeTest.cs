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

            FPAInitializer.Initialize();

            var pospolicy = FPAPolicy.GetPosPolicies();
            pospolicy.TicketechNG.NGService.ServiceUrl.Value = "https://mpk.parkingdreams.app";
            //prod. pospolicy.TicketechNG.CoreApiKey.Value = "_HD1.0_KtCiFm5CzX1bpwaU35It4pDBq0YB4TuWFVUS9DxQWNSxog+q/vL02P2U38nccSvBuA3N/LrIYKO4x9sWgMNpLvwgWbxhn6Y+M4Y/z+rJgnw7AaBI8F+ChzhJ1m7PIfHrsixm9gHtWw3UMKjnZpr13QiEvvIXv0cj6G4cEj13oz8OZmlw6uraxA==";
            //dev. only read. mpk222. pospolicy.TicketechNG.CoreApiKey.Value = "_HD1.0_AAPKKtkouejdpPCQMeVRTgsCxRUJETk4/+qH8OZ9MeZgv2Vr3VVJAH1W0AJfwrQU2fmA4hgeRll1rRh4MU+D0j6TgGfY+KxjEgB2r9BbfHh1lKxahQjP1TeT3pTwU+bGrCZ4U6YI7bXSbWThNwFaGOE8nNa62TseqBApCBEQPS7RTZCb8+sc1g==";

            //dev. all permits. mpk222.
            pospolicy.TicketechNG.CoreApiKey.Value = "_HD1.0_+ZW3lZZ2ov4T1vQuhtvm6XskR9rrt2m9cUQHiV+Kl3MtJKpoizj6apHZhpziQ2ADhwpoD+qF3GzYgTfU4mYWgOH3PQXFIW4j5++jsQ5L4PygyDaF5zzoBXScTUKLo4+xfaLH9Nm12W5EFBbXGh6IdUQ4o/2nAg0vzo6bGQ8OkLF+ZjwgD5SJ2Q==";

            FPAPolicy.SetPosPolicies(pospolicy);


            string cameraIP = "10.30.50.106";
            string direction = "ENTRY";

            //string workstationId = PosProxy.Workstations.SetAVRFlash(direction, cameraIP);

            
            PosProxy.SyncQueue.Clear();

            var broker = new FPABroker();

            Task.Run(async () => {
                await broker.Start();

                Thread.Sleep(300000);

                await broker.Stop();
            }).Wait();
        }


        [TestMethod]
        public void TestReadOfficeCamera()
        {
            //test reading events from camera installed in office

            FPAInitializer.Initialize();

            //A. read camera
            FPACameraConfiguration configuration = new FPACameraConfiguration
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

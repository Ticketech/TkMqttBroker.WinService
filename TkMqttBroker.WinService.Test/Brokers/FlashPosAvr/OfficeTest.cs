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

            //prod. mpk222, all permits
            pospolicy.TicketechNG.NGService.ServiceUrl.Value = "https://mpk.ticketech.app";
            pospolicy.TicketechNG.CoreApiKey.Value = "_HD1.0_mdnirNzU85fS5tvaHii7UdFit418oy51NzL/XwLH4RE4weiY0RTHwKGN6pYHgKpFNL6sTfaW1XauThZae8w0iQNHVkXwJNpTghdDyvkxsfttD4DFOFJse+RCs72PLUnZNTlLdfZ4s15FIhHRwOVwVqwE9YfEoYdkJDe6oUPxm9ugR+mio5EL3g==";

            //dev. all permits. mpk222.
            //pospolicy.TicketechNG.NGService.ServiceUrl.Value = "https://mpk.parkingdreams.app";
            //pospolicy.TicketechNG.CoreApiKey.Value = "_HD1.0_+ZW3lZZ2ov4T1vQuhtvm6XskR9rrt2m9cUQHiV+Kl3MtJKpoizj6apHZhpziQ2ADhwpoD+qF3GzYgTfU4mYWgOH3PQXFIW4j5++jsQ5L4PygyDaF5zzoBXScTUKLo4+xfaLH9Nm12W5EFBbXGh6IdUQ4o/2nAg0vzo6bGQ8OkLF+ZjwgD5SJ2Q==";

            FPAPolicy.SetPosPolicies(pospolicy);

            //add avr to loc mach config ///
            //string cameraIP = "10.30.50.106";
            //string direction = "ENTRY";
            //string workstationId = PosProxy.Workstations.SetAVRFlash(direction, cameraIP);


            string inWkid = "AVR079";
            string outWkid = "AVR073";
            string cameraIP = "10.30.50.106";
            PosProxy.WorkstationsProxy.ClearAVRFlash();
            PosProxy.WorkstationsProxy.AddAVRFlash(inWkid, "ENTRY", cameraIP);
            PosProxy.WorkstationsProxy.AddAVRFlash(outWkid, "EXIT", cameraIP);


            PosProxy.SyncQueue.Clear();
            PosProxy.StaysProxy.VoidOpen();

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

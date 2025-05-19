using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Threading;
using System.Threading.Tasks;
using TkMqttBroker.WinService.Brokers.FlashPosAvr;

namespace TkMqttBroker.WinService.Test.Brokers.FlashPosAvr
{
    [TestClass]
    public class OfficeTest
    {
        //test fvr camera in ticketech office

        [TestInitialize]
        public void InitializeTest()
        {
            FlashPosAvrInitializer.Initialize();
        }



        [TestMethod]
        public void TestOfficeCamera()
        {

            //A. read camera
            FlashPosAvrCameraConfiguration configuration = new FlashPosAvrCameraConfiguration
            {
                //IP = "10.30.50.106", // "broker.hivemq.com"
                Port = 1884,
            };
            var reader = new FlashPosAvrReader(configuration);

            Task.Run(async () => 
            {
                await reader.Start();

                Thread.Sleep(300000);

                await reader.Stop();
            }).Wait();

        }
    }
}

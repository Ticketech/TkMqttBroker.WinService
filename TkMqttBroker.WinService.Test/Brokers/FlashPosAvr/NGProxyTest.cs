using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Threading.Tasks;
using TkMqttBroker.WinService.Brokers.FlashPosAvr;

namespace TkMqttBroker.WinService.Test.Brokers.FlashPosAvr
{
    [TestClass]
    public class NGProxyTest
    {
        [TestMethod]
        public void TestGetGarage()
        {
            //test theproxy can get the garage identifier

            FPAInitializer.Initialize();

            FPANGProxy client = new FPANGProxy();

            string gid = null;

            Task.Run(async () => 
            {
                gid = await client.GarageIdentifier();
            }).Wait();

            Assert.IsNotNull(gid);

            Guid res;
            Assert.IsTrue(Guid.TryParse(gid, out res));
            
        }

    }
}

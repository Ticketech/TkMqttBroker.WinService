using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Tk.ConfigurationManager;
using Tk.NetTiers;
using Tk.Utilities.Log4Net;
using TkMqttBroker.WinService.Brokers.FlashPosAvr;
using TkMqttBroker.WinService.Test.Proxies;

namespace TkMqttBroker.WinService.Test.Brokers.FlashPosAvr
{
    [TestClass]
    public class ConsumerTest
    {
        public static log4net.ITktLog logger = log4net.TktLogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);


        [TestInitialize]
        public void InitializeTest()
        {
            FlashPosAvrInitializer.Initialize();
        }


        [TestMethod]
        public void TestConsume()
        {

            var consumer = new FlashPosAvrConsumer();


            //A.
            Proxies.PosProxy.SyncQueue.Clear();
            SyncQueues sync = Proxies.PosProxy.SyncQueue.AddFlashPosAvr();

            Task.Run(async () =>
            {
                await consumer.Start();

                Thread.Sleep(30000);
            }).Wait();

            sync = Proxies.PosProxy.SyncQueue.GetLatest();

            Assert.IsNotNull(sync.SynqSyncDate);
        }
    }
}

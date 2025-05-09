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
            //log4net
            Log4NetHelper.Init();
            Tk.NetTiers.DataAccessLayer.TransactionManager trmgr = Tk.NetTiers.DataAccessLayer.DataRepository.Provider.CreateTransaction();
            Log4NetHelper.setAdoNetAppenderConnection(trmgr.ConnectionString);
            ConfigFileSections configFileSections = (ConfigFileSections)System.Configuration.ConfigurationManager.GetSection("currentLocationGUID");
            Log4NetHelper.setLocationGuid(new Guid(configFileSections.CurrentLocationGUID));
            Log4NetHelper.setSoftwareVersion(Assembly.GetAssembly(typeof(FlashPosAvrService)).GetName().Version.ToString());
            trmgr.Dispose();
        }


        [TestMethod]
        public void TestConsume()
        {

            var consumer = new FlashPosAvrConsumer();


            //A.
            PosProxy.SyncQueue.Clear();
            SyncQueues sync = PosProxy.SyncQueue.AddFlashPosAvr();

            Task.Run(async () =>
            {
                await consumer.Start();

                Thread.Sleep(30000);
            }).Wait();

            sync = PosProxy.SyncQueue.GetLatest();

            Assert.IsNotNull(sync.SynqSyncDate);
        }
    }
}

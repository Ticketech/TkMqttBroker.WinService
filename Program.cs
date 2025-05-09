using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;
using Tk.ConfigurationManager;
using Tk.Utilities.Log4Net;
using TkMqttBroker.WinService.Brokers.FlashPosAvr;

namespace TkMqttBroker.WinService
{
    //gmz.next. created.
    static class Program
    {
        public static log4net.ITktLog logger = log4net.TktLogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);


        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        static void Main()
        {
            //log4net
            Log4NetHelper.Init();
            Tk.NetTiers.DataAccessLayer.TransactionManager trmgr = Tk.NetTiers.DataAccessLayer.DataRepository.Provider.CreateTransaction();
            Log4NetHelper.setAdoNetAppenderConnection(trmgr.ConnectionString);
            ConfigFileSections configFileSections = (ConfigFileSections)System.Configuration.ConfigurationManager.GetSection("currentLocationGUID");
            Log4NetHelper.setLocationGuid(new Guid(configFileSections.CurrentLocationGUID));
            Log4NetHelper.setSoftwareVersion(Assembly.GetAssembly(typeof(FlashPosAvrService)).GetName().Version.ToString());
            trmgr.Dispose();


            ServiceBase[] ServicesToRun;
            ServicesToRun = new ServiceBase[]
            {
                new FlashPosAvrService()
            };
            ServiceBase.Run(ServicesToRun);
        }
    }
}

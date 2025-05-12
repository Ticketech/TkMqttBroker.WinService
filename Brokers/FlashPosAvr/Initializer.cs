using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Tk.ConfigurationManager;
using Tk.NetTiers;
using Tk.NetTiers.DataAccessLayer;
using Tk.Utilities.Log4Net;

namespace TkMqttBroker.WinService.Brokers.FlashPosAvr
{
    public static class FlashPosAvrInitializer
    {
        static readonly log4net.ITktLog logger = log4net.TktLogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);


        public static void Initialize()
        {
            InitializeLog4Net();
            InitializePos();
        }


        private static void InitializeLog4Net()
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


        private static void InitializePos()
        {
            try
            {
                Locations locations = Tk.ConfigurationManager.TkConfigurationManager.GetLocationsFromConfigFile();

                if (locations == null)
                {
                    int hasElemts = 0;

                    locations = DataRepository.LocationsProvider.GetPaged(0, 1, out hasElemts)[0];
                }

                TkConfigurationManager.CurrentLocationGUID = locations.LocationGUID;
                TkConfigurationManager.CurrentCompanyGUID = locations.CompanyGUID;
                TkConfigurationManager.CurrentLocationCode = locations.LocationCode.Trim(); //gmz.33.0.
                TkConfigurationManager.CurrentLocationId = locations.LocationId.Trim(); //gmz.33.0.

            }
            catch (Exception e)
            {
                logger.Error(e.Message, "InitializerManager Error", e);
                throw;
            }
        }

    }
}

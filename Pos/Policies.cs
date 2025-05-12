using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tk.Business.Policies;
using Tk.ConfigurationManager;
using Tk.NetTiers.DataAccessLayer;
using TkMqttBroker.WinService.Brokers.FlashPosAvr;
using TKDEV = Tk.ConfigurationManager.DevicesConfiguration;


namespace TkMqttBroker.WinService.Pos
{
    public static class PosPolicies
    {
        public static LocationPolicies GetCurrentPolicies()
        {
            int count = 0;
            var polloc = DataRepository.PoliciesLocationsProvider.GetPaged(
                $"policyversion = 0 and getdate() between effectivefrom and effectiveto"
                , $"effectivefrom desc", 0, 1, out count).First();

            var policy = JsonConvert.DeserializeObject<LocationPolicies>(polloc.PolicyValue);

            return policy;
        }


        public static FlashAvrProducerConfiguration GetBrokerConfiguration()
        {
            var config = global::TkMqttBroker.WinService.Properties.TkMqttBorker.Default;

            return new FlashAvrProducerConfiguration
            {
                ClientId = config.BrokerClientId,
                Password = config.CameraPassword,
                Port = config.CameraPort,
                Topic = config.BrokerTopic,
                Username = config.CameraUsername,
            };
        }


        public static string LocationId()
        {
            return (string)DataRepository.Provider.ExecuteScalar($@"
select top 1 locationid
from versions ver, locations loc
where ver.locationguid = loc.locationguid"
                );
        }


        //list of camera ips
        internal static List<TKDEV.DeviceConfiguration> GetPosAvrConfigurations()
        {
            List<TKDEV.DeviceConfiguration> configs = new List<TKDEV.DeviceConfiguration>();

            foreach (var workstationId in TkConfigurationManager.GetWorkstations())
            {
                foreach(var device in TkConfigurationManager.GetDevices(workstationId))
                {
                    if (device.Type == "AVR" && device.Model == "FlashAvr")
                    {
                        configs.Add(device);
                    }
                }
            }

            return configs;
        }
    }
}

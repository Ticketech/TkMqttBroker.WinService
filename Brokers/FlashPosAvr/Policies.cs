using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tk.Business.Policies;
using Tk.ConfigurationManager;
using Tk.NetTiers;
using Tk.NetTiers.DataAccessLayer;
using Tk.Serialization;
using TkMqttBroker.WinService.Brokers.FlashPosAvr;
using TKDEV = Tk.ConfigurationManager.DevicesConfiguration;


namespace TkMqttBroker.WinService.Brokers.FlashPosAvr
{
    public static class FlashPosAvrPolicies
    {
        static readonly log4net.ITktLog logger = log4net.TktLogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);


        public static LocationPolicies GetCurrentPolicies()
        {
            
            int count = 0;
            var polloc = DataRepository.PoliciesLocationsProvider.GetPaged(
                $"policyversion = 0 and getdate() between effectivefrom and effectiveto"
                , $"effectivefrom desc", 0, 1, out count).First();

            var policy = DeserializeLocationPoliciesFromXML(polloc.PolicyValue);
                //.Replace("<?xml version=\"1.0\" encoding=\"utf-16\"?>\r\n", "")
                //.Replace(" xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\" xmlns:xsd=\"http://www.w3.org/2001/XMLSchema\"","")
                //);

            return policy;


            LocationPolicies DeserializeLocationPoliciesFromXML(string xml)
            {
                LocationPolicies policies = Serializer.CustomXmlDeserialize<LocationPolicies>(xml);

                policies.Initialize();

                policies.InitializeDefaults();

                return policies;
            }

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
            return (string)DataRepository.Provider.ExecuteScalar(CommandType.Text, $@"
select top 1 locationid
from versions ver, locations loc
where ver.locationguid = loc.locationguid"
                );
        }


        //list of camera ips
        public static List<TKDEV.DeviceConfiguration> GetPosAvrConfigurations()
        {
            List<TKDEV.DeviceConfiguration> configs = new List<TKDEV.DeviceConfiguration>();

            foreach (var workstationId in TkConfigurationManager.GetWorkstations())
            {
                foreach(var device in TkConfigurationManager.GetDevices(workstationId))
                {
                    if (device.Type == "AVR" && device.Model == "AVRFlash")
                    {
                        configs.Add(device);
                    }
                }
            }

            return configs;
        }
    }
}

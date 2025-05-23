using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Net;
using System.Net.Sockets;
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


    public static class FlashPosAvrPolicy
    {
        static readonly log4net.ITktLog logger = log4net.TktLogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        private static readonly FlashPosAvrBrokerConfiguration _brokerPolicies;



        static FlashPosAvrPolicy()
        {
            _brokerPolicies = GetBrokerPolicies();
        }


        public static FlashPosAvrBrokerConfiguration BrokerPolicies
        {
            get { return (FlashPosAvrBrokerConfiguration)_brokerPolicies.Clone(); }
        }


        public static LocationPolicies GetPosPolicies()
        {
            try
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
            catch(Exception ex)
            {
                logger.Error("Error initializing policies", "Init Policies", ex);
                throw;
            }

        }


        public static FlashPosAvrBrokerConfiguration GetBrokerPolicies()
        {
            var config = global::TkMqttBroker.WinService.Properties.FlashPosAvr.Default;

            var policy = new FlashPosAvrBrokerConfiguration
            {
                CameraPort = config.CameraPort,

                ColorConfidenceMin = config.ColorConfidenceMin,
                MakeConfidenceMin = config.MakeConfidenceMin,
                PlateConfidenceMin = config.PlateConfidenceMin,
                StateConfidenceMin = config.StateConfidenceMin,

                ClientId = $"tkt:fpa-broker:{TkConfigurationManager.CurrentLocationId}",

                SoftwareVersion = System.Reflection.Assembly.GetAssembly(typeof(FlashPosAvrService)).GetName().Version.ToString(),
            };

            var pospolicy = GetPosPolicies();

            var state = DataRepository.StatesProvider.GetByStateGUID(pospolicy.Data.StateGUID);
            policy.DefaultStateCode = state.StateCode;

            policy.NGServiceUrl = pospolicy.TicketechNG.NGService.ServiceUrl.Value;
            policy.NGApiKey = ConfigurationDecrypter.DecryptValueWithHeader(pospolicy.TicketechNG.CoreApiKey.Value);

            return policy;
        }


        public static string StateCode(string name)
        {
            var state = DataRepository.StatesProvider.GetByStateName(name);

            return state?.StateCode;
        }


        //list of camera ips
        public static List<FlashPosAvrCameraConfiguration> GetCameraConfigurations()
        {
            List<FlashPosAvrCameraConfiguration> configs = new List<FlashPosAvrCameraConfiguration>();

            foreach (var workstationId in TkConfigurationManager.GetWorkstations())
            {
                foreach(var device in TkConfigurationManager.GetDevices(workstationId))
                {
                    if (device.Type == "AVR" && device.Model == "AVRFlash")
                    {
                        try
                        {
                            configs.Add(new FlashPosAvrCameraConfiguration
                            {
                                WorkstationId = workstationId,
                                IP = device.Location,
                                Direction = (FPADirection)Enum.Parse(typeof(FPADirection), device.SpoolerPrefix.ToUpper()), //entry or exit
                            });
                        }
                        catch(Exception ex)
                        {
                            logger.Error("Error getting camer configuration", "Get Camera Configuration", $"WorkstationId:{workstationId},Error:{ex}");
                        }
                    }
                }
            }

            return configs;
        }


        //https://stackoverflow.com/questions/6803073/get-local-ip-address
        public static string GetLocalIPAddress()
        {
            string localIp = null;

            try
            {
                var host = Dns.GetHostEntry(Dns.GetHostName());
                foreach (var ip in host.AddressList)
                {
                    if (ip.AddressFamily == AddressFamily.InterNetwork)
                    {
                        localIp = ip.ToString();
                    }
                }
            }
            catch(Exception ex)
            {
                logger.Error("Error getting local ip", "Get Local IP", ex);
            }

            return localIp;
        }
    }
}

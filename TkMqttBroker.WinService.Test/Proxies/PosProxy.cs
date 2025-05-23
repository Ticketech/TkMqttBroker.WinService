using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tk.NetTiers;
using Tk.NetTiers.DataAccessLayer;
using System.Data;
using Newtonsoft.Json;
using Tk.Services.REST.Models.Stays;
using System.Data;


namespace TkMqttBroker.WinService.Test.Proxies
{
    public static class PosProxy
    {


        public static class Workstations
        {
            public static void AddAVRFlash(string workstationNumber = "077", string direction = "ENTRY")
            {
                DataRepository.Provider.ExecuteNonQuery(CommandType.Text, $@"
delete from LocationsMachinesConfigurations 
where MachineName = 'FLASH{workstationNumber}'
");

                DataRepository.Provider.ExecuteNonQuery(CommandType.Text, $@"
insert into LocationsMachinesConfigurations
select LocationGUID, 'FLASH{workstationNumber}', LocationMachineConfigurationSection, ConfigurationTypeCode, MachineConfigurationValue, SoftwareVersion
from LocationsMachinesConfigurations
where MachineName = 'avr070'
and ConfigurationTypeCode = 0;"
);


                DataRepository.Provider.ExecuteNonQuery(CommandType.Text, $@"
update LocationsMachinesConfigurations
set MachineConfigurationValue = '<posDevices> <device name=""AVR"" type=""AVR"" model=""AVRFlash"" required=""false"" location=""192.168.1.10"" spoolerPrefix=""{direction}"" /></posDevices>'
where MachineName = 'FLASH{workstationNumber}' and LocationMachineConfigurationSection = 'posdevices'"
);
            }
        }



        public static class SyncQueue
        {
            private static string FlashPosAvrDataType = "FlashPosAvrBroker";

            public static SyncQueues GetLatest()
            {
                int count = 0;
                return DataRepository.SyncQueuesProvider.GetPaged(null, $"timestamp desc", 0, 1, out count)
                    .FirstOrDefault();
            }

            public static int Count()
            {
                return DataRepository.SyncQueuesProvider.GetAll().Count();
            }

            internal static void Clear()
            {
                DataRepository.Provider.ExecuteNonQuery(CommandType.Text, $"delete from syncqueues");
            }

            internal static SyncQueues AddFlashPosAvr()
            {
                SyncQueues sync = new SyncQueues
                {
                    SynqCount = 0,
                    SynqData = JsonConvert.SerializeObject(new CheckInRequest()),
                    SynqDataType = FlashPosAvrDataType,
                    Timestamp = DateTime.Now,
                    SoftwareVersion = "fpv.01.001",
                };

                DataRepository.SyncQueuesProvider.Insert(sync);

                return sync;
            }
        }
    }
}

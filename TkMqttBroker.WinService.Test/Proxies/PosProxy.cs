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

namespace TkMqttBroker.WinService.Test.Proxies
{
    public static class PosProxy
    {

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

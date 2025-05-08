using System;
using System.Data.SqlClient;
using System.Threading.Tasks;
using Tk.Services.REST.Models.Stays;
using Newtonsoft.Json;
using System.Data;
using System.Configuration;
using Tk.NetTiers.DataAccessLayer;
using Tk.NetTiers;

namespace TkMqttBroker.WinService.Brokers.FlashPosAvr
{
    public class FlashPosAvrRepository
    {
        private readonly string _dataType; //table syncqueues
        private readonly string _softwareVersion;


        public FlashPosAvrRepository()
        {
            _softwareVersion = System.Reflection.Assembly.GetAssembly(
                typeof(FlashPosAvrService)).GetName().Version.ToString();

            _dataType = "FlashPosAvrBroker";
        }


        ~FlashPosAvrRepository()
        {
        }


        public async Task Save(CheckInRequest data)
        {
            await Task.Run(async () =>
            {
                DateTime currentTime = await GetServerTime();

                SyncQueues ntdata = new SyncQueues
                {
                    SynqCount = 0,
                    SynqData = JsonConvert.SerializeObject(data),
                    SynqDataType = _dataType,
                    SynqSyncDate = currentTime,
                    SoftwareVersion = _softwareVersion,
                    Timestamp = currentTime,
                };

                DataRepository.SyncQueuesProvider.Insert(ntdata);
            });
        }


        private async Task<DateTime> GetServerTime()
        {
            DateTime time = DateTime.Now;

            await Task.Run(async () =>
            {
                time =  (DateTime)DataRepository.Provider.ExecuteScalar($@"select getdatetime()");
            });

            return time;
        }


        public async Task<CheckInRequest> GetUnsync()
        {
            CheckInRequest data = null;

            string query = $@"
select top 1 synqsyncdata
from syncqueues
where synqsyncdate is null
order by timestamp desc
";

            await Task.Run(async () =>
            {
                var ds = DataRepository.Provider.ExecuteDataSet(CommandType.Text, query);

                if (ds.Tables[0].Rows.Count > 0)
                {
                    data = JsonConvert.DeserializeObject<CheckInRequest>(ds.Tables[0].Rows[0]["synqsyncdata"].ToString());
                }
            });

            return data;
        }


        
    }
}
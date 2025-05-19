using System;
using System.Data.SqlClient;
using System.Threading.Tasks;
using Tk.Services.REST.Models.Stays;
using Newtonsoft.Json;
using System.Data;
using System.Configuration;
using Tk.NetTiers.DataAccessLayer;
using Tk.NetTiers;
using System.Collections.Generic;

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


        public async Task Add(CheckInRequest data)
        {
            await Task.Run((Func<Task>)(async () =>
            {
                DateTime currentTime = await this.GetServerTime();

                SyncQueues ntdata = new SyncQueues
                {
                    SynqCount = 0,
                    SynqData = JsonConvert.SerializeObject(data),
                    SynqDataType = _dataType,
                    SynqSyncDate = null,
                    SoftwareVersion = _softwareVersion,
                    Timestamp = currentTime,
                };

                DataRepository.SyncQueuesProvider.Insert(ntdata);
            }));
        }


        private async Task<DateTime> GetServerTime()
        {
            DateTime time = DateTime.Now;

            await Task.Run(async () =>
            {
                time =  (DateTime)DataRepository.Provider.ExecuteScalar(CommandType.Text, $@"select getdate()");
            });

            return time;
        }


        //fifo order, only 3 tries
        public async Task<TList<SyncQueues>> GetUnsync()
        {
            TList<SyncQueues> syncList = new TList<SyncQueues>();

            await Task.Run(async () =>
            {
                int count = 0;
                syncList = DataRepository.SyncQueuesProvider.GetPaged(
                    $"synqdatatype = '{_dataType}' and synqsyncdate is null and synqcount < 3", $"timestamp asc", 0, 10, out count);
            });

            return syncList;
        }

        internal async Task SetSynced(SyncQueues sync)
        {
            await Task.Run((Func<Task>)(async () =>
            {
                sync.SynqSyncDate = await this.GetServerTime();
                sync.SynqCount += 1;

                sync.EntityState = EntityState.Changed;
                DataRepository.SyncQueuesProvider.Save(sync);
            }));
        }


        internal async Task SetSyncFailed(SyncQueues sync)
        {
            await Task.Run((Func<Task>)(async () =>
            {
                sync.SynqCount++;
                //add last sync date???

                sync.EntityState = EntityState.Changed;
                DataRepository.SyncQueuesProvider.Save(sync);
            }));
        }
    }
}
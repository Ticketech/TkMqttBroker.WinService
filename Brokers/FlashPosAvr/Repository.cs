﻿using System;
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
            await Task.Run((Func<Task>)(async () =>
            {
                DateTime currentTime = await this.GetServerTime();

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


        //fifo order
        public async Task<SyncQueues> GetUnsync()
        {
            SyncQueues data = null;

            await Task.Run(async () =>
            {
                int count = 0;
                var sync = DataRepository.SyncQueuesProvider.GetPaged(
                    $"synqdatatype = '{_dataType}' and synqsyncdate is null", $"timestamp asc", 0, 1, out count);

                if (count > 0)
                    data = sync[0];
            });

            return data;
        }

        internal async Task SetSynced(SyncQueues sync)
        {
            await Task.Run((Func<Task>)(async () =>
            {
                sync.SynqSyncDate = await this.GetServerTime();
                sync.SynqCount++;

                sync.EntityState = EntityState.Changed;
                DataRepository.SyncQueuesProvider.Save(sync);
            }));
        }

     
    }
}
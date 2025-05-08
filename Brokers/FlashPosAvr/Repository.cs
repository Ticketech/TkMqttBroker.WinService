using System;
using System.Data.SqlClient;
using System.Threading.Tasks;
using Tk.Services.REST.Models.Stays;
using Newtonsoft.Json;
using System.Data;
using System.Configuration;

namespace TkMqttBroker.WinService.Brokers.FlashPosAvr
{
    public class FlashPosAvrRepository
    {
        private readonly string _dataType; //table syncqueues
        private readonly string _softwareVersion;
        private readonly SqlConnection _sqlConnection;


        public FlashPosAvrRepository()
        {
            _softwareVersion = System.Reflection.Assembly.GetAssembly(
                typeof(FlashPosAvrService)).GetName().Version.ToString();

            _dataType = "FlashPosAvrBroker";

            _sqlConnection = new SqlConnection(ConfigurationManager.AppSettings["PosDbConnection"]);
            _sqlConnection.Open();
        }


        ~FlashPosAvrRepository()
        {
            _sqlConnection.Close();
        }


        public async Task Save(CheckInRequest data)
        {
            DateTime currentTime = await GetServerTime();

           SqlCommand cmd = new SqlCommand();
            cmd.CommandType = System.Data.CommandType.Text;

            cmd.CommandText = $@"
INSERT INTO [dbo].[SyncQueues]
           [SynqDataType]
           ,[SynqData]
           ,[Timestamp]
           ,[SynqCount]
           ,[SynqSyncDate]
           ,[SoftwareVersion])
     VALUES
           {_dataType}
           ,{JsonConvert.SerializeObject(data)}
           ,{currentTime}
           ,{0}
           ,{currentTime}
           ,{_softwareVersion})
";
            cmd.Connection = _sqlConnection;

            cmd.ExecuteNonQuery();
            _sqlConnection.Close();
        }


        private async Task<DateTime> GetServerTime()
        {
            SqlCommand cmd = new SqlCommand();

            cmd.CommandType = System.Data.CommandType.Text;
            cmd.CommandText = $@"select getdatetime()";
            cmd.Connection = _sqlConnection;

            return (DateTime)(await cmd.ExecuteScalarAsync());
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
                DataSet ds = new DataSet();
                SqlDataAdapter adapter = new SqlDataAdapter(query, _sqlConnection);
                adapter.Fill(ds);

                if (ds.Tables[0].Rows.Count > 0)
                {
                    data = JsonConvert.DeserializeObject<CheckInRequest>(ds.Tables[0].Rows[0]["synqsyncdata"].ToString());
                }
            });

            return data;
        }


        
    }
}
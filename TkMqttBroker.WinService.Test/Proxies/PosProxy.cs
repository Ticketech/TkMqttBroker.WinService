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
using Tk.BridgeComponent.KernelData.Data;
using Tk.ConfigurationManager;

namespace TkMqttBroker.WinService.Test.Proxies
{
    public static class PosProxy
    {

        public static class MPSProxy
        {
            public static MPS NewTag(string locationId = null)
            {
                string testId = "04"; //change if problems with duplicates, increase by one, use hexa, etc
                var tag = new MPS
                {
                    AccountNumber = $"{testId}{DateTime.Now:HHmmssff}",
                    AccountStatus = (byte)CustomerInfo.CustomerStatusEnum.Normal,
                    AccountType = 0,
                    AlternateColor = "RED",
                    AlternateMake = "FORD",
                    AlternatePlate = $"2{DateTime.Now:ffffss}", //numeric
                    VehicleSizeAlternate = (byte)VehicleIdentification.VehicleSize.Standard,
                    AllowCharge = true,
                    BalanceDue = 0,
                    BalanceDueDate = DateTime.Now,
                    Color = "BLUE",
                    Make = "DODGE",
                    Plate = $"1{DateTime.Now:ffffss}", //numeric
                    VehicleSize = (byte)VehicleIdentification.VehicleSize.Standard,
                    CustomerName = "Customer",
                    Deleted = null,
                    ExitFee = 0,
                    LastPaymentAmount = null,
                    LastPaymentDate = null,
                    LastPaymentWhere = null,
                    LocationId = locationId == null ? TkConfigurationManager.CurrentLocationId : locationId, //gmz.58.20.
                    MonthlyCardNumber = null,
                    MTDAmount = 0,
                    MTDPeriod = null,
                    PaymentStatus = (byte)CustomerInfo.CustomerStatusEnum.Normal,
                    PhoneNumber = "1234567890",
                    RateDescription = null,
                    RFIDNumber = Convert.ToInt64($"{testId}9{DateTime.Now:HHmmssff}"),
                    TagNumber = $"{testId}{DateTime.Now:mmssff}",
                    Timestamp = DateTime.Now,
                    VinNumber = null,

                    AlternateVehicleGUID = Guid.NewGuid(),
                    CustomerGUID = Guid.NewGuid(),
                    VehicleGUID = Guid.NewGuid(),
                    VehicleTagGUID = Guid.NewGuid(),

                };

                tag.RFIDNumber = Convert.ToInt64($"9{tag.TagNumber}");
                tag.MpsID = tag.RFIDNumber.Value;

                tag.CustomerName += $" {tag.AccountNumber}";

                return tag;
            }


            public static MPS AddTagToPOS(string locationId = null)
            {
                var mps = NewTag(locationId);
                AddTagToPOS(mps);

                return mps;
            }

            public static MPS AddTagToPOS(MPS mps)
            {
                DataRepository.MPSProvider.Insert(mps);

                try
                {
                    DataRepository.Provider.ExecuteNonQuery(CommandType.Text, "drop table TmpMDBMPS");
                }
                catch { }

                bool? processOK = true;
                DataRepository.ReplicationsProvider.ImportAPiMPS(TkConfigurationManager.CurrentLocationGUID, ref processOK);

                return mps;
            }

        }


        public static class StaysProxy
        {
            public static Stays GetLatest()
            {
                int count = 0;
                return DataRepository.StaysProvider.GetPaged("stayvoided = 0", "staydatein desc", 0, 1, out count).FirstOrDefault();
            }
        }



        public static class WorkstationsProxy
        {
            //eliminate avrflash devices, change to avr
            public static void ClearAVRFlash()
            {
                DataRepository.Provider.ExecuteNonQuery(CommandType.Text, $@"
update locationsmachinesconfigurations
set MachineConfigurationValue = replace(MachineConfigurationValue,'avrflash', 'AVR')
");
            }


            public static void Delete(string workstationId)
            {
                DataRepository.Provider.ExecuteNonQuery(CommandType.Text, $@"
delete from LocationsMachinesConfigurations 
where MachineName = '{workstationId}'
");
            }

            public static string AddAVRFlash(string workstationId = "AVR079", string direction = "ENTRY", string ip = "127.0.0.1")
            {
                Delete(workstationId);

                DataRepository.Provider.ExecuteNonQuery(CommandType.Text, $@"
insert into LocationsMachinesConfigurations
select LocationGUID, '{workstationId}', LocationMachineConfigurationSection, ConfigurationTypeCode, MachineConfigurationValue, SoftwareVersion
from LocationsMachinesConfigurations
where MachineName = 'avr070'
and ConfigurationTypeCode = 0;"
);


                DataRepository.Provider.ExecuteNonQuery(CommandType.Text, $@"
update LocationsMachinesConfigurations
set MachineConfigurationValue = '<posDevices> <device name=""AVR"" type=""AVR"" model=""AVRFlash"" required=""false"" location=""{ip}"" spoolerPrefix=""{direction}"" /></posDevices>'
where MachineName = '{workstationId}' and LocationMachineConfigurationSection = 'posdevices'"
);

                return workstationId;
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

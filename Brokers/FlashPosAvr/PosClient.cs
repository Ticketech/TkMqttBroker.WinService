using System;
using System.Threading.Tasks;
using Tk.Services.REST.Models.Stays;

namespace TkMqttBroker.WinService.Brokers.FlashPosAvr
{
    public class PosClient
    {
        public PosClient()
        {
        }

        internal Task CheckInOutAVR(CheckInRequest avrData)
        {
            throw new NotImplementedException();
        }
    }
}
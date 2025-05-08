using System;
using System.Threading.Tasks;
using Tk.Services.REST.Models.Stays;

namespace TkMqttBroker.WinService.Brokers.FlashPosAvr
{
    internal class NGClient
    {
        public NGClient()
        {
        }

        internal Task Send(CheckInRequest avrData)
        {
            throw new NotImplementedException();
        }
    }
}
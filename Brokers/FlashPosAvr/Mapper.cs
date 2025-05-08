using MQTTnet;
using System;
using Tk.Services.REST.Models.Stays;

namespace TkMqttBroker.WinService.Brokers.FlashPosAvr
{
    internal class FlashPosAvrMapper
    {
        public FlashPosAvrMapper()
        {
        }

        internal CheckInRequest PosAvrData(MqttApplicationMessageReceivedEventArgs data)
        {
            throw new NotImplementedException();
        }

        internal NGPostAvrEntryRawRequest NGPostAvrEntryRawRequest(CheckInRequest avrData)
        {
            throw new NotImplementedException();
        }
    }
}
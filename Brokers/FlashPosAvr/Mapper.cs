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

        internal CheckInRequest CheckInRequest(FVRFlashAvrData data)
        {
            return new CheckInRequest();
        }

        internal NGPostAvrEntryRawRequest NGPostAvrEntryRawRequest(CheckInRequest avrData)
        {
            return new NGPostAvrEntryRawRequest();
        }


    }
}
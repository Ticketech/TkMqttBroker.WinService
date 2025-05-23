using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Tk.Services.REST.Models.Stays;
using TkMqttBroker.WinService.Brokers.FlashPosAvr;

namespace TkMqttBroker.WinService.Test.Mockers
{
    public class PosProxyMock : IPosProxy
    {
        private readonly bool _callOk;
        private readonly StayInfo _response;

        public PosProxyMock(bool callOk, StayInfo response)
        {
            _callOk = callOk;
            _response = response;
        }

        public CheckInRequest LastRequest { get; set; }

        public async Task<CheckInResponse> CheckInOutAVR(CheckInRequest avrData)
        {
            LastRequest = avrData;

            await Task.Run(async () =>
            {
                Thread.Sleep(3000);
            });

            return new CheckInResponse
            {
                code = _callOk ? 0 : -1,
                stay = _callOk ? CheckInResponse(avrData) : null,
            };
        }

        private StayInfo CheckInResponse(CheckInRequest avrData)
        {
            return new StayInfo
            {
                checkin_time = _response.checkin_time,
                checkin_wsid = avrData.infoplate.direction == "ENTRY" ? avrData.infoplate.workstation_id : _response.checkin_wsid,
                checkout_time = avrData.infoplate.direction == "ENTRY"? (DateTime?)null : _response.checkout_time,
                checkout_wsid = avrData.infoplate.direction == "ENTRY" ? null : avrData.infoplate.workstation_id,
                plate = avrData.infoplate.plate,
                stay_guid = Guid.NewGuid(),
                stay_type = _response.stay_type,
                tag_number = _response.tag_number,
                security_code = "0000",
                ticket_number = _response.ticket_number,
            };
        }
    }
}

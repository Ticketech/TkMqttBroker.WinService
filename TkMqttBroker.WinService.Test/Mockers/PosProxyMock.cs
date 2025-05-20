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
        private readonly bool _checkinoutavrRes;

        public PosProxyMock(bool checkinoutavrRes)
        {
            _checkinoutavrRes = checkinoutavrRes;
        }


        public async Task<bool> CheckInOutAVR(CheckInRequest avrData)
        {
            await Task.Run(async () =>
            {
                Thread.Sleep(3000);
            });

            return _checkinoutavrRes;
        }
    }
}

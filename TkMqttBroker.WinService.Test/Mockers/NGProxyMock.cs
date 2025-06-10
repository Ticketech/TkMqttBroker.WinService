using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using TkMqttBroker.WinService.Brokers.FlashPosAvr;

namespace TkMqttBroker.WinService.Test.Mockers
{
    public class NGProxyMock : INGProxy
    {
        private bool _sendRes;

        public NGProxyMock(bool sendRes)
        {
            _sendRes = sendRes;
        }


        public async Task<bool> Send(NGPostAvrEntryRequestBody data)
        {
            await Task.Run(async () =>
            {
                Thread.Sleep(1000);
            });

            return _sendRes;
        }

        public void SetSendResult(bool sendRes)
        {
            _sendRes = sendRes;
        }
    }
}

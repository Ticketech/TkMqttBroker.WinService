using System;
using System.Threading;
using System.Threading.Tasks;
using Tk.Services.REST.Models.Stays;

namespace TkMqttBroker.WinService.Brokers.FlashPosAvr
{
    public class PosAvrConsumer
    {
        private readonly FlashPosAvrRepository _repo;
        private readonly NGClient _ng;
        Timer _timer;


        public PosAvrConsumer()
        {
            _repo = new FlashPosAvrRepository();
            _ng = new NGClient();
        }

        public void Start()
        {
            StartTimer();
        }

        private void StartTimer()
        {
            _timer = new Timer(async e => await OnTick(), null, 10000, Timeout.Infinite);
        }

        private async Task OnTick()
        {
            try
            {
                CheckInRequest avrData = null;
                do
                {
                    avrData = await _repo.GetUnsync();

                    if (avrData != null)
                        await _ng.Send(avrData);

                } while (avrData != null);
            }
            finally
            {
                StartTimer();
            }
        }


    }
}
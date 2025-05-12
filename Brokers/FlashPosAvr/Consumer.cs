using Newtonsoft.Json;
using System;
using System.Threading;
using System.Threading.Tasks;
using Tk.Services.REST.Models.Stays;

namespace TkMqttBroker.WinService.Brokers.FlashPosAvr
{
    public class FlashPosAvrConsumer
    {
        private readonly FlashPosAvrRepository _repo;
        private readonly FlashPosAvrNGProxy _ng;
        private readonly FlashPosAvrMapper _mapper;
        Timer _timer;
        private readonly SemaphoreSlim _semaphoreSlim;


        public FlashPosAvrConsumer()
        {
            _repo = new FlashPosAvrRepository();
            _ng = new FlashPosAvrNGProxy();
            _mapper = new FlashPosAvrMapper();
            _semaphoreSlim = new SemaphoreSlim(1);
        }

        public async Task Start()
        {
            StartTimer();
        }

        private void StartTimer()
        {
            _timer = new Timer(async e => await OnTick(), null, 10000, Timeout.Infinite);
        }


        public async Task Stop()
        {
            await _semaphoreSlim.WaitAsync();
            try
            {
                if (_timer != null)
                    _timer.Dispose();
            }
            finally
            {
                _semaphoreSlim.Release();
            }
        }


        private async Task OnTick()
        {
            await _semaphoreSlim.WaitAsync();

            try
            {
                CheckInRequest avrData = null;
                do
                {
                    var sync = await _repo.GetUnsync();

                    if (sync != null)
                    {
                        //todo: if it fails? 

                        avrData =  JsonConvert.DeserializeObject<CheckInRequest>(sync.SynqData);

                        await _ng.Send(_mapper.NGPostAvrEntryRawRequest(avrData));

                        await _repo.SetSynced(sync);
                    }

                } while (avrData != null);
            }
            finally
            {
                StartTimer();
                _semaphoreSlim.Release();
            }
        }

    }
}
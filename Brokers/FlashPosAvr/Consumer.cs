using Newtonsoft.Json;
using System;
using System.Threading;
using System.Threading.Tasks;
using Tk.Services.REST.Models.Stays;

namespace TkMqttBroker.WinService.Brokers.FlashPosAvr
{
    public class FlashPosAvrConsumer
    {
        private static SemaphoreSlim _semaphoreSlim = new SemaphoreSlim(1);

        private readonly FlashPosAvrRepository _repo;
        private readonly FlashPosAvrNGProxy _ng;
        private readonly FlashPosAvrMapper _mapper;


        public FlashPosAvrConsumer()
        {
            _repo = new FlashPosAvrRepository();
            _ng = new FlashPosAvrNGProxy();
            _mapper = new FlashPosAvrMapper();
        }


        public async Task Start()
        {
        }



        public async Task Stop()
        {
            try
            {
                await _semaphoreSlim.WaitAsync();
            }
            finally
            {
                _semaphoreSlim.Release();
            }
        }


        public async Task Sync()
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
                _semaphoreSlim.Release();
            }
        }

    }
}
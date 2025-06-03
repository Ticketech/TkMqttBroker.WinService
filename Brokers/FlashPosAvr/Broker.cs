using MQTTnet.Client;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Tk.Services.REST.Models.Stays;

namespace TkMqttBroker.WinService.Brokers.FlashPosAvr
{
    //gmz.next. created.
    public class FPABroker
    {
        public static log4net.ITktLog logger = log4net.TktLogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        private static SemaphoreSlim _semaphoreSlim = new SemaphoreSlim(1);

        private readonly Dictionary<string,IMqttClientMock> _mqttMocks; //key = workstationid; eg, AVR079
        private readonly FPARepository _repo;
        private readonly INGProxy _ng;
        private readonly IPosProxy _pos;
        private readonly FPAMapper _mapper;

        private List<FPAProducer> _producers = new List<FPAProducer>();
        private FPABrokerConfiguration _configuration;

        Timer _timer;


        public FPABroker()
        {
            _mqttMocks = null;

            _repo = new FPARepository();
            _mapper = new FPAMapper();
            _ng = new FPANGProxy ();

            logger.Info("Broker initialized");
        }


        //for testing
        public FPABroker(Dictionary<string,IMqttClientMock> mqttMocks, IPosProxy posMock, INGProxy ngMock)
        {
            _mqttMocks = mqttMocks;

            _repo = new FPARepository();
            _mapper = new FPAMapper();
            _ng = ngMock;
            _pos = posMock;
        }


        public async Task Start()
        {
            _configuration = FPAPolicy.BrokerPolicies;

            //connect to cameras
            foreach (var cameraConfig in FPAPolicy.GetCameraConfigurations())
            {
                cameraConfig.Port = _configuration.CameraPort;

                FPAProducer producer;
                if (_mqttMocks == null)
                    producer = new FPAProducer(cameraConfig);
                else
                    producer = new FPAProducer(cameraConfig, _mqttMocks[cameraConfig.WorkstationId], _pos);

                _producers.Add(producer);

                await producer.Start();
            }

            StartTimer();

            logger.Info("Broker started");
        }



        private void StartTimer()
        {
            _timer = new Timer(async e => await OnTick(), null, 10000, Timeout.Infinite);
        }


        private async Task OnTick()
        {
            try
            {
                _timer.Dispose();

                //cameras
                foreach(var camera in _producers)
                {
                    if(camera.ReportBlackout())
                    {
                        try
                        {
                            await _semaphoreSlim.WaitAsync();

                            await camera.Reconnect();
                        }
                        finally
                        {
                            _semaphoreSlim.Release();
                        }
                    }
                }

                //ng
                await SyncNG();
            }
            finally
            {
                StartTimer();
            }
        }


        public async Task SyncNG()
        {
            try
            {
                await _semaphoreSlim.WaitAsync();

                CheckInRequest avrData = null;

                var syncList = await _repo.GetUnsync();

                foreach (var sync in syncList)
                {
                    try
                    {
                        avrData = JsonConvert.DeserializeObject<CheckInRequest>(sync.SynqData);
                        bool res = await _ng.Send(_mapper.NGPostAvrEntryRawRequest(avrData));

                        //todo: if it fails? 
                        if (res)
                            await _repo.SetSynced(sync);
                        else
                            await _repo.SetSyncFailed(sync);
                    }
                    catch (Exception ex)
                    {
                        logger.Error("Error syncing an item", "Sync Item", $"SynqGuid:{sync.SynqGUID},Message:{ex}");
                    }
                }
            }
            catch (Exception ex)
            {
                logger.Error("Error syncing the queue", "Sync Queue", ex);
            }
            finally
            {
                _semaphoreSlim.Release();
            }
        }



        public async Task Stop()
        {
            try
            {
                await _semaphoreSlim.WaitAsync();

                if (_timer != null)
                    _timer.Dispose();

                //disconnect from cameras
                foreach (var producer in _producers)
                    await producer.Stop();
            }
            finally
            {
                _semaphoreSlim.Release();
            }

            logger.Info("Broker stopped");
        }

    }

}
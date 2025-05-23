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
    public class FlashPosAvrBroker
    {
        public static log4net.ITktLog logger = log4net.TktLogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        private static SemaphoreSlim _semaphoreSlim = new SemaphoreSlim(1);

        private readonly IMqttClient _mqttClient;
        private readonly FlashPosAvrRepository _repo;
        private readonly INGProxy _ng;
        private readonly IPosProxy _pos;
        private readonly FlashPosAvrMapper _mapper;

        private List<FlashPosAvrProducer> _producers = new List<FlashPosAvrProducer>();
        private FlashPosAvrBrokerConfiguration _configuration;

        Timer _timer;


        public FlashPosAvrBroker()
        {
            _mqttClient = null;

            _repo = new FlashPosAvrRepository();
            _mapper = new FlashPosAvrMapper();
            _ng = new FlashPosAvrNGProxy();

            logger.Info("Broker initialized");
        }


        //for testing
        public FlashPosAvrBroker(IMqttClientMock mqttMock, IPosProxy posMock, INGProxy ngMock)
        {
            _mqttClient = mqttMock;

            _repo = new FlashPosAvrRepository();
            _mapper = new FlashPosAvrMapper();
            _ng = ngMock;
            _pos = posMock;
        }


        public async Task Start()
        {
            _configuration = FlashPosAvrPolicy.BrokerPolicies;

            //connect to cameras
            foreach (var cameraConfig in FlashPosAvrPolicy.GetCameraConfigurations())
            {
                cameraConfig.Port = _configuration.CameraPort;

                FlashPosAvrProducer producer;
                if (_mqttClient == null)
                    producer = new FlashPosAvrProducer(cameraConfig);
                else
                    producer = new FlashPosAvrProducer(cameraConfig, _mqttClient as IMqttClientMock, _pos);

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
                    camera.ReportBlackout();
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
using System;
using System.Collections.Generic;

namespace TkMqttBroker.WinService.Brokers.FlashPosAvr
{
    internal class FlashPosAvrBroker
    {
        private List<FlashAvrProducer> _flashClients = new List<FlashAvrProducer>();
        private PosAvrConsumer _posClient;
        private List<FlashAvrProducerConfiguration> _cameras;

        public FlashPosAvrBroker()
        {
        }

        public void Start()
        {
            //connect to cameras
            foreach(var camera in _cameras)
            {
                var client = new FlashAvrProducer(camera);
                _flashClients.Add(client);

                client.Start();
            }

            //start rest sync
            _posClient = new PosAvrConsumer();
            _posClient.Start();
        }


        public void Stop()
        {
            //stop rest sync

            //disconnect from cameras
        }
    }
}
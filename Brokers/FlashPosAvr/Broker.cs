using System;
using System.Collections.Generic;

namespace TkMqttBroker.WinService.Brokers.FlashPosAvr
{
    internal class FlashPosAvrBroker
    {
        private List<FlashAvrProducer> _producers = new List<FlashAvrProducer>();
        private PosAvrConsumer _consumer;
        private List<FlashAvrProducerConfiguration> _cameraConfigurations;

        public FlashPosAvrBroker()
        {
        }

        public async void Start()
        {
            //connect to cameras
            foreach(var config in _cameraConfigurations)
            {
                var producer = new FlashAvrProducer(config);
                _producers.Add(producer);

                await producer.Start();
            }

            //start rest sync
            _consumer = new PosAvrConsumer();
            await _consumer.Start();
        }


        public async void Stop()
        {
            //stop rest sync
            await _consumer.Stop();

            //disconnect from cameras
            foreach (var producer in _producers)
                await producer.Stop();
        }
    }
}
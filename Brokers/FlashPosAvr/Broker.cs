using System;
using System.Collections.Generic;

namespace TkMqttBroker.WinService.Brokers.FlashPosAvr
{
    internal class FlashPosAvrBroker
    {
        private List<FlashPosAvrProducer> _producers = new List<FlashPosAvrProducer>();
        private FlashPosAvrConsumer _consumer;
        private List<FlashAvrProducerConfiguration> _cameraConfigurations;

        public FlashPosAvrBroker()
        {
        }

        public async void Start()
        {
            //connect to cameras
            foreach(var config in _cameraConfigurations)
            {
                var producer = new FlashPosAvrProducer(config);
                _producers.Add(producer);

                await producer.Start();
            }

            //start rest sync
            _consumer = new FlashPosAvrConsumer();
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
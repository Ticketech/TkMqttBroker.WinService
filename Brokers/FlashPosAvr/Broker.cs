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

        public void Start()
        {
            //connect to cameras
            foreach(var config in _cameraConfigurations)
            {
                var producer = new FlashAvrProducer(config);
                _producers.Add(producer);

                producer.Start();
            }

            //start rest sync
            _consumer = new PosAvrConsumer();
            _consumer.Start();
        }


        public void Stop()
        {
            //stop rest sync

            //disconnect from cameras
        }
    }
}
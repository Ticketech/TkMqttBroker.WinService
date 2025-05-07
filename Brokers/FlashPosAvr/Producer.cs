using System;

namespace TkMqttBroker.WinService.Brokers.FlashPosAvr
{
    internal class FlashAvrProducer
    {
        private FlashAvrProducerConfiguration camera;

        public FlashAvrProducer(FlashAvrProducerConfiguration camera)
        {
            this.camera = camera;
        }

        internal void Start()
        {
            throw new NotImplementedException();
        }
    }



    internal class FlashAvrProducerConfiguration
    {
    }



}
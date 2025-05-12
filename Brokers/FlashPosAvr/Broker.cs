using MQTTnet.Client;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using TkMqttBroker.WinService.Pos;

namespace TkMqttBroker.WinService.Brokers.FlashPosAvr
{
    public class FlashPosAvrBroker
    {
        private readonly IMqttClient _mqttClient;
        private List<FlashPosAvrProducer> _producers = new List<FlashPosAvrProducer>();
        private FlashPosAvrConsumer _consumer;
        private FlashAvrProducerConfiguration _configuration;


        public FlashPosAvrBroker()
        {
            _mqttClient = null;
        }



        //for testing
        public FlashPosAvrBroker(IMqttClient mock)
        {
            _mqttClient = mock;
        }


        public async Task Start()
        {
            _configuration = PosPolicies.GetBrokerConfiguration();

            //connect to cameras
            foreach(var avrConfig in PosPolicies.GetPosAvrConfigurations())
            {
                FlashPosAvrProducer producer;
                if (_mqttClient == null)
                    producer = new FlashPosAvrProducer(_configuration.Clone(avrConfig));
                else
                    producer = new FlashPosAvrProducer(_configuration.Clone(avrConfig), _mqttClient);

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
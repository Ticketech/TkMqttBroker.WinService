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
        private FlashAvrProducerConfiguration _cameraMasterConfig;


        public FlashPosAvrBroker()
        {
            var policy = PosPolicies.GetCurrentPolicies();

            _cameraMasterConfig = new FlashAvrProducerConfiguration
            {
                ClientId = PosPolicies.LocationId(),
                Password = policy.AVR.FlashAvr.Password,
                Port = policy.AVR.FlashAvr.Port,
                Topic = policy.AVR.FlashAvr.Topic,
                Username = policy.AVR.FlashAvr.Username,
            };

            _mqttClient = null;
        }



        //for testing
        public FlashPosAvrBroker(IMqttClient mock)
        {
            _mqttClient = mock;
        }


        public async Task Start()
        {
            //connect to cameras
            foreach(string broker in PosPolicies.GetFlashPosAvrBrokers())
            {
                FlashPosAvrProducer producer;
                if (_mqttClient == null)
                    producer = new FlashPosAvrProducer(_cameraMasterConfig.Clone(broker));
                else
                    producer = new FlashPosAvrProducer(_cameraMasterConfig.Clone(broker), _mqttClient);

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
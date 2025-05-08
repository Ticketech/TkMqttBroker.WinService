using MQTTnet.Client.Subscribing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TkMqttBroker.WinService.Brokers.FlashPosAvr
{

    public class FlashAvrProducerConfiguration
    {
        public string Broker;
        public int? Port;
        public string Username;
        public string Password;
        public string ClientId;
        public MqttClientSubscribeOptions Topic;
    }




}

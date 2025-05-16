using MQTTnet;
using MQTTnet.Client;
using MQTTnet.Client.Connecting;
using MQTTnet.Client.Disconnecting;
using MQTTnet.Client.ExtendedAuthenticationExchange;
using MQTTnet.Client.Options;
using MQTTnet.Client.Publishing;
using MQTTnet.Client.Receiving;
using MQTTnet.Client.Subscribing;
using MQTTnet.Client.Unsubscribing;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using TkMqttBroker.WinService.Brokers.FlashPosAvr;

namespace TkMqttBroker.WinService.Test.Mockers
{


    public class MqttClientMock : IMqttClientMock
    {

        public bool IsConnected => true;

        public IMqttClientOptions Options => null;

        public IMqttClientConnectedHandler ConnectedHandler { get; set; }
        public IMqttClientDisconnectedHandler DisconnectedHandler { get; set;}
        public IMqttApplicationMessageReceivedHandler ApplicationMessageReceivedHandler { get; set; }


        public Func<MqttApplicationMessageReceivedEventArgs, Task> OnUseApplicationMessageReceivedEventAsync;
        private Func<MqttApplicationMessageReceivedEventArgs, Task> _useApplicationMessageReceivedHandler;

        public async Task<MqttClientAuthenticateResult> ConnectAsync(IMqttClientOptions options, CancellationToken cancellationToken)
        {
            return null;
        }

        public async Task DisconnectAsync(MqttClientDisconnectOptions options, CancellationToken cancellationToken)
        {
        }

        public void Dispose()
        {
            return;
        }

        public async Task PingAsync(CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        //public async Task<MqttClientPublishResult> PublishAsync(MqttApplicationMessage applicationMessage, CancellationToken cancellationToken)
        //{
        //    await ApplicationMessageReceivedHandler.HandleApplicationMessageReceivedAsync(
        //        new MqttApplicationMessageReceivedEventArgs("123", applicationMessage));

        //    return null;
        //}


        public async Task<MqttClientPublishResult> PublishAsync(MqttApplicationMessage applicationMessage, CancellationToken cancellationToken)
        {
            await _useApplicationMessageReceivedHandler.Invoke(new MqttApplicationMessageReceivedEventArgs("123", applicationMessage));

            return null;
        }


        public async Task<FVRPayload> PublishSomething()
        {
            var payload = new FVRPayload
            {
                eventType = "testo",
            };

            var data = new MqttApplicationMessageBuilder()
             .WithTopic("detection")
             .WithPayload(JsonConvert.SerializeObject(payload))
             .WithExactlyOnceQoS()  // QoS 2 para entrega exacta
             .WithRetainFlag()  // Conservar el mensaje en el broker
             .Build();

            await PublishAsync(data, CancellationToken.None);

            return payload;
        }

        public async Task SendExtendedAuthenticationExchangeDataAsync(MqttExtendedAuthenticationExchangeData data, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public async Task<MqttClientSubscribeResult> SubscribeAsync(MqttClientSubscribeOptions options, CancellationToken cancellationToken)
        {
            return null;
        }

        public async Task<MqttClientUnsubscribeResult> UnsubscribeAsync(MqttClientUnsubscribeOptions options, CancellationToken cancellationToken)
        {
            return null;
        }

        public void UseApplicationMessageReceivedHandler_Mock(Func<MqttApplicationMessageReceivedEventArgs, Task> handler)
        {
            _useApplicationMessageReceivedHandler = handler;
        }
    }


}

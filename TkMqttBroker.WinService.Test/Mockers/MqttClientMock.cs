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
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace TkMqttBroker.WinService.Test.Mockers
{
    public class MqttClientMock : IMqttClient
    {

        public bool IsConnected => true;

        public IMqttClientOptions Options => null;

        public IMqttClientConnectedHandler ConnectedHandler { get; set; }
        public IMqttClientDisconnectedHandler DisconnectedHandler { get; set;}
        public IMqttApplicationMessageReceivedHandler ApplicationMessageReceivedHandler { get; set; }


        Func<MqttApplicationMessageReceivedEventArgs, Task> func;
        public IMqttClient UseApplicationMessageReceivedHandler(Func<MqttApplicationMessageReceivedEventArgs, Task> handler) 
        {
            func = handler;

            return this;
        }


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

        public async Task<MqttClientPublishResult> PublishAsync(MqttApplicationMessage applicationMessage, CancellationToken cancellationToken)
        {
            await func.Invoke(new MqttApplicationMessageReceivedEventArgs("MockClientId", applicationMessage));

            return null;
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
    }
}

using MQTTnet;
using MQTTnet.Client;
using MQTTnet.Client.Connecting;
using MQTTnet.Client.Options;
using MQTTnet.Client.Receiving;
using MQTTnet.Client.Subscribing;
using System;
using System.Collections.Generic;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Tk.Services.REST.Models.Stays;

namespace TkMqttBroker.WinService.Brokers.FlashPosAvr
{
    //credits: https://docs.google.com/document/d/1meiNhvV4Ooz-nrf5kF48nSXbK1YwT85rgXonwDnlf5o/edit?tab=t.0


    public class FlashAvrProducer: IMqttApplicationMessageReceivedHandler
    {
        private FlashAvrProducerConfiguration _cameraConfiguration;
        private readonly FlashPosAvrRepository _repo;
        private readonly FlashPosAvrMapper _mapper;
        private readonly PosClient _pos;
        private IMqttClient _mqttClient;
        private readonly SemaphoreSlim _semaphoreSlim;


        //for testing
        public FlashAvrProducer(FlashAvrProducerConfiguration configuration, IMqttClient mock)
        {
            _cameraConfiguration = configuration;
            _repo = new FlashPosAvrRepository();
            _mapper = new FlashPosAvrMapper();
            _pos = new PosClient();

            _semaphoreSlim = new SemaphoreSlim(1);

            _mqttClient = mock;
            //_mqttClient.UseApplicationMessageReceivedHandler(async e => await OnUseApplicationMessageReceived(e));
            _mqttClient.ApplicationMessageReceivedHandler = this;
        }




        public FlashAvrProducer(FlashAvrProducerConfiguration configuration)
        {
            _cameraConfiguration = configuration;
            _repo = new FlashPosAvrRepository();
            _mapper = new FlashPosAvrMapper();
            _pos = new PosClient();

            _semaphoreSlim = new SemaphoreSlim(1);
        }


        public async Task Start()
        {
            //create client
            if (_mqttClient != null)
                await CreateProducer();
        }


        public async Task Stop()
        {
            await _semaphoreSlim.WaitAsync();

            try
            {
                await _mqttClient.UnsubscribeAsync(_cameraConfiguration.Topic);
                await _mqttClient.DisconnectAsync();
            }
            finally
            {
                _semaphoreSlim.Release();
            }
        }



        private async Task CreateProducer()
        {
            // Create a MQTT client factory
            var factory = new MqttFactory();

            // Create a MQTT client instance
            _mqttClient = factory.CreateMqttClient();

            // Create MQTT client options
            var options = new MqttClientOptionsBuilder()
                .WithTcpServer(_cameraConfiguration.Broker, _cameraConfiguration.Port) // MQTT broker address and port
                .WithCredentials(_cameraConfiguration.Username, _cameraConfiguration.Password) // Set username and password
                .WithClientId(_cameraConfiguration.ClientId)
                .WithCleanSession()
                .WithTls(
                    o =>
                    {
                        // The used public broker sometimes has invalid certificates. This sample accepts all
                        // certificates. This should not be used in live environments.
                        o.CertificateValidationHandler = _ => true;

                        // The default value is determined by the OS. Set manually to force version.
                        o.SslProtocol = SslProtocols.Tls12; ;

                        // Please provide the file path of your certificate file. The current directory is /bin.
                        var certificate = new X509Certificate("/opt/emqxsl-ca.crt", "");
                        o.Certificates = new List<X509Certificate> { certificate };
                    }
                )
                .Build();

            //connect
            var connectResult = await _mqttClient.ConnectAsync(options);

            //subscribe
            if (connectResult.ResultCode == MqttClientConnectResultCode.Success)
            {
                Console.WriteLine("Connected to MQTT broker successfully.");

                // Subscribe to a topic
                await _mqttClient.SubscribeAsync(_cameraConfiguration.Topic);

                // Callback function when a message is received
                _mqttClient.UseApplicationMessageReceivedHandler(async e => await OnUseApplicationMessageReceived(e));
            }

        }


        //produce data for pos avr
        private async Task OnUseApplicationMessageReceived(MqttApplicationMessageReceivedEventArgs data)
        {
            //// Convertir el payload a una cadena legible
            //var messagePayload = Encoding.UTF8.GetString(e.ApplicationMessage.Payload);
            //Console.WriteLine($"Mensaje recibido en el tema '{e.ApplicationMessage.Topic}': {messagePayload}");

            await _semaphoreSlim.WaitAsync();

            try
            {
                CheckInRequest avrData = _mapper.PosAvrData(data);
                await _repo.Save(avrData);

                await _pos.CheckInOutAVR(avrData);
            }
            finally
            {
                _semaphoreSlim.Release();
            }
        }

        public async Task HandleApplicationMessageReceivedAsync(MqttApplicationMessageReceivedEventArgs eventArgs)
        {
            string hola = "Hello world!";
        }
    }






}
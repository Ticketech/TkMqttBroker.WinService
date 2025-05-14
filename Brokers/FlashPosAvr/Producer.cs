using MQTTnet;
using MQTTnet.Client;
using MQTTnet.Client.Connecting;
using MQTTnet.Client.Options;
using MQTTnet.Client.Receiving;
using MQTTnet.Client.Subscribing;
using Newtonsoft.Json;
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


    public class FlashPosAvrProducer //: IMqttApplicationMessageReceivedHandler
    {
        private FlashPosAvrProducerConfiguration _cameraConfiguration;
        private readonly FlashPosAvrRepository _repo;
        private readonly FlashPosAvrMapper _mapper;
        private readonly FlashPosAvrPosProxy _pos;
        private IMqttClient _mqttClient;
        private readonly SemaphoreSlim _semaphoreSlim;


        //for testing
        public FlashPosAvrProducer(FlashPosAvrProducerConfiguration configuration, IMqttClientMock mock)
        {
            _cameraConfiguration = configuration;
            _repo = new FlashPosAvrRepository();
            _mapper = new FlashPosAvrMapper();
            _pos = new FlashPosAvrPosProxy();

            _semaphoreSlim = new SemaphoreSlim(1);

            mock.UseApplicationMessageReceivedHandler_Mock(async e => await OnUseApplicationMessageReceivedEvent(e));
            _mqttClient = mock;
        }




        public FlashPosAvrProducer(FlashPosAvrProducerConfiguration configuration)
        {
            _cameraConfiguration = configuration;
            _repo = new FlashPosAvrRepository();
            _mapper = new FlashPosAvrMapper();
            _pos = new FlashPosAvrPosProxy();

            _semaphoreSlim = new SemaphoreSlim(1);
        }


        public async Task Start()
        {
            //create client
            if (_mqttClient == null)
                await CreateProducer();
        }


        public async Task Stop()
        {
            await _semaphoreSlim.WaitAsync();

            try
            {
                await _mqttClient.UnsubscribeAsync("detection");
                await _mqttClient.UnsubscribeAsync("heartbeat");
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
                .WithTcpServer(_cameraConfiguration.IP, _cameraConfiguration.Port) // MQTT broker address and port
                //.WithCredentials(_cameraConfiguration.Username, _cameraConfiguration.Password) // Set username and password
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
                        //var certificate = new X509Certificate("/opt/emqxsl-ca.crt", "");
                        //o.Certificates = new List<X509Certificate> { certificate };
                    }
                )
                .Build();

            //connect
            var connectResult = await _mqttClient.ConnectAsync(options, CancellationToken.None);

            //subscribe
            if (connectResult.ResultCode == MqttClientConnectResultCode.Success)
            {
                // Subscribe to a topic
                await _mqttClient.SubscribeAsync("detection");
                await _mqttClient.SubscribeAsync("heartbeat");

                // Callback function when a message is received
                //_mqttClient.ApplicationMessageReceivedHandler = this;
                _mqttClient.UseApplicationMessageReceivedHandler(async e => await OnUseApplicationMessageReceivedEvent(e));
            }

        }


        ////produce data for pos avr
        protected virtual async Task OnUseApplicationMessageReceivedEvent(MqttApplicationMessageReceivedEventArgs e)
        {
            //todo: respond to all events

            try
            {
                await _semaphoreSlim.WaitAsync();

                var topic = e.ApplicationMessage.Topic;
                // Convertir el payload a una cadena legible
                var strPayload = Encoding.UTF8.GetString(e.ApplicationMessage.Payload);

                if (topic == "detection")
                {
                    //ack mqtt

                    CheckInRequest avrData = _mapper.CheckInRequest(
                        JsonConvert.DeserializeObject<FVRFlashAvrData>(strPayload));

                    //save data to sync ng
                    await _repo.Save(avrData);

                    //call pos
                    await _pos.CheckInOutAVR(avrData);
                }
                else if (topic == "heartbeat")
                {
                    //ack
                }
            }
            finally
            {
                _semaphoreSlim.Release();
            }
        }





        //public virtual async Task HandleApplicationMessageReceivedAsync(MqttApplicationMessageReceivedEventArgs data)
        //{
        //    await _semaphoreSlim.WaitAsync();

        //    // Convertir el payload a una cadena legible
        //    var messagePayload = Encoding.UTF8.GetString(data.ApplicationMessage.Payload);
        //    //Console.WriteLine($"Mensaje recibido en el tema '{e.ApplicationMessage.Topic}': {messagePayload}");

        //    try
        //    {
        //        CheckInRequest avrData = _mapper.CheckInRequest(
        //            JsonConvert.DeserializeObject<FVRFlashAvrData>(messagePayload));

        //        await _repo.Save(avrData);

        //        await _pos.CheckInOutAVR(avrData);
        //    }
        //    finally
        //    {
        //        _semaphoreSlim.Release();
        //    }
        //}
    }



    public class FlashPosAvrReader : FlashPosAvrProducer
    {
        public static log4net.ITktLog logger = log4net.TktLogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);


        public FlashPosAvrReader(FlashPosAvrProducerConfiguration configuration)
            : base(configuration)
        { }


        protected override async Task OnUseApplicationMessageReceivedEvent(MqttApplicationMessageReceivedEventArgs data)
        {
            string otherdata = JsonConvert.SerializeObject(data);

            logger.Info("FVR new message", "FVR Message", otherdata);
        }

    }








}
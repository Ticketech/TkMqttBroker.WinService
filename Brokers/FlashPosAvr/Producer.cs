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
using Tk.ConfigurationManager;
using Tk.Services.REST.Models.Stays;

namespace TkMqttBroker.WinService.Brokers.FlashPosAvr
{
    //credits: https://docs.google.com/document/d/1meiNhvV4Ooz-nrf5kF48nSXbK1YwT85rgXonwDnlf5o/edit?tab=t.0


    public class FlashPosAvrProducer //: IMqttApplicationMessageReceivedHandler
    {
        public static log4net.ITktLog logger = log4net.TktLogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        private readonly FlashPosAvrRepository _repo;
        private readonly FlashPosAvrMapper _mapper;
        private readonly IPosProxy _pos;

        private readonly SemaphoreSlim _semaphoreSlim;

        private readonly FlashPosAvrBrokerConfiguration _brokerConfig;
        private readonly FlashPosAvrCameraConfiguration _cameraConfiguration;
        //private readonly string _clientId;

        private IMqttClient _mqttClient;
        private DateTime _lastHearbeat;


        //for testing
        public FlashPosAvrProducer(FlashPosAvrCameraConfiguration camera, IMqttClientMock mqttMock, IPosProxy posMock)
        {
            _brokerConfig = FlashPosAvrPolicy.BrokerPolicies;

            _cameraConfiguration = camera;
            _repo = new FlashPosAvrRepository();
            _mapper = new FlashPosAvrMapper();
            _pos = posMock;

            _semaphoreSlim = new SemaphoreSlim(1);

            mqttMock.UseApplicationMessageReceivedHandler_Mock(async e => await OnUseApplicationMessageReceivedEvent(e));
            _mqttClient = mqttMock;

            _lastHearbeat = DateTime.Now;
        }



        public FlashPosAvrProducer(FlashPosAvrCameraConfiguration camera)
        {
            _brokerConfig = FlashPosAvrPolicy.BrokerPolicies;

            _cameraConfiguration = camera;
            _repo = new FlashPosAvrRepository();
            _mapper = new FlashPosAvrMapper();
            _pos = new FlashPosAvrPosProxy();

            _semaphoreSlim = new SemaphoreSlim(1);

            _lastHearbeat = DateTime.Now;
        }


        public async Task Start()
        {
            //create client
            if (_mqttClient == null)
                await CreateProducer();
        }


        public async Task Stop()
        {
            try
            {
                await _semaphoreSlim.WaitAsync();

                await _mqttClient.UnsubscribeAsync("detection");
                await _mqttClient.UnsubscribeAsync("heartbeat");
                await _mqttClient.DisconnectAsync();
            }
            catch (Exception ex)
            {

            }
            finally
            {
                _semaphoreSlim.Release();
            }
        }



        public void ReportBlackout()
        {
            if (_lastHearbeat < DateTime.Now.AddMinutes(-2))
                logger.Warn($"Possible camera balckuut.", "Camera Balckout", $"Workstation:{_cameraConfiguration.WorkstationId},LastHB:{_lastHearbeat:HH:mm:ss}");
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
                .WithClientId(_brokerConfig.ClientId) //(_clientId)
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
                //await _mqttClient.SubscribeAsync("heartbeat");

                // Callback function when a message is received
                //_mqttClient.ApplicationMessageReceivedHandler = this;
                _mqttClient.UseApplicationMessageReceivedHandler(async e => await OnUseApplicationMessageReceivedEvent(e));
            }

        }


        ////produce data for pos avr
        protected virtual async Task OnUseApplicationMessageReceivedEvent(MqttApplicationMessageReceivedEventArgs e)
        {
            //todo: respond to all events
            const string method = "POS.Rest.CheckInOutAVR";

            try
            {
                await _semaphoreSlim.WaitAsync();

                var topic = e.ApplicationMessage.Topic;
                // Convertir el payload a una cadena legible
                var strPayload = Encoding.UTF8.GetString(e.ApplicationMessage.Payload);

                if (topic == "detection")
                {
                    var payload = JsonConvert.DeserializeObject<FVRPayload>(strPayload);

                    //ack mqtt
                    await _mqttClient.PublishAsync(_mapper.DetectionAck(payload.eventData.encounterId));

                    CheckInRequest avrData = _mapper.CheckInRequest(payload, _cameraConfiguration);

                    //save data to sync ng
                    await _repo.Add(avrData);

                    //check confidence
                    if (avrData.infoplate.confidence >= _brokerConfig.PlateConfidenceMin)
                    {
                        //call pos
                        var res = await _pos.CheckInOutAVR(avrData);

                        //publish result
                        if (res.code == 0)
                        {
                            await _mqttClient.PublishAsync(_mapper.CheckStayConsume(payload.eventData.encounterId, method, res.stay));
                        }
                        else
                            await _mqttClient.PublishAsync(_mapper.EventWithDescriptionConsume(
                                payload.eventData.encounterId, method, _mapper.CheckStayFailedDescription));
                    }
                    else
                        await _mqttClient.PublishAsync(_mapper.EventWithDescriptionConsume(
                            payload.eventData.encounterId, method, _mapper.NoConfidenceDescription));
                }
                else if (topic == "heartbeat")
                {
                    _lastHearbeat = DateTime.Now;

                    //ack
                    await _mqttClient.PublishAsync(_mapper.HearbeatAck());
                }
            }
            finally
            {
                _semaphoreSlim.Release();
            }
        }


    }



    public class FlashPosAvrReader : FlashPosAvrProducer
    {
        public static log4net.ITktLog logger = log4net.TktLogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);


        public FlashPosAvrReader(FlashPosAvrCameraConfiguration configuration)
            : base(configuration)
        { }


        protected override async Task OnUseApplicationMessageReceivedEvent(MqttApplicationMessageReceivedEventArgs data)
        {
            string otherdata = JsonConvert.SerializeObject(data);

            logger.Info("FVR new message", "FVR Message", otherdata);
        }

    }








}
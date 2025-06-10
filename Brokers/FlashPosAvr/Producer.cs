using MQTTnet;
using MQTTnet.Client;
using MQTTnet.Client.Connecting;
using MQTTnet.Client.Options;
using MQTTnet.Client.Receiving;
using MQTTnet.Client.Subscribing;
using MQTTnet.Exceptions;
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


    public class FPAProducer //: IMqttApplicationMessageReceivedHandler
    {
        public static log4net.ITktLog logger = log4net.TktLogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        private readonly FPARepository _repo;
        private readonly FPAMapper _mapper;
        private readonly IPosProxy _pos;

        private readonly SemaphoreSlim _semaphoreSlim;

        private readonly FPABrokerConfiguration _brokerConfig;
        private readonly string _clientId;
        private readonly FPACameraConfiguration _cameraConfiguration;

        private IMqttClient _mqttClient;
        private DateTime _lastHearbeat;

        private bool _isActive;


        //for testing
        public FPAProducer(FPACameraConfiguration camera, IMqttClientMock mqttMock, IPosProxy posMock)
        {
            _brokerConfig = FPAPolicy.BrokerPolicies;
            _clientId = $"{_brokerConfig}:{camera.WorkstationId}"; //actual producer's client it

            _cameraConfiguration = camera;
            _repo = new FPARepository();
            _mapper = new FPAMapper();
            _pos = posMock;

            _semaphoreSlim = new SemaphoreSlim(1);

            mqttMock.UseApplicationMessageReceivedHandler_Mock(async e => await OnUseApplicationMessageReceivedEvent(e));
            _mqttClient = mqttMock;

            _lastHearbeat = DateTime.Now;
        }



        public FPAProducer(FPACameraConfiguration camera)
        {
            _brokerConfig = FPAPolicy.BrokerPolicies;
            _clientId = $"{_brokerConfig}:{camera.WorkstationId}"; //actual producer's client it

            _cameraConfiguration = camera;
            _repo = new FPARepository();
            _mapper = new FPAMapper();
            _pos = new FPAPosProxy();

            _semaphoreSlim = new SemaphoreSlim(1);

            _lastHearbeat = DateTime.Now;
        }


        public async Task Start()
        {
            bool res = false;

            //create client
            if (_mqttClient == null)
                res = await CreateProducer();
            else
                res = true;

            if (res)
            {
                _isActive = true;

                logger.Info("Camera client started", "Start", $"WorkstationId:{_cameraConfiguration?.WorkstationId}");
            }
            else
                logger.Error("Camera client cannot start", "Start", $"WorkstationId:{_cameraConfiguration?.WorkstationId}");
        }


        public async Task Stop()
        {
            try
            {
                await _semaphoreSlim.WaitAsync();

                if (_mqttClient.IsConnected)
                {
                    await _mqttClient.UnsubscribeAsync("detection");
                    await _mqttClient.UnsubscribeAsync("heartbeat");
                    await _mqttClient.DisconnectAsync();
                }
            }
            catch (Exception ex)
            {
                logger.Error("Failed to disconnect from mqtt client", "Stop", $"WorkstationId:{_cameraConfiguration?.WorkstationId},Message:{ex}");
            }
            finally
            {
                _isActive = false;

                _semaphoreSlim.Release();
            }

            logger.Info("Camera client stopped", "Stop", $"WorkstationId:{_cameraConfiguration?.WorkstationId}");
        }



        public bool ReportBlackout()
        {
            bool wasblackout = false;

            if (_lastHearbeat < DateTime.Now.AddMinutes(-2))
            {
                wasblackout = true;
                logger.Warn($"Possible camera blackout.", "Camera Blackout", $"Workstation:{_cameraConfiguration?.WorkstationId},LastHB:{_lastHearbeat:HH:mm:ss}");
            }

            return wasblackout;
        }


        private async Task<bool> CreateProducer()
        {
            bool res = false;

            try
            {
                // Create a MQTT client factory
                var factory = new MqttFactory();

                // Create a MQTT client instance
                _mqttClient = factory.CreateMqttClient();

                // Create MQTT client options
                var options = new MqttClientOptionsBuilder()
                    .WithTcpServer(_cameraConfiguration.IP, _cameraConfiguration.Port) // MQTT broker address and port
                                                                                       //.WithCredentials(_cameraConfiguration.Username, _cameraConfiguration.Password) // Set username and password
                    .WithClientId(_clientId)
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

                    res = true;
                }
                else
                    throw new Exception($"Cannot connect to camera. WorkstationId:{_cameraConfiguration.WorkstationId}, ResultCode:{connectResult.ResultCode}");

            }
            catch(Exception ex)
            {
                logger.Error("Error creating camera client", "Create Camera Client", $"WorkstationId:{_cameraConfiguration?.WorkstationId},Message:{ex}");
            }

            return res;
        }


        ////produce data for pos avr
        protected virtual async Task OnUseApplicationMessageReceivedEvent(MqttApplicationMessageReceivedEventArgs e)
        {
            //todo: respond to all events
            const string method = "POS.Rest.CheckInOutAVR";

            logger.Debug($"Message from camera", "Camera Event", $"WorkstatinId:{_cameraConfiguration.WorkstationId},IsActive:{_isActive}");

            try
            {
                _lastHearbeat = DateTime.Now;

                await _semaphoreSlim.WaitAsync();

                if (!_isActive) return;

                var topic = e.ApplicationMessage.Topic;
                // Convertir el payload a una cadena legible
                var strPayload = Encoding.UTF8.GetString(e.ApplicationMessage.Payload);

                logger.Info("Received camera event", "Camera Event", $"WorkstationId:{_cameraConfiguration?.WorkstationId},Topic:{topic},Payload:{strPayload}");

                if (topic == "detection")
                {
                    var payload = JsonConvert.DeserializeObject<FVRPayload>(strPayload);

                    var avrData = _mapper.CheckInRequest(payload, _cameraConfiguration);

                    //save data to sync ng
                    await _repo.Add(avrData);

                    //ack mqtt
                    await PublishAndLog("Ack Detection", _mapper.DetectionAck(payload.eventData.encounterId));

                    //check confidence
                    if (avrData.infoplate.confidence >= _brokerConfig.PlateConfidenceMin)
                    {
                        //call pos
                        var res = await _pos.CheckInOutAVR(avrData);

                        //publish result
                        if (res.code == 0)
                        {
                            await PublishAndLog("Outcome Stay OK", _mapper.CheckStayConsume(payload.eventData.encounterId, method, res.stay, avrData));
                        }
                        else
                            await PublishAndLog("Outcome Stay Failure", _mapper.EventWithDescriptionConsume(
                                payload.eventData.encounterId, method, _mapper.CheckStayFailedDescription));
                    }
                    else
                        await PublishAndLog("Outcome Plate No Confidence", _mapper.EventWithDescriptionConsume(
                            payload.eventData.encounterId, method, _mapper.NoConfidenceDescription));
                }

                else if (topic == "heartbeat")
                {
                    //ack
                    await PublishAndLog("Ack Heartbeat", _mapper.HearbeatAck());
                }
            }
            catch(MqttCommunicationException ex)
            {
                logger.Error("Disconnection detected. Will try to reconnect", "Camera Event", $"WorkstationId:{_cameraConfiguration?.WorkstationId},Message:{ex}");

                await Reconnect();
            }
            catch(Exception ex)
            {
                logger.Error("Error processing camera event", "Camera Event", $"WorkstationId:{_cameraConfiguration?.WorkstationId},Message:{ex}");
            }
            finally
            {
                _semaphoreSlim.Release();
            }



            // MACROS ////////////////////////////////
            async Task PublishAndLog(string logMessage, MqttApplicationMessage message)
            {
                logger.Debug(logMessage, "Publish Event", $"WorkstationId:{_cameraConfiguration?.WorkstationId},Message:{JsonConvert.SerializeObject(message)}");

                await _mqttClient.PublishAsync(message);
            }

        }


        public async Task Reconnect()
        {
            try
            {
                //await _semaphoreSlim.WaitAsync();
                logger.Debug("Reconnecting", "Reconnect", $"WorkstationId:{_cameraConfiguration?.WorkstationId}");

                _mqttClient.Dispose();

                await CreateProducer();

                _lastHearbeat = DateTime.Now;

                _isActive = true;
            }
            catch(Exception ex)
            {
                logger.Error("Reconnection failed", "Reconnect", $"WorkstationId:{_cameraConfiguration?.WorkstationId},Message:{ex}");
            }
            finally
            {
                //_semaphoreSlim.Release();
            }
        }
    }


}
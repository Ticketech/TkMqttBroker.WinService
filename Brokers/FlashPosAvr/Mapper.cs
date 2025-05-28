using MQTTnet;
using Newtonsoft.Json;
using System;
using System.Collections.Specialized;
using System.Linq;
using Tk.ConfigurationManager;
using Tk.Services.REST.Models.Stays;

namespace TkMqttBroker.WinService.Brokers.FlashPosAvr
{
    public class FlashPosAvrMapper
    {
        static readonly log4net.ITktLog logger = log4net.TktLogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        private readonly FlashPosAvrBrokerConfiguration _config;

        public readonly string CheckStayFailedDescription = "Received by POS--check in/out failed";
        public readonly string NoConfidenceDescription = "Confidence below threshold--not processed";


        public FlashPosAvrMapper()
        {
            _config = FlashPosAvrPolicy.BrokerPolicies;
        }


        public CheckInRequest CheckInRequest(FVRPayload payload, FlashPosAvrCameraConfiguration workstation)
        {
            var map = new CheckInRequest
            {
                infoplate = new AVRPlateInfo
                {
                    camera_id = payload.deviceMxId,
                    confidence = PosConfidence(payload.eventData.licensePlateConfidence),
                    direction = workstation.Direction.ToString(),
                    location_id = TkConfigurationManager.CurrentLocationId,
                    plate = $"{payload.eventData.licensePlate} : {workstation.Direction} : Duration : 0hr 0m 0s",
                    workstation_id = workstation.WorkstationId,
                    workstation_name = workstation.WorkstationId,
                    id = payload.eventId,
                    lane_id = PosLaneId(payload.eventData.laneId), //must be a number!
                    make = CheckInRequestMake(payload.eventData),
              
                    full_image = payload.mainImagePath,
                    cropped_image = payload.lpCropPath,
                    alert = false,
                    evidence_image = CheckInRequestEvidenceImage(payload),
                    latitude = 0,
                    longitude = 0,
                    vehicle_category = payload.eventData.type,
                    headgear = "", //?
                    colour = CheckInRequestColor(payload.eventData),
                    db_match = false, //?
                    event_timestamp = EpochMiliseconds(payload.eventDate),
                },
            };

            //region
            CheckInRequestRegion(map, payload.eventData);

            return map;
        }

        public string CheckInRequestEvidenceImage(FVRPayload payload)
        {
            var path = payload.lpCropPaths?.Where(e => e != null && e.Contains("__IR__LPC.jpg")).FirstOrDefault();

            return path;
        }

        public int PosLaneId(string laneId)
        {
            int result;
            if (int.TryParse(laneId, out result))
                return result;
            else
                return 0;
        }

        public string CheckInRequestMake(FVREventData eventData)
        {
            if (PosConfidence(eventData.makeConfidence) < _config.MakeConfidenceMin)
                return "OTHER";
            else
                return eventData.make;
        }


        public int PosConfidence(double fvrConfidence)
        {
            return (int)Math.Round(fvrConfidence * 100.0);
        }


        public string CheckInRequestColor(FVREventData eventData)
        {
            if (PosConfidence(eventData.makeConfidence) < _config.ColorConfidenceMin)
                return "OTHER";
            else
                return eventData.color;
        }


        public void CheckInRequestRegion(CheckInRequest target, FVREventData eventData)
        {
            if (PosConfidence(eventData.stateConfidence) < _config.StateConfidenceMin)
            {
                target.infoplate.region = $"us-{_config.DefaultStateCode}".ToLower();
                target.infoplate.region_confidence = 100;
            }
            else
            {
                string code = FlashPosAvrPolicy.StateCode(eventData.state);
                if (code == null)
                {
                    target.infoplate.region = $"us-{_config.DefaultStateCode}".ToLower();
                    target.infoplate.region_confidence = 100;
                }
                else
                {
                    target.infoplate.region = $"us-{FlashPosAvrPolicy.StateCode(eventData.state)}".ToLower();
                    target.infoplate.region_confidence = PosConfidence(eventData.stateConfidence);
                }
            }
        }


        public long EpochMiliseconds(DateTime eventDate)
        {
            //https://stackoverflow.com/questions/9453101/how-do-i-get-epoch-time-in-c

            TimeSpan t = DateTime.UtcNow - new DateTime(1970, 1, 1);
            return (long)t.TotalMilliseconds;
        }

        public NGPostAvrEntryRawRequest NGPostAvrEntryRawRequest(CheckInRequest source)
        {
            return new NGPostAvrEntryRawRequest
            {
                infoplate = new NGInfoplate
                {
                    gcamera_id = source.infoplate.camera_id,
                    confidence = source.infoplate.confidence,
                    direction = source.infoplate.direction,
                    location_id = source.infoplate.location_id,
                    plate = source.infoplate.plate,
                    workstation_id = source.infoplate.workstation_id,
                    workstation_name = source.infoplate.workstation_name,
                    Id = source.infoplate.id,
                    lane_id = source.infoplate.lane_id,
                    make = source.infoplate.make,

                    full_image = source.infoplate.full_image,
                    cropped_image = source.infoplate.cropped_image,
                    alert = source.infoplate.alert,
                    evidence_image = source.infoplate.evidence_image,
                    latitude = source.infoplate.latitude,
                    longitude = source.infoplate.longitude,
                    vehicle_category = NGVehicleCategory(source.infoplate.vehicle_category),
                    headgear = source.infoplate.headgear,
                    colour = source.infoplate.colour,
                    db_match = source.infoplate.db_match,
                    event_timestamp = source.infoplate.event_timestamp.ToString(),
                }
            };
        }

        public string NGVehicleCategory(string vehicle_category)
        {
            return "CAR"; //default until we know what is in source
        }

        public MqttApplicationMessage DetectionAck(string encounterId)
        {
            var ack = new
            {
                schema_version = 0.01,
                message_uid = Guid.NewGuid().ToString(),
                sender_uid = TkConfigurationManager.CurrentLocationGUID.ToString(),
                sender_node_type = _config.ClientId,
                event_ts = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                event_type = "ack",
                encounter_id = encounterId,
                client_data = new
                {
                    ip_address = FlashPosAvrPolicy.GetLocalIPAddress(),
                    client_version = _config.SoftwareVersion,
                    event_category_id = ""
                },
                events = new
                {
                    event_data = new object[] { }
                },
                payload_data = new
                {
                    _payload = "",
                    _payload_type = "STRING"
                },
                event_count = 0,
                payload = false
            };

            string json = JsonConvert.SerializeObject(ack);

            return new MqttApplicationMessageBuilder()
                .WithTopic("detection-ext")
                .WithPayload(json)
                .WithExactlyOnceQoS()
                .WithRetainFlag(false)
                .Build();
        }


        public MqttApplicationMessage HearbeatAck()
        {
            var response = new
            {
                schema_version = 0.01,
                message_uid = Guid.NewGuid().ToString(),
                sender_uid = TkConfigurationManager.CurrentLocationGUID.ToString(),
                sender_node_type = _config.ClientId,
                event_ts = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                event_type = "heartbeat",
                encounter_id = (string)null,
                client_data = new
                {
                    ip_address = FlashPosAvrPolicy.GetLocalIPAddress(),
                    client_version = _config.SoftwareVersion,
                    event_category_id = ""
                },
                events = new
                {
                    event_data = new object[0]
                },
                payload_data = new
                {
                    _payload = "",
                    _payload_type = "STRING"
                },
                event_count = 0,
                payload = true
            };

            string json = JsonConvert.SerializeObject(response);

            return new MqttApplicationMessageBuilder()
                .WithTopic("heartbeat-ext")
                .WithPayload(json)
                .WithExactlyOnceQoS()
                .WithRetainFlag(false)
                .Build();
        }


        public MqttApplicationMessage EventWithDescriptionConsume(string encounterId, string method, string description)
        {
            var outcome = new
            {
                schema_version = 0.01,
                message_uid = Guid.NewGuid().ToString(),
                sender_uid = TkConfigurationManager.CurrentLocationGUID.ToString(),
                sender_node_type = _config.ClientId,
                event_ts = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                event_type = "outcome",
                encounter_id = encounterId,

                client_data = new
                {
                    ip_address = FlashPosAvrPolicy.GetLocalIPAddress(),
                    client_version = _config.SoftwareVersion,
                    event_category_id = "7922"
                },

                events = new
                {
                    event_data = new[]
                     {
                         new { _key = "method", _value = method },
                         new { _key = "eventDescription", _value = $"{description}" }
                    }
                },

                payload_data = new
                {
                    _payload = "",
                    _payload_type = "STRING"
                },

                event_count = 2,
                payload = false
            }; string json = JsonConvert.SerializeObject(outcome, Formatting.Indented);

            //Console.WriteLine("Mensaje Outcome MQTT:\n" + json); 

            return new MqttApplicationMessageBuilder()
            .WithTopic("detection-ext")
            .WithPayload(json)
            .WithExactlyOnceQoS()
            .WithRetainFlag(false)
            .Build();
        }



        internal MqttApplicationMessage CheckStayConsume(string encounterId, string method, StayInfo stay)
        {
            string description = "";
            string trigger = "";
            if (stay.checkout_time == null) //checkin
            {
                trigger = "CheckIn";

                description = $"Check-In Ticket #{stay.ticket_number}";
                if (!string.IsNullOrWhiteSpace(stay.tag_number))
                    description += $" Tag {stay.tag_number}";
            }
            else //checkout
            {
                trigger = "CheckOut";

                description = $"Check-Out Ticket #{stay.ticket_number}";
            }

            var payload = new
            {
                schema_version = 0.01,
                message_uid = Guid.NewGuid().ToString(),
                sender_uid = TkConfigurationManager.CurrentLocationGUID.ToString(),
                sender_node_type = _config.ClientId,
                event_ts = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                event_type = "outcome",
                encounter_id = encounterId,

                client_data = new
                {
                    ip_address = FlashPosAvrPolicy.GetLocalIPAddress(),
                    client_version = _config.SoftwareVersion,
                    event_category_id = "7922"
                },
                
                events = new
                {
                    event_data = new[]
                    {
                         new { _key = "method", _value = method },
                         new { _key = "ticketId", _value = stay.stay_guid.ToString() },
                         new { _key = "ticketNumber", _value = stay.ticket_number.ToString() },
                         new { _key = "ticketType", _value = $"{stay.stay_type} Ticketech" },
                         new { _key = "ticketTrigger", _value = $"{trigger}" },
                         new { _key = "plate", _value = stay.plate },
                         new { _key = "eventDescription", _value = $"{description}" }
                    }
                },

                payload_data = new
                {
                    _payload = "",
                    _payload_type = "STRING"
                },

                event_count = 7,
                payload = false
            }; 
            
            string json = JsonConvert.SerializeObject(payload, Formatting.Indented);

            //Console.WriteLine("Mensaje Outcome MQTT:\n" + json); 
            
        

            return new MqttApplicationMessageBuilder()
            .WithTopic("detection-ext")
            .WithPayload(json)
            .WithExactlyOnceQoS()
            .WithRetainFlag(false)
            .Build();
        }

        //internal MqttApplicationMessage CheckoutConsume(string encounterId, string method, StayInfo data)
        //{
        //    var outcome = new
        //    {
        //        schema_version = 0.01,
        //        message_uid = Guid.NewGuid().ToString(),
        //        sender_uid = TkConfigurationManager.CurrentLocationGUID.ToString(),
        //        sender_node_type = _config.ClientId,
        //        event_ts = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
        //        event_type = "outcome",
        //        encounter_id = encounterId,

        //        client_data = new
        //        {
        //            ip_address = FlashPosAvrPolicy.GetLocalIPAddress(),
        //            client_version = _config.SoftwareVersion,
        //            event_category_id = "7922"
        //        },

        //        events = new
        //        {
        //            event_data = new[]
        //           {
        //                 new { _key = "method", _value = method },
        //                 new { _key = "ticketId", _value = data.stay_guid.ToString() },
        //                 new { _key = "ticketNumber", _value = data.ticket_number.ToString() },
        //                 new { _key = "ticketType", _value = $"{data.stay_type},Ticketech" },
        //                 new { _key = "ticketTrigger", _value = "CheckOut" },
        //                 new { _key = "plate", _value = data.plate },
        //                 new { _key = "eventDescription", _value = $"Check-Out Ticket #{data.ticket_number}" }
        //            }
        //        },

        //        payload_data = new
        //        {
        //            _payload = "",
        //            _payload_type = "STRING"
        //        },

        //        event_count = 7,
        //        payload = false
        //    }; string json = JsonConvert.SerializeObject(outcome, Formatting.Indented);

        //    //Console.WriteLine("Mensaje Outcome MQTT:\n" + json); 

        //    return new MqttApplicationMessageBuilder()
        //    .WithTopic("detection-ext")
        //    .WithPayload(json)
        //    .WithExactlyOnceQoS()
        //    .WithRetainFlag(false)
        //    .Build();
        //}

        //internal MqttApplicationMessage NoConfidenceConsume(string encounterId, string method)
        //{
        //    var outcome = new
        //    {
        //        schema_version = 0.01,
        //        message_uid = Guid.NewGuid().ToString(),
        //        sender_uid = TkConfigurationManager.CurrentLocationGUID.ToString(),
        //        sender_node_type = _config.ClientId,
        //        event_ts = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
        //        event_type = "outcome",
        //        encounter_id = encounterId,

        //        client_data = new
        //        {
        //            ip_address = FlashPosAvrPolicy.GetLocalIPAddress(),
        //            client_version = _config.SoftwareVersion,
        //            event_category_id = "7922"
        //        },

        //        events = new
        //        {
        //            event_data = new[]
        //              {
        //                 new { _key = "method", _value = method },
        //                 new { _key = "eventDescription", _value = $"Confidence below threshold--not processed" }
        //            }
        //        },

        //        payload_data = new
        //        {
        //            _payload = "",
        //            _payload_type = "STRING"
        //        },

        //        event_count = 2,
        //        payload = false
        //    }; string json = JsonConvert.SerializeObject(outcome, Formatting.Indented);

        //    //Console.WriteLine("Mensaje Outcome MQTT:\n" + json); 

        //    return new MqttApplicationMessageBuilder()
        //    .WithTopic("detection-ext")
        //    .WithPayload(json)
        //    .WithExactlyOnceQoS()
        //    .WithRetainFlag(false)
        //    .Build();
        //}
    }
}
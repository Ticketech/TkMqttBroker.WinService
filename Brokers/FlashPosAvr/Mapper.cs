using MQTTnet;
using Newtonsoft.Json;
using System;
using Tk.ConfigurationManager;
using Tk.Services.REST.Models.Stays;

namespace TkMqttBroker.WinService.Brokers.FlashPosAvr
{
    internal class FlashPosAvrMapper
    {
        public FlashPosAvrMapper()
        {
        }

        internal CheckInRequest CheckInRequest(FVRPayload source, string workstationId)
        {
            return new CheckInRequest
            {
                infoplate = new AVRPlateInfo
                {
                    camera_id = source.deviceMxId,
                    confidence = PercentageInt(source.eventData.licensePlateConfidence),
                    direction = "no idea", //direction??
                    location_id = TkConfigurationManager.CurrentLocationId,
                    plate = $"{source.eventData.licensePlate} : EXIT : Duration : 1hr 7m 22s", //duration??
                    workstation_id = workstationId,
                    workstation_name = workstationId,
                    id = source.eventId,
                    lane_id = Convert.ToInt32(source.eventData.laneId), //must be a number!
                    make = source.eventData.make,
              
                    full_image = @"https://s3.ap-south-1.amazonaws.com/uploads.live.videoanalytics/Ticketech/Garage%201%20Exit/2020-02-04/1570213035304-Camera/1_51_54am_1580761314474_0.jpg",
                    cropped_image = @"https://s3.ap-south-1.amazonaws.com/uploads.live.videoanalytics/Ticketech/Garage%201%20Exit/2020-02-04/1570213035304-Camera/1_51_54am_1580761314474_1.jpg",
                    alert = true,
                    evidence_image = @"https://s3.ap-south-1.amazonaws.com/uploads.live.videoanalytics/Ticketech/Garage%201%20Exit/2020-02-04/1570213035304-Camera/1_51_54am_1580761314474_0.jpg",
                    latitude = 0,
                    longitude = 0,
                    vehicle_category = source.eventData.type,
                    headgear = "", //?
                    colour = source.eventData.color,
                    db_match = true, //?
                    event_timestamp = (long)source.eventDate.ToFileTimeUtc(), //used?
                },
            };
        }

        private int PercentageInt(double value)
        {
            return Convert.ToInt32(Math.Round(value * 100, 2));
        }

        internal NGPostAvrEntryRawRequest NGPostAvrEntryRawRequest(CheckInRequest source)
        {
            return new NGPostAvrEntryRawRequest();
        }


        internal MqttApplicationMessage DetectionAck(string encounterId)
        {
            string uid = Guid.NewGuid().ToString();

            var ack = new
            {
                schema_version = 0.01,
                message_uid = uid,
                sender_uid = uid,
                sender_node_type = "FVR",
                event_ts = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                event_type = "ack",
                encounter_id = encounterId,
                client_data = new   /////////////////////////////////////////////////// who is this?
                {
                    ip_address = FlashPosAvrPolicy.GetLocalIPAddress(),
                    client_version = FlashPosAvrPolicy.SoftwareVersion,
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


        internal MqttApplicationMessage HearbeatAck()
        {
            string uid = Guid.NewGuid().ToString();

            var response = new
            {
                schema_version = 0.01,
                message_uid = uid,
                sender_uid = uid,
                sender_node_type = "FVR",
                event_ts = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                event_type = "heartbeat",
                encounter_id = (string)null,
                client_data = new   /////////////////////////////////////////////////// who is this?
                {
                    ip_address = FlashPosAvrPolicy.GetLocalIPAddress(),
                    client_version = FlashPosAvrPolicy.SoftwareVersion,
                    event_category_id = "9000"
                },
                events = new
                {
                    event_data = new[]
                    {
                        new { _key = "status", _value = "ok" }
                    }
                },
                payload_data = new
                {
                    _payload = "heartbeat ack from client",
                    _payload_type = "STRING"
                },
                event_count = 1,
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
    }
}
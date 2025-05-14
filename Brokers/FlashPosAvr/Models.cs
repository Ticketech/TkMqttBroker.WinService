using MQTTnet.Client.Subscribing;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tk.ConfigurationManager.DevicesConfiguration;

namespace TkMqttBroker.WinService.Brokers.FlashPosAvr
{

    //public class FlashPosAvrBrokerConfiguration
    //{
    //    public string Password;
    //    public int Port;
    //    public string Topic;
    //    public string Username;

    //}


    public class FlashPosAvrProducerConfiguration
    {
        public string IP;
        public int? Port;
        public string ClientId;
        public string Topic; // public MqttClientSubscribeOptions Topic;



        public FlashPosAvrProducerConfiguration Clone(DeviceConfiguration tkdevice)
        {
            var clone = (FlashPosAvrProducerConfiguration)this.MemberwiseClone();
            clone.IP = tkdevice.Location; //ip address

            return clone;
        }
    }




    #region Flash



    // Root myDeserializedClass = JsonConvert.DeserializeObject<Root>(myJsonResponse);
    public class FVRClientData
    {
        public string type { get; set; }
        public FVRFVRProperties properties { get; set; }
        public List<string> required { get; set; }
    }

    public class FVRClientVersion
    {
        public string type { get; set; }
    }

    public class FVREncounterId
    {
        public List<string> type { get; set; }
    }

    public class FVREventCategoryId
    {
        public string type { get; set; }
    }

    public class FVREventCount
    {
        public string type { get; set; }
    }

    public class FVREventData
    {
        public string type { get; set; }
        public FVRItems items { get; set; }
    }

    public class FVREvents
    {
        public string type { get; set; }
        public FVRFVRProperties properties { get; set; }
        public List<string> required { get; set; }
    }

    public class FVREventTs
    {
        public string type { get; set; }
    }

    public class FVREventType
    {
        public string type { get; set; }
        public List<string> @enum { get; set; }
    }

    public class FVRIpAddress
    {
        public string type { get; set; }
        public string format { get; set; }
    }

    public class FVRItems
    {
        public string type { get; set; }
        public FVRFVRProperties properties { get; set; }
        public List<string> required { get; set; }
    }

    public class FVRKey
    {
        public string type { get; set; }
    }

    public class FVRMessageUid
    {
        public string type { get; set; }
        public string format { get; set; }
    }

    public class FVRPayload
    {
        public string type { get; set; }
    }

    public class FVRPayload2
    {
        public string type { get; set; }
    }

    public class FVRPayloadData
    {
        public string type { get; set; }
        public FVRFVRProperties properties { get; set; }
        public List<string> required { get; set; }
    }

    public class FVRPayloadType
    {
        public string type { get; set; }
        public List<string> @enum { get; set; }
    }

    public class FVRFVRProperties
    {
        public FVRSchemaVersion schema_version { get; set; }
        public FVRMessageUid message_uid { get; set; }
        public FVRSenderUid sender_uid { get; set; }
        public FVRSenderNodeType sender_node_type { get; set; }
        public FVREventTs event_ts { get; set; }
        public FVREventType event_type { get; set; }
        public FVREncounterId encounter_id { get; set; }
        public FVRClientData client_data { get; set; }
        public FVREvents events { get; set; }
        public FVRPayloadData payload_data { get; set; }
        public FVREventCount event_count { get; set; }
        public FVRPayload payload { get; set; }
        public FVRIpAddress ip_address { get; set; }
        public FVRClientVersion client_version { get; set; }
        public FVREventCategoryId event_category_id { get; set; }
        public FVREventData event_data { get; set; }
        public FVRKey _key { get; set; }
        public FVRValue _value { get; set; }
        public FVRPayload _payload { get; set; }
        public FVRPayloadType _payload_type { get; set; }
    }

    public class FVRFlashAvrData
    {
        [JsonProperty("$schema")]
        public string schema { get; set; }
        public string type { get; set; }
        public FVRFVRProperties properties { get; set; }
        public List<string> required { get; set; }
    }

    public class FVRSchemaVersion
    {
        public string type { get; set; }
    }

    public class FVRSenderNodeType
    {
        public string type { get; set; }
    }

    public class FVRSenderUid
    {
        public string type { get; set; }
        public string format { get; set; }
    }

    public class FVRValue
    {
        public string type { get; set; }
    }



    #endregion 




    #region NG

    public class NGPostAvrEntryRawRequest
    {
        public NGInfoplate infoplate;
    }


    public class NGInfoplate
    {
        public string Id;
        public string gcamera_id;
        public int lane_id;
        public string plate;
        public string reading;
        public string make;
        public string colour;
        public string direction;
        public int confidence;
        public string full_image;
        public string cropped_image;
        public bool alert;
        public string evidence_image;
        public int latitude;
        public int longitude;
        public string vehicle_category;
        public string headgear;
        public bool db_match;
        public string event_timestamp;
        public string workstation_name;
        public string workstation_id;
        public string location_id;
    }

    #endregion


}

using MQTTnet.Client.Subscribing;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
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
        public string Topic; // public MqttClientSubscribeOptions Topic;
    }



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

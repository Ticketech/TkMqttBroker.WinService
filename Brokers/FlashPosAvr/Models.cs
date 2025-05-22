using MQTTnet;
using MQTTnet.Client;
using MQTTnet.Client.Subscribing;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tk.ConfigurationManager.DevicesConfiguration;
using Tk.Services.REST.Models.Stays;

namespace TkMqttBroker.WinService.Brokers.FlashPosAvr
{

    //public class FlashPosAvrBrokerConfiguration
    //{
    //    public string Password;
    //    public int Port;
    //    public string Topic;
    //    public string Username;

    //}

    public interface IPosProxy
    {
        Task<CheckInResponse> CheckInOutAVR(CheckInRequest avrData);
    }

    public interface INGProxy
    {
        Task<bool> Send(NGPostAvrEntryRawRequest data);
    }

    //mock mqtt client
    public interface IMqttClientMock: IMqttClient
    {
        void UseApplicationMessageReceivedHandler_Mock(Func<MqttApplicationMessageReceivedEventArgs, Task> handler);
    }


    public class FlashPosAvrBrokerConfiguration
    {
        public int CameraPort;
        public string ClientId;
        public int ColorConfidenceMin;
        public int MakeConfidenceMin;
        public int PlateConfidenceMin;
        public int StateConfidenceMin;
        public string DefaultStateCode;
        public string SoftwareVersion;

        public string NGServiceUrl { get; set; }
        public string NGApiKey { get; set; }

        public FlashPosAvrBrokerConfiguration Clone()
        {
            var clone = (FlashPosAvrBrokerConfiguration)this.MemberwiseClone();

            return clone;
        }
    }

    public enum FPADirection
    {
        ENTRY,
        EXIT
    }


    public class FlashPosAvrCameraConfiguration
    {
        public string WorkstationId;
        public string IP;
        public int Port;
        public FPADirection Direction;
    }




    #region Flash


    public class FVREventData
    {
        public string encounterId { get; set; }
        public string color { get; set; }
        public double colorConfidence { get; set; }
        public string laneId { get; set; }
        public int laneNumber { get; set; }
        public string licensePlate { get; set; }
        public double licensePlateConfidence { get; set; }
        public string make { get; set; }
        public double makeConfidence { get; set; }
        public string model { get; set; }
        public int modelConfidence { get; set; }
        public string propulsion { get; set; }
        public double propulsionConfidence { get; set; }
        public string state { get; set; }
        public double stateConfidence { get; set; }
        public string type { get; set; }
        public double typeConfidence { get; set; }
    }

    public class FVRPayload
    {
        public string eventId { get; set; }
        public string eventType { get; set; }
        public FVREventData eventData { get; set; }
        public string deviceMxId { get; set; }
        public string mainImagePath { get; set; }
        public string lpCropPath { get; set; }
        public List<string> lpCropPaths { get; set; }
        public DateTime eventDate { get; set; }
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

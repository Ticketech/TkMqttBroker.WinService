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
        Task<bool> Send(NGPostAvrEntryRequestBody data);
    }

    //mock mqtt client
    public interface IMqttClientMock : IMqttClient
    {
        void UseApplicationMessageReceivedHandler_Mock(Func<MqttApplicationMessageReceivedEventArgs, Task> handler);
    }


    public class FPABrokerConfiguration
    {
        public int CameraPort;
        public string ClientId;
        public int ColorConfidenceMin;
        public int MakeConfidenceMin;
        public int PlateConfidenceMin;
        public int StateConfidenceMin;
        public string DefaultStateCode;
        public string SoftwareVersion;

        public string NGServiceUrl;
        public string NGApiKey;

        public string PosApiKey;
        public string PosServiceUrl;

        public string LocationId;

        public FPABrokerConfiguration Clone()
        {
            var clone = (FPABrokerConfiguration)this.MemberwiseClone();

            return clone;
        }
    }

    public enum FPADirection
    {
        ENTRY,
        EXIT
    }


    public class FPACameraConfiguration
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


    public class NGPostAvrEntryRequestBody
    {
        public string garage_identifier;
        public string external_id;
        public string external_camera_id;
        public string license_plate; //plate : direction
        public string reading; //plate
        public string region; //us-ny
        public int region_confidence;
        public string make;
        public string colour;
        public string direction; //Entry,Exit
        public int confidence;
        public string full_image;
        public string cropped_image;
        public string evidence_image;
        public string vehicle_category; //CAR
        public long event_timestamp_epoch_ms;
        public string workstation_name;
        public string external_workstation_id;
        public string external_location_id;
        public string stay_identifier;
        public string source; //flashfvr
        public string unsigned_gcp_full;
        public string unsigned_gcp_cropped;
        public string unsigned_gcp_evidence;
        public DateTime unsigned_gcp_signed_timestamp;
    }



    public class NGPageResultGarageGet
    {
        public NGGarageGet[] data;
    }

    public class NGGarageGet
    {
        public string identifier;
    }

    #endregion


}

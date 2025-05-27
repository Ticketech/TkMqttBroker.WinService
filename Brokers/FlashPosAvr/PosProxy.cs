using Newtonsoft.Json;
using System;
using System.Configuration;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using Tk.ConfigurationManager;
using Tk.Services.REST.Models.Stays;


namespace TkMqttBroker.WinService.Brokers.FlashPosAvr
{
    public class FlashPosAvrPosProxy : IPosProxy
    {
        static readonly log4net.ITktLog logger = log4net.TktLogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        private readonly FlashPosAvrBrokerConfiguration _config;

        public FlashPosAvrPosProxy()
        {
            _config = FlashPosAvrPolicy.BrokerPolicies;
        }


        public async Task<CheckInResponse> CheckInOutAVR(CheckInRequest avrData)
        {
            CheckInResponse result = null;

            try
            {
                using (var client = GetClient())
                {
                    string ApiCall = _config.PosServiceUrl + $"/api/v2/UltraApi/Locations/{TkConfigurationManager.CurrentLocationId}/Stays/AVR";
                    string payload = JsonConvert.SerializeObject(avrData);
                    var request = new StringContent(payload, Encoding.UTF8, "application/json");

                    logger.Info("Request Pos CheckInOutAVR", "Call Pos CheckInOutAVR", $"POST, Url:{ApiCall},Payload:{payload}");

                    var response = await client.PostAsync(ApiCall, request);

                    var responseStr = (await response.Content.ReadAsStringAsync()).ToString();

                    logger.Info("Response Pos CheckInOutAVR", "Call Pos CheckInOutAVR", $"Url:{ApiCall},Response:{responseStr}");

                    if ((int)response.StatusCode >= 500 && (int)response.StatusCode <= 599)
                        throw new Exception($"System Error. Status:{response.StatusCode},Message:{responseStr}.");

                    result = JsonConvert.DeserializeObject<CheckInResponse>(responseStr);
                }
            }
            catch (Exception ex)
            {
                logger.Error("Error checking in/out on pos", "Call Pos CheckInOutAVR", $"Request:{JsonConvert.SerializeObject(avrData)},Message:{ex}");

                result = new CheckInResponse
                {
                    code = -1,
                    message = ex.Message,
                };
            }

            return result;
        }


        private HttpClient GetClient()
        {
            var handler = new HttpClientHandler();

            handler.ClientCertificateOptions = ClientCertificateOption.Manual;
            handler.ServerCertificateCustomValidationCallback =
                (httpRequestMessage, cert, cetChain, policyErrors) =>
                {
                    return true;
                };

            var client = new HttpClient(handler);

            client.DefaultRequestHeaders.TryAddWithoutValidation("Authorization", $"Bearer {_config.PosApiKey}");
            client.DefaultRequestHeaders.Add("Accept", "application/json");
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            client.Timeout = new TimeSpan(0, 0, 15);

            return client;
        }
    }
}
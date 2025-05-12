using Newtonsoft.Json;
using System;
using System.Configuration;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using Tk.ConfigurationManager;
using Tk.Services.REST.Models.Stays;
using TkMqttBroker.WinService.Pos;

namespace TkMqttBroker.WinService.Brokers.FlashPosAvr
{
    public class PosClient
    {
        private readonly string _serviceUrl;
        private readonly string _locationId;
        private readonly string _apiKey;

        public PosClient()
        {
            _serviceUrl = global::TkMqttBroker.WinService.Properties.TkMqttBorker.Default.PosServiceUrl;
            _locationId = PosPolicies.LocationId();
            _apiKey = global::TkMqttBroker.WinService.Properties.TkMqttBorker.Default.PosApiKey;
        }



        public async Task<bool> CheckInOutAVR(CheckInRequest avrData)
        {
            bool res = false;

            try
            {
                using (var client = GetClient())
                {
                    string ApiCall = _serviceUrl + $"/api/v2/UltraApi/Locations/{_locationId}/Stays/AVR";
                    var request = new StringContent(JsonConvert.SerializeObject(avrData), Encoding.UTF8, "application/json");

                    var response = await client.PostAsync(ApiCall, request);

                    var responseStr = (await response.Content.ReadAsStringAsync()).ToString();

                    if ((int)response.StatusCode >= 500 && (int)response.StatusCode <= 599)
                        throw new Exception($"System Error. Status:{response.StatusCode},Message:{responseStr}.");

                    var result = JsonConvert.DeserializeObject<CheckInResponse>(responseStr);

                    if (result.code != 0)
                        throw new Exception($"Processing error. Code:{result.code}.Message:{result.message}.");

                    res = true;
                }
            }
            catch (Exception ex)
            {

            }

            return res;
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

            client.DefaultRequestHeaders.TryAddWithoutValidation("Authorization", $"Bearer {_apiKey}");
            client.DefaultRequestHeaders.Add("Accept", "application/json");
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            client.Timeout = new TimeSpan(0, 0, 15);

            return client;
        }
    }
}
using Newtonsoft.Json;
using System;
using System.Configuration;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using Tk.Services.REST.Models.Stays;

namespace TkMqttBroker.WinService.Brokers.FlashPosAvr
{
    public class PosClient
    {
        private readonly string _serviceUrl;
        private readonly string _locationId;
        private readonly string _apiKey;

        public PosClient()
        {
            _serviceUrl = ConfigurationManager.AppSettings["PosServiceUrl"];
            _locationId = ConfigurationManager.AppSettings["LocationId"];
            _apiKey = ConfigurationManager.AppSettings["PosApiKey"];
        }



        public async Task<bool> CheckInOutAVR(CheckInRequest avrData)
        {
            bool res = false;

            try
            {
                using (var client = new HttpClient())
                {
                    client.DefaultRequestHeaders.TryAddWithoutValidation("Authorization", $"Bearer {_apiKey}");
                    client.DefaultRequestHeaders.Add("Accept", "application/json");
                    client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                    client.Timeout = new TimeSpan(0, 1, 0);

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



    }
}
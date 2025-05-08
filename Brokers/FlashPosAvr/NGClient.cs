using Newtonsoft.Json;
using System;
using System.Configuration;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using Tk.Services.REST.Models.Stays;

namespace TkMqttBroker.WinService.Brokers.FlashPosAvr
{
    internal class NGClient
    {
        private readonly string _serviceUrl;
        private readonly string _apiKey;

        public NGClient()
        {
            _serviceUrl = ConfigurationManager.AppSettings["NGServiceUrl"];
            _apiKey = ConfigurationManager.AppSettings["NGApiKey"];
        }


        public async Task<bool> Send(NGPostAvrEntryRawRequest data)
        {
            bool res = false;

            try
            {
                using (var client = GetClient())
                {
                    string ApiCall = _serviceUrl + $"/api/core/avr/entry/raw";
                    var request = new StringContent(JsonConvert.SerializeObject(data), Encoding.UTF8, "application/json");

                    var response = await client.PostAsync(ApiCall, request);

                    var responseStr = (await response.Content.ReadAsStringAsync()).ToString();

                    if ((int)response.StatusCode >= 500 && (int)response.StatusCode <= 599)
                        throw new Exception($"System Error. Status:{response.StatusCode},Message:{responseStr}.");

                    if (response.StatusCode != HttpStatusCode.Created)
                        throw new Exception($"Processing error. Code:{response.StatusCode}.Message:{responseStr}.");

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
            client.Timeout = new TimeSpan(0, 1, 0);

            return client;
        }
    }
}
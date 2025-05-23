using Newtonsoft.Json;
using System;
using System.Configuration;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using Tk.ConfigurationManager;



namespace TkMqttBroker.WinService.Brokers.FlashPosAvr
{
    public class FlashPosAvrNGProxy: INGProxy
    {
        static readonly log4net.ITktLog logger = log4net.TktLogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        private readonly FlashPosAvrBrokerConfiguration _config;


        public FlashPosAvrNGProxy()
        {
            _config = FlashPosAvrPolicy.BrokerPolicies;
        }


        public async Task<bool> Send(NGPostAvrEntryRawRequest data)
        {
            bool res = false;

            try
            {
                using (var client = GetClient())
                {
                    string ApiCall = _config.NGServiceUrl + $"/api/core/avr/entry/raw";
                    string payload = JsonConvert.SerializeObject(data);
                    var request = new StringContent(payload, Encoding.UTF8, "application/json");

                    logger.Info("Request", "Send Raw Avr", $"Url:{ApiCall},Payload:{payload}");

                    var response = await client.PostAsync(ApiCall, request);

                    var responseStr = (await response.Content.ReadAsStringAsync()).ToString();

                    logger.Info("Response", "Send Raw Avr", $"Url:{ApiCall},Response:{responseStr}");

                    if ((int)response.StatusCode >= 500 && (int)response.StatusCode <= 599)
                        throw new Exception($"System Error. Status:{response.StatusCode},Message:{responseStr}.");

                    if (response.StatusCode != HttpStatusCode.Created)
                        throw new Exception($"Processing error. Code:{response.StatusCode}.Message:{responseStr}.");

                    res = true;
                }
            }
            catch (Exception ex)
            {
                logger.Error("Error sending raw avr", "Send Raw Avr", $"Param:{JsonConvert.SerializeObject(data)},Error:{ex}");
            }

            return res;
        }


        private HttpClient GetClient()
        {
            //http client
            var handler = new HttpClientHandler();

            handler.ClientCertificateOptions = ClientCertificateOption.Manual;
            handler.ServerCertificateCustomValidationCallback =
                (httpRequestMessage, cert, cetChain, policyErrors) =>
                {
                    return true;
                };

            var client = new HttpClient(handler);

            client.DefaultRequestHeaders.TryAddWithoutValidation("Authorization", $"Bearer {_config.NGApiKey}");
            client.DefaultRequestHeaders.Add("Accept", "application/json");
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            client.Timeout = new TimeSpan(0, 0, 5);

            return client;
        }
    }
}
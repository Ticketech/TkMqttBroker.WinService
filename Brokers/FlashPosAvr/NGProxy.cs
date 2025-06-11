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
    public class FPANGProxy : INGProxy
    {
        static readonly log4net.ITktLog logger = log4net.TktLogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        private readonly FPABrokerConfiguration _config;
        private string _garageIdentifier;

        public FPANGProxy ()
        {
            _config = FPAPolicy.BrokerPolicies;
        }


        public async Task<bool> Send(NGPostAvrEntryRequestBody data)
        {
            bool res = false;

            try
            {
                using (var client = GetClient())
                {
                    string ApiCall = _config.NGServiceUrl + $"/api/core/avr/entry";

                    data.garage_identifier = await GarageIdentifier();
                    string payload = JsonConvert.SerializeObject(data);
                    
                    var request = new StringContent(payload, Encoding.UTF8, "application/json");
                    
                    logger.Info("Request NG raw avr", "Send Raw Avr", $"POST,Url:{ApiCall},Payload:{payload}");

                    var response = await client.PostAsync(ApiCall, request);

                    var responseStr = (await response.Content.ReadAsStringAsync()).ToString();

                    logger.Info("Response NG raw avr", "Send Raw Avr", $"Url:{ApiCall},HttpStatus:{response.StatusCode},Response:{responseStr}");

                    if ((int)response.StatusCode >= 500 && (int)response.StatusCode <= 599)
                        throw new Exception($"System Error. Status:{response.StatusCode},Message:{responseStr}.");

                    if (response.StatusCode != HttpStatusCode.OK && response.StatusCode != HttpStatusCode.Created)
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

        public async Task<string> GarageIdentifier()
        {
            if (_garageIdentifier == null)
            {
                try
                {
                    using (var client = GetClient())
                    {
                        string ApiCall = _config.NGServiceUrl + $"/api/core/garage?tt_location_id={_config.LocationId}";

                        logger.Info("Request Get Garage", "Get Garage", $"GET,Url:{ApiCall}");

                        var response = await client.GetAsync(ApiCall);

                        var responseStr = (await response.Content.ReadAsStringAsync()).ToString();

                        logger.Info("Response Get Garage", "Get Garage", $"Url:{ApiCall},Status:{response.StatusCode},Response:{responseStr}");

                        if ((int)response.StatusCode >= 500 && (int)response.StatusCode <= 599)
                            throw new Exception($"System Error. Status:{response.StatusCode},Message:{responseStr}.");

                        if (response.StatusCode != HttpStatusCode.OK && response.StatusCode != HttpStatusCode.Created)
                            throw new Exception($"Processing error. Code:{response.StatusCode}.Message:{responseStr}.");

                        NGPageResultGarageGet result = JsonConvert.DeserializeObject<NGPageResultGarageGet>(responseStr);

                        if ((result.data?.Length ?? 0) == 0)
                            throw new Exception($"No data was returned");

                        _garageIdentifier = result.data[0].identifier;
                    }
                }
                catch (Exception ex)
                {
                    logger.Error("Error getting garage", "Get Garage", $"Param:{_config.LocationId},Error:{ex}");
                }
            }

            return _garageIdentifier;
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

            //client.DefaultRequestHeaders.TryAddWithoutValidation("Authorization", $"Bearer {_config.NGApiKey}");
            client.DefaultRequestHeaders.Add("x-api-key", _config.NGApiKey);
            client.DefaultRequestHeaders.Add("Accept", "application/json");
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            client.Timeout = new TimeSpan(0, 0, 5);

            return client;
        }
    }
}
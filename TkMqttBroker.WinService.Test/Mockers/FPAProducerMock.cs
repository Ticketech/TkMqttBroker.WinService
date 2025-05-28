using MQTTnet;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TkMqttBroker.WinService.Brokers.FlashPosAvr;

namespace TkMqttBroker.WinService.Test.Mockers
{
    public class FPAProducerMock : FPAProducer
    {
        public static log4net.ITktLog logger = log4net.TktLogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);


        public FPAProducerMock(FPACameraConfiguration configuration)
            : base(configuration)
        { }


        protected override async Task OnUseApplicationMessageReceivedEvent(MqttApplicationMessageReceivedEventArgs data)
        {
            string otherdata = JsonConvert.SerializeObject(data);

            logger.Info("FVR new message", "FVR Message", otherdata);
        }

    }
}

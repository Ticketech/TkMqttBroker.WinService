using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tk.Business.Policies;
using Tk.NetTiers.DataAccessLayer;

namespace TkMqttBroker.WinService.Pos
{
    public static class PosPolicies
    {
        public static LocationPolicies GetCurrentPolicies()
        {
            int count = 0;
            var polloc = DataRepository.PoliciesLocationsProvider.GetPaged(
                $"policyversion = 0 and getdate() between effectivefrom and effectiveto"
                , $"effectivefrom desc", 0, 1, out count).First();

            var policy = JsonConvert.DeserializeObject<LocationPolicies>(polloc.PolicyValue);

            return policy;
        }


        //list of camera ips
        internal static IEnumerable<string> GetFlashPosAvrBrokers()
        {
            throw new NotImplementedException();
        }
    }
}

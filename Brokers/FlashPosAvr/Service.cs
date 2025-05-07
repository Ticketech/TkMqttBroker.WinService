using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;

namespace TkMqttBroker.WinService.Brokers.FlashPosAvr
{
    public partial class FlashPosAvrService : ServiceBase
    {
        private FlashPosAvrBroker _broker;

        public FlashPosAvrService()
        {
            InitializeComponent();
        }

        protected override void OnStart(string[] args)
        {
            _broker = new FlashPosAvrBroker();

            _broker.Start();
        }

        protected override void OnStop()
        {
            _broker.Stop();
        }
    }
}

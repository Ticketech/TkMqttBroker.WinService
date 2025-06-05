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
    //gmz.1.0.0. created.
    public partial class FPAService : ServiceBase
    {
        static readonly log4net.ITktLog logger = log4net.TktLogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);


        private FPABroker _broker;

        public FPAService()
        {
            try
            {
                InitializeComponent();

                FPAInitializer.Initialize();
            }
            catch (Exception ex)
            {
                logger.Error("Error creating service", "Create Service", ex);
            }
        }

        protected override void OnStart(string[] args)
        {
            try
            {
                _broker = new FPABroker();

                Task.Run(async () =>
                {
                    await _broker.Start();
                }).Wait();
            }
            catch (Exception ex)
            {
                logger.Error("Error starting service", "Start Service", ex);
            }
         
            
        }

        protected override void OnStop()
        {
            try
            {
                Task.Run(async () =>
                {
                    await _broker.Stop();
                }).Wait();
            }
            catch (Exception ex)
            {
                logger.Error("Error stopping service", "Stop Service", ex);
            }
          
        }
    }
}

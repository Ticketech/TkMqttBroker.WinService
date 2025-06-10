using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using TkMqttBroker.WinService.Brokers.FlashPosAvr;

namespace TkMqttBroker.WinService.Test.Brokers.FlashPosAvr
{
    [TestClass]
    public class MapperTest
    {
        [TestMethod]
        public void TestEpochMiliseconds()
        {

            var mapper = new FPAMapper();

            DateTime date = DateTime.Now;

            var milis = mapper.EpochMiliseconds(date);
            var sameDate = mapper.UTCFromEpochMiliseconds(milis).ToLocalTime();

            Assert.AreEqual(date.ToString("yyyyMMddHHmmssfff"), sameDate.ToString("yyyyMMddHHmmssfff"));
        }
    }
}

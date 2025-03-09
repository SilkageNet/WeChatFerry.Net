using WeChatFerry.Net.Core;

namespace WeChatFerry.Net.Tests
{
    [TestClass]
    public class SDKManagerTest
    {
        [TestMethod]
        public void Main()
        {
            var skdManager = new SDKManager("path/to/sdk");
            var ret = skdManager.WxInitSDK(false, 6666);
            Assert.AreEqual(0, ret);
            ret = skdManager.WxDestroySDK();
            Assert.AreEqual(0, ret);
        }
    }
}
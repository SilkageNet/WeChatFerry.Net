namespace WeChatFerry.Net.Tests
{
    [TestClass]
    public class WCFSDKTest
    {
        [TestMethod]
        public void Main()
        {
            var skdManager = new SDK("path/to/sdk");
            var ret = skdManager.WxInit(false, 6666);
            Assert.AreEqual(0, ret);
            ret = skdManager.WxDestroy();
            Assert.AreEqual(0, ret);
        }
    }
}
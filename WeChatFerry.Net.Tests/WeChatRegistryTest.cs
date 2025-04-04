namespace WeChatFerry.Net.Tests
{
    [TestClass]
    public class WeChatRegistryTest
    {
        [TestMethod]
        public void ParseVersion()
        {
            Assert.AreEqual(WeChatRegistry.ParseVersion(0x63090c33)?.ToString(), "3.9.12.51");
        }

        [TestMethod]
        public void Set()
        {
            WeChatRegistry.NeedUpdateType = false;
        }
    }
}

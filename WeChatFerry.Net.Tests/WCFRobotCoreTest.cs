namespace WeChatFerry.Net.Tests
{
    [TestClass]
    public class WCFRobotCoreTest
    {
        [TestMethod]
        public async Task Main()
        {
            var robot = new WCFClient();
            Assert.IsNotNull(robot);
            robot.OnRecvMsg += (s, e) => Console.WriteLine($"[{e.Type}] {e.Sender}:{e.Content}");
            await robot.Start();
            robot.SendTxt("filehelper", "Hello, World!");
            await Task.Delay(10000);
        }
    }
}

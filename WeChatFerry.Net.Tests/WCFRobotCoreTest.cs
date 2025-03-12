namespace WeChatFerry.Net.Tests
{
    [TestClass]
    public class WCFRobotCoreTest
    {
        [TestMethod]
        public async Task Main()
        {
            var robot = new WCFRobotCore();
            Assert.IsNotNull(robot);
            robot.OnRecvMsg += (s, e) => Console.WriteLine($"[{e.Type}] {e.Sender}:{e.Content}");
            await robot.Start();
            robot.SendMsg(Message.CreateTxt("filehelper", "Hello, World!"));
            await Task.Delay(10000);
        }
    }
}

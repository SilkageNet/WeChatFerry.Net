using WeChatFerry.Net;

var robot = new WCFRobotCore();
robot.OnRecvMsg += (s, e) => Console.WriteLine($"[{e.Type}] {e.Sender}:{e.Content}");
if (!await robot.Start())
{
    Console.WriteLine("Failed to start the robot.");
    return;
}
robot.SendMsg(Message.CreateTxt("filehelper", "Hello, World!"));
Console.ReadLine();
robot.Stop();
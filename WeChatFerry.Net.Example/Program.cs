using WeChatFerry.Net;

var client = new WCFClient();
client.OnRecvMsg += (s, e) => Console.WriteLine($"[{e.Type}] {e.Sender}:{e.Content}");
if (!await client.Start())
{
    Console.WriteLine("Failed to start the robot.");
    return;
}
client.SendMsg(Message.CreateTxt("filehelper", "Hello, World!"));
Console.ReadLine();
client.Stop();

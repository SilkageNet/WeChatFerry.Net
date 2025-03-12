# WeChatFerry.Net

A [WeChatFerry](https://github.com/lich0821/WeChatFerry) Client SDK based on .NET.

## Usage

```csharp

using WeChatFerry.Net;

public class Program
{
    public static async Task Main(string[] args)
    {
        var robot = new WCFRobotCore();
        robot.OnRecvMsg += (s, e) => Console.WriteLine($"[{e.Type}] {e.Sender}:{e.Content}");
        if (!robot.Start())
        {
            Console.WriteLine("Failed to start the robot.");
            return;
        }
        robot.SendMsg(Message.CreateTxt("filehelper", "Hello, World!"));
        Console.ReadLine();
        robot.Stop();
    }
}

```

## Thanks

- [WeChat](https://www.wechat.com/)
- [WeChatFerry](https://github.com/lich0821/WeChatFerry)

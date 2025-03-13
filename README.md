# WeChatFerry.Net

![NuGet Version](https://img.shields.io/nuget/vpre/WeChatFerry.Net)
![NuGet Downloads](https://img.shields.io/nuget/dt/WeChatFerry.Net)
![License](https://img.shields.io/github/license/SilkageNet/WeChatFerry.Net)

A [WeChatFerry](https://github.com/lich0821/WeChatFerry) Client SDK based on .NET.

## Usage

```csharp

using WeChatFerry.Net;

using var client = new WCFClient();
client.OnRecvMsg += (s, e) => Console.WriteLine($"[{e.Type}] {e.Sender}:{e.Content}");
if (!await client.Start())
{
    Console.WriteLine("Failed to start the robot.");
    return;
}
client.SendMsg(Message.CreateTxt("filehelper", "Hello, World!"));
Console.ReadLine();

```

## Thanks

- [WeChat](https://www.wechat.com/)
- [WeChatFerry](https://github.com/lich0821/WeChatFerry)

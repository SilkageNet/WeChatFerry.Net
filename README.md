# WeChatFerry.Net

![NuGet Version](https://img.shields.io/nuget/vpre/WeChatFerry.Net)
![NuGet Downloads](https://img.shields.io/nuget/dt/WeChatFerry.Net)
![GitHub Workflow Status](https://img.shields.io/github/actions/workflow/status/SilkageNet/WeChatFerry.Net/release-nuget.yml)
![Top Language](https://img.shields.io/github/languages/top/SilkageNet/WeChatFerry.Net)
![License](https://img.shields.io/github/license/SilkageNet/WeChatFerry.Net)

A [WeChatFerry](https://github.com/lich0821/WeChatFerry) Client SDK based on .NET, and you can use it to make a WeChat robot simply.

## Preparation

Install specified version of [WeChat](https://www.wechat.com/). The supported WeChat version depends on the WCF version. Currently, the default version is [3.9.12.17](https://github.com/lich0821/WeChatFerry/releases/download/v39.4.2/WeChatSetup-3.9.12.17.exe). For other WCF versions, you can download them yourself and specify them through `WCFClient.Options`.

## Installation

```bash
dotnet add package WeChatFerry.Net
```

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

## Disclaimer

This project is only for learning and communication, and it is strictly prohibited to use it for commercial purposes. If you have any questions, please contact me to delete it.

## Thanks

- [WeChat](https://www.wechat.com/)
- [WeChatFerry](https://github.com/lich0821/WeChatFerry)

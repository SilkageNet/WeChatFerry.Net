using WeChatFerry.Net;

using var client = new WCFClient();
client.OnRecvMsg += async (s, e) =>
{
    if (e.Type == MessageType.FileOrLink)
    {
        var extra = e.Extra;
        if (string.IsNullOrEmpty(extra))
        {
            extra = $"{e.ID}.tmp";
        }
        var thumb = e.Thumb;
        if (string.IsNullOrEmpty(thumb))
        {
            thumb = $"{e.ID}.thumb.tmp";
        }
        var ok = await client.RPCDonwloadAttachAsync(e.ID, thumb, extra);
        if (!ok)
        {
            Console.WriteLine($"Failed to download file: {e.ID}");
            return;
        }
        Console.WriteLine($"File downloaded: {extra}");
    }
    Console.WriteLine($"Received message from {e.Sender}: {e.Content}");
};
if (!await client.Start())
{
    Console.WriteLine("Failed to start the robot.");
    return;
}
client.SendTxt("filehelper", "Hello, World!");
var selfWxid = await client.RPCGetSelfWxidAsync();
Console.WriteLine($"Self wxid: {selfWxid}");
Console.ReadLine();

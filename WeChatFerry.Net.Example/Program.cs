using WeChatFerry.Net;

using var client = new WCFClient();
client.OnRecvMsg += (s, e) =>
{
    if (e.IsGroup)
    {
        var caller = client.Caller!;
        var dt = caller.ExecDBQuery("MicroMsg.db", $"SELECT RoomData FROM ChatRoom WHERE ChatRoomName = '{e.Roomid}';");
        if (dt is not null)
        {
            for (var i = 0; i < dt.Rows.Count; i++)
            {
                var row = dt.Rows[i];
                Console.WriteLine($"RoomData: {System.Text.Encoding.UTF8.GetString((byte[])(row["RoomData"]))}");
            }
        }
    }
};
if (!await client.Start())
{
    Console.WriteLine("Failed to start the robot.");
    return;
}
client.SendMsg(Message.CreateTxt("filehelper", "Hello, World!"));
var caller = client.Caller!;
var selfWxid = caller.GetSelfWxid();
Console.WriteLine($"Self wxid: {selfWxid}");
var contacts = client.GetContacts();
Console.WriteLine($"Contacts count: {contacts.Count}");
if (contacts.Count > 0)
{
    var dt = caller.ExecDBQuery("MicroMsg.db", $"SELECT NickName FROM Contact WHERE UserName = '{contacts[0].Wxid}';");
    if (dt is not null)
    {
        for (var i = 0; i < dt.Rows.Count; i++)
        {
            var row = dt.Rows[i];
            Console.WriteLine($"NickName: {row["NickName"]}");
        }
    }
}
Console.ReadLine();

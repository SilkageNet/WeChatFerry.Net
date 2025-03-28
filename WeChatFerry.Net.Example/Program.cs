using System.Text.Json;
using WeChatFerry.Net;

using var client = new WCFClient();
client.OnRecvMsg += (s, e) =>
{
    //if (e.IsGroup)
    //{
    //    var dt = client.RPCExecDBQuery("MicroMsg.db", $"SELECT RoomData FROM ChatRoom WHERE ChatRoomName = '{e.Roomid}';");
    //    if (dt is not null)
    //    {
    //        for (var i = 0; i < dt.Rows.Count; i++)
    //        {
    //            var row = dt.Rows[i];
    //            Console.WriteLine($"RoomData: {System.Text.Encoding.UTF8.GetString((byte[])(row["RoomData"]))}");
    //        }
    //    }
    //}
};
if (!await client.Start())
{
    Console.WriteLine("Failed to start the robot.");
    return;
}
//client.SendTxt("filehelper", "Hello, World!");
//var selfWxid = client.RPCGetSelfWxid();
//Console.WriteLine($"Self wxid: {selfWxid}");
//var contacts = client.RPCGetContacts();
//Console.WriteLine($"Contacts count: {contacts.Count}");
//if (contacts.Count > 0)
//{
//    var dt = client.RPCExecDBQuery("MicroMsg.db", $"SELECT NickName FROM Contact WHERE UserName = '{contacts[0].Wxid}';");
//    if (dt is not null)
//    {
//        for (var i = 0; i < dt.Rows.Count; i++)
//        {
//            var row = dt.Rows[i];
//            Console.WriteLine($"NickName: {row["NickName"]}");
//        }
//    }
//}

//var msgTyps = client.RPCGetMsgTypes();
//Console.WriteLine($"MsgTypes: {JsonSerializer.Serialize(msgTyps)}");

var dict = new Dictionary<string, List<DbTable>>();
var dbList = client.RPCGetDBNames();
foreach (var db in dbList)
{
    var tables = client.RPCGetDBTables(db);
    foreach (var table in tables)
    {
        var sql = client.RPCExecDBQuery(db, $"SELECT sql FROM sqlite_master WHERE type = 'table' AND name = '{table.Name}';");
        if (sql != null && sql.Rows.Count > 0)
        {
            table.Sql = sql.Rows[0]["sql"].ToString();
        }
    }

    dict.Add(db, tables);
}
File.WriteAllText("db.json", JsonSerializer.Serialize(dict, new JsonSerializerOptions { WriteIndented = true }));
Console.ReadLine();

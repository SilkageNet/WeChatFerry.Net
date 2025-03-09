using Google.Protobuf;
using NLog;
using nng;
using System.Data;
using System.Reflection;
using System.Xml;
using WeChatFerry.Net.Models;

namespace WeChatFerry.Net.Core
{
    public class WCFClient
    {
        private readonly Logger _logger = LogManager.GetCurrentClassLogger();
        private readonly int _port;
        private readonly IAPIFactory<INngMsg> _factory;
        private readonly IPairSocket _cmdSocket;
        private Task? _loopRecvMsgTask;
        private CancellationTokenSource? _loopRecvMsgCTS;

        public event EventHandler<WxMsg>? OnRecvMsg;

        public WCFClient(int port = 6666)
        {
            _port = port;
            var assemblyPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            var context = new NngLoadContext(assemblyPath);
            _factory = NngLoadContext.Init(context, "nng.Factories.Latest.Factory");
            _cmdSocket = _factory.PairOpen().ThenDial($"tcp://127.0.0.1:{port}").Unwrap();
        }

        public bool IsLogin()
        {
            try
            {
                var req = new Request { Func = Functions.FuncIsLogin };
                var res = CallRPC(req);
                var ok = res.Status == 1;
                if (!ok) _logger.Warn($"IsLogin failed, status: {res.Status}");
                return ok;
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "IsLogin failed");
                return false;
            }
        }

        public void EnableRecvTxt(bool enablePyq = false)
        {
            try
            {
                var req = new Request { Func = Functions.FuncEnableRecvTxt, Flag = enablePyq };
                var res = CallRPC(req);
                if (res.Status != 1) _logger.Warn($"EnableRecvTxt failed, status: {res.Status}");

                if (_loopRecvMsgTask == null || _loopRecvMsgTask.IsCompleted || _loopRecvMsgTask.IsCanceled)
                {
                    _loopRecvMsgCTS = new CancellationTokenSource();
                    _loopRecvMsgTask = Task.Run(() => LoopRecvMsg(_loopRecvMsgCTS.Token));
                }
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "EnableRecvTxt failed");
            }
        }

        public void DisableRecvTxt()
        {
            try
            {
                _loopRecvMsgCTS?.Cancel();
                _loopRecvMsgTask?.Wait();
                _loopRecvMsgCTS = null;
                _loopRecvMsgTask = null;

                var req = new Request { Func = Functions.FuncDisableRecvTxt };
                var res = CallRPC(req);
                if (res.Status != 1) _logger.Warn($"DisableRecvTxt failed, status: {res.Status}");
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "DisableRecvTxt failed");
            }
        }

        public string GetSelfWxid()
        {
            try
            {
                var req = new Request { Func = Functions.FuncGetSelfWxid };
                var res = CallRPC(req);
                return res.Str;
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "GetSelfWxid failed");
                return string.Empty;
            }
        }

        public Dictionary<int, string> GetMsgTypes()
        {
            try
            {
                var req = new Request { Func = Functions.FuncGetMsgTypes };
                var res = CallRPC(req);
                return res.Types_?.Types_?.ToDictionary(x => x.Key, x => x.Value) ?? new Dictionary<int, string>();
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "GetMsgTypes failed");
                return new Dictionary<int, string>();
            }
        }

        public List<RpcContact> GetContacts()
        {
            try
            {
                var req = new Request { Func = Functions.FuncGetContacts };
                var res = CallRPC(req);
                return res.Contacts.Contacts.ToList();
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "GetContacts failed");
                return new List<RpcContact>();
            }
        }

        public List<string> GetDbNames()
        {
            try
            {
                var req = new Request { Func = Functions.FuncGetDbNames };
                var res = CallRPC(req);
                return res.Dbs?.Names?.ToList() ?? [];
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "GetDbNames failed");
                return [];
            }
        }

        public List<DbTable> GetDbTables(string db)
        {
            try
            {
                var req = new Request { Func = Functions.FuncGetDbTables, Str = db };
                var res = CallRPC(req);
                return res.Tables?.Tables.ToList() ?? [];
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "GetDbTables failed");
                return new List<DbTable>();
            }
        }

        public DataTable? ExecDbQuery(string db, string sql)
        {
            try
            {
                var req = new Request { Func = Functions.FuncExecDbQuery, Query = new DbQuery { Db = db, Sql = sql } };
                var res = CallRPC(req);
                if (res.Status != 1)
                {
                    _logger.Warn($"ExecDbQuery failed, status: {res.Status}");
                    return null;
                }
                if (res.Rows == null)
                {
                    _logger.Error("ExecDbQuery failed, rows is null");
                    return null;
                }
                var dt = new DataTable();
                foreach (var r in res.Rows.Rows)
                {
                    var row = dt.NewRow();
                    foreach (var item in r.Fields)
                    {
                        if (!dt.Columns.Contains(item.Column)) dt.Columns.Add(item.Column);
                        row[item.Column] = item.Type == 4 ? item.Content.ToBase64() : item.Content.ToStringUtf8();
                    }
                    dt.Rows.Add(row);
                }
                return dt;
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "ExecDbQuery failed");
                return null;
            }
        }

        public UserInfo? GetSelfInfo()
        {
            try
            {
                var req = new Request { Func = Functions.FuncGetUserInfo };
                var res = CallRPC(req);
                return res.Ui;
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "GetSelfInfo failed");
                return null;
            }
        }


        public bool SendTxt(string receiver, string msg, string aters = "")
        {
            try
            {
                var req = new Request
                {
                    Func = Functions.FuncSendTxt,
                    Txt = new TextMsg
                    {
                        Receiver = receiver,
                        Msg = msg,
                        Aters = aters
                    }
                };
                var res = CallRPC(req);
                var ok = res.Status == 0;
                if (!ok) _logger.Warn($"SendTxt failed, status: {res.Status}");
                return ok;
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "SendTxt failed");
                return false;
            }
        }

        public bool SendImg(string receiver, string path)
        {
            try
            {
                var req = new Request
                {
                    Func = Functions.FuncSendImg,
                    File = new PathMsg
                    {
                        Receiver = receiver,
                        Path = path
                    }
                };
                var res = CallRPC(req);
                var ok = res.Status == 0;
                if (!ok) _logger.Warn($"SendImg failed, status: {res.Status}");
                return ok;
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "SendImg failed");
                return false;
            }
        }

        public bool SendFile(string receiver, string path)
        {
            try
            {
                var req = new Request
                {
                    Func = Functions.FuncSendFile,
                    File = new PathMsg
                    {
                        Receiver = receiver,
                        Path = path
                    }
                };
                var res = CallRPC(req);
                var ok = res.Status == 0;
                if (!ok) _logger.Warn($"SendFile failed, status: {res.Status}");
                return ok;
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "SendFile failed");
                return false;
            }
        }

        public bool SendEmotion(string receiver, string path)
        {
            try
            {
                var req = new Request
                {
                    Func = Functions.FuncSendEmotion,
                    File = new PathMsg
                    {
                        Receiver = receiver,
                        Path = path
                    }
                };
                var res = CallRPC(req);
                var ok = res.Status == 0;
                if (!ok) _logger.Warn($"SendEmotion failed, status: {res.Status}");
                return ok;
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "SendEmotion failed");
                return false;
            }
        }

        public bool ForwardMsg(string receiver, ulong msgID)
        {
            try
            {
                var req = new Request
                {
                    Func = Functions.FuncForwardMsg,
                    Fm = new ForwardMsg
                    {
                        Receiver = receiver,
                        Id = msgID
                    }
                };
                var res = CallRPC(req);
                var ok = res.Status == 1;
                if (!ok) _logger.Warn($"ForwardMsg failed, status: {res.Status}");
                return ok;
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "ForwardMsg failed");
                return false;
            }
        }

        public bool SendRichTxt(RichText richText)
        {
            try
            {
                var req = new Request
                {
                    Func = Functions.FuncSendRichTxt,
                    Rt = richText
                };
                var res = CallRPC(req);
                var ok = res.Status == 0;
                if (!ok) _logger.Warn($"SendRichTxt failed, status: {res.Status}");
                return ok;
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "SendRichTxt failed");
                return false;
            }
        }

        public void AcceptFriend(string content)
        {
            var xml = new XmlDocument();
            xml.LoadXml(content);
            var v3 = xml.SelectSingleNode("/msg/@encryptusername")?.Value ?? "";
            var v4 = xml.SelectSingleNode("/msg/@ticket")?.Value ?? "";
            var scene = int.Parse(xml.SelectSingleNode("/msg/@scene")?.Value ?? "0");
            AcceptFriend(v3, v4, scene);
        }

        public void AcceptFriend(string v3, string v4, int scene)
        {
            try
            {
                var req = new Request { Func = Functions.FuncAcceptFriend, V = new Verification { V3 = v3, V4 = v4, Scene = scene } };
                var res = CallRPC(req);

            }
            catch (Exception ex)
            {
                _logger.Error(ex, "AcceptFriend failed");
            }
        }

        public RpcContact? GetContactInfo(string wxid)
        {
            try
            {
                var req = new Request { Func = Functions.FuncGetContactInfo, Str = wxid };
                var res = CallRPC(req);
                if (res.Status != 1)
                {
                    _logger.Warn($"GetContactInfo failed, status: {res.Status}");
                    return null;
                }
                return res.Contacts?.Contacts?.FirstOrDefault();
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "GetContactInfo failed");
                return null;
            }
        }

        public bool DonwloadAttach(ulong id, string thumb, string extra)
        {
            try
            {
                var req = new Request { Func = Functions.FuncDownloadAttach, Att = new AttachMsg { Id = id, Thumb = thumb, Extra = extra } };
                var res = CallRPC(req);
                var ok = res.Status == 0;
                return res.Status == 0;
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "DonwloadAttach failed");
                return false;
            }
        }

        public string DecryptImage(string src, string dst)
        {
            try
            {
                var req = new Request { Func = Functions.FuncDecryptImage, Dec = new DecPath { Src = src, Dst = dst } };
                var res = CallRPC(req);
                return res.Str;
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "DecryptImage failed");
                return string.Empty;
            }
        }

        public string DownloadImage(ulong id, string extra, string dst, int tryLimit = 30)
        {
            try
            {
                if (!DonwloadAttach(id, "", extra))
                {
                    _logger.Info("DonwloadAttach failed");
                    return string.Empty;
                }
                while (tryLimit-- > 0)
                {
                    var path = DecryptImage(extra, dst);
                    if (!string.IsNullOrEmpty(path)) return path;
                    Thread.Sleep(1000);
                }
                return string.Empty;
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "DownloadImage failed");
                return string.Empty;
            }
        }

        private Response CallRPC(Request request)
        {
            if (!_cmdSocket.IsValid()) throw new Exception("CMD socket is not valid");
            _cmdSocket.Send(request.ToByteArray());
            var msg = _cmdSocket.RecvMsg().Unwrap();
            var data = msg.AsSpan().ToArray();
            return Response.Parser.ParseFrom(data);
        }

        private void LoopRecvMsg(CancellationToken token)
        {
            try
            {
                var msgSocket = _factory.PairOpen().ThenDial($"tcp://127.0.0.1:{_port + 1}").Unwrap();
                if (msgSocket == null) return;

                while (!token.IsCancellationRequested)
                {
                    var msg = msgSocket.RecvMsg().Unwrap();
                    var data = msg.AsSpan().ToArray();
                    var res = Response.Parser.ParseFrom(data);
                    OnRecvMsg?.Invoke(this, res.Wxmsg);
                }
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "LoopRecvMsg failed");
            }
        }
    }
}

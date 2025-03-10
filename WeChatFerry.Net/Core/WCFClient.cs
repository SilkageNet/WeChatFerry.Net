using Google.Protobuf;
using NLog;
using nng;
using System.Data;
using System.Reflection;
using System.Xml;
using WeChatFerry.Net.Models;

namespace WeChatFerry.Net.Core
{
    public class WCFClient : IDisposable
    {
        private readonly Logger _logger = LogManager.GetCurrentClassLogger();
        private readonly int _port;
        private readonly IAPIFactory<INngMsg> _factory;
        private readonly IPairSocket _cmdSocket;
        private Task? _loopRecvMsgTask;
        //private CancellationTokenSource? _loopRecvMsgCTS;

        public event EventHandler<WxMsg>? OnRecvMsg;

        public WCFClient(int port = 6666)
        {
            _port = port;
            var assemblyPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            var context = new NngLoadContext(assemblyPath);
            _factory = NngLoadContext.Init(context, "nng.Factories.Latest.Factory");
            _cmdSocket = _factory.PairOpen().ThenDial($"tcp://127.0.0.1:{port}").Unwrap();
        }

        public void Start()
        {

        }

        public void Dispose()
        {

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

        #region RPC Functions
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

        public UserInfo? GetUserInfo()
        {
            try
            {
                var req = new Request { Func = Functions.FuncGetUserInfo };
                var res = CallRPC(req);
                return res.Ui;
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "GetUserInfo failed");
                return null;
            }
        }

        public Dictionary<int, string> GetMsgTypes()
        {
            try
            {
                var req = new Request { Func = Functions.FuncGetMsgTypes };
                var res = CallRPC(req);
                return res.Types_?.Types_?.ToDictionary(x => x.Key, x => x.Value) ?? [];
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "GetMsgTypes failed");
                return new Dictionary<int, string>();
            }
        }


        public bool EnableRecvTxt(bool enablePyq = false)
        {
            try
            {
                var req = new Request { Func = Functions.FuncEnableRecvTxt, Flag = enablePyq };
                var res = CallRPC(req);
                var ok = res.Status == 0;
                if (!ok) _logger.Warn($"EnableRecvTxt failed, status: {res.Status}");
                return ok;
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "EnableRecvTxt failed");
                return false;
            }
        }

        public bool DisableRecvTxt()
        {
            try
            {
                var req = new Request { Func = Functions.FuncDisableRecvTxt };
                var res = CallRPC(req);
                var ok = res.Status == 0;
                if (!ok) _logger.Warn($"DisableRecvTxt failed, status: {res.Status}");
                return ok;
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "DisableRecvTxt failed");
                return false;
            }
        }

        public List<RpcContact> GetContacts()
        {
            try
            {
                var req = new Request { Func = Functions.FuncGetContacts };
                var res = CallRPC(req);
                return [.. res.Contacts.Contacts];
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

        public string GetAudioMsg(ulong id, string dir)
        {
            try
            {
                var req = new Request { Func = Functions.FuncGetAudioMsg, Am = new AudioMsg { Id = id, Dir = dir } };
                var res = CallRPC(req);
                return res.Str;
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "GetAudioMsg failed");
                return string.Empty;
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

        [Obsolete("Not implemented in WeChatFerry")]
        public bool SendXml(string receiver, string path, string content, ulong type)
        {
            try
            {
                var req = new Request
                {
                    Func = Functions.FuncSendXml,
                    Xml = new XmlMsg
                    {
                        Receiver = receiver,
                        Path = path,
                        Content = content,
                        Type = type
                    }
                };
                var res = CallRPC(req);
                var ok = res.Status == 0;
                if (!ok) _logger.Warn($"SendXml failed, status: {res.Status}");
                return ok;
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "SendXml failed");
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

        public bool SendPatMsg(string roomID, string wxid)
        {
            try
            {
                var req = new Request
                {
                    Func = Functions.FuncSendPatMsg,
                    Pm = new PatMsg { Roomid = roomID, Wxid = wxid }
                };
                var res = CallRPC(req);
                var ok = res.Status == 0;
                if (!ok) _logger.Warn($"SendPat failed, status: {res.Status}");
                return ok;
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "SendPat failed");
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

        [Obsolete("Not implemented in WeChatFerry")]
        public bool AcceptFriend(string v3, string v4, int scene)
        {
            try
            {
                var req = new Request { Func = Functions.FuncAcceptFriend, V = new Verification { V3 = v3, V4 = v4, Scene = scene } };
                var res = CallRPC(req);
                var ok = res.Status == 1;
                if (!ok) _logger.Warn($"AcceptFriend failed, status: {res.Status}");
                return ok;
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "AcceptFriend failed");
                return false;
            }
        }

        [Obsolete("Not implemented in WeChatFerry")]
        public bool RecvTransfer(string wxid, string tfid, string taid)
        {
            try
            {
                var req = new Request
                {
                    Func = Functions.FuncRecvTransfer,
                    Tf = new Transfer
                    {
                        Wxid = wxid,
                        Tfid = tfid,
                        Taid = taid
                    }
                };
                var res = CallRPC(req);
                var ok = res.Status == 0;
                if (!ok) _logger.Warn($"RecvTransfer failed, status: {res.Status}");
                return ok;
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "RecvTransfer failed");
                return false;
            }
        }

        public bool RefreshPyq(ulong id = 0)
        {
            try
            {
                var req = new Request { Func = Functions.FuncRefreshPyq, Ui64 = id };
                var res = CallRPC(req);
                var ok = res.Status == 1;
                if (!ok) _logger.Warn($"RefreshPyq failed, status: {res.Status}");
                return ok;
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "RefreshPyq failed");
                return false;
            }
        }

        public bool DonwloadAttach(ulong id, string thumb, string extra)
        {
            try
            {
                var req = new Request { Func = Functions.FuncDownloadAttach, Att = new AttachMsg { Id = id, Thumb = thumb, Extra = extra } };
                var res = CallRPC(req);
                var ok = res.Status == 0;
                if (!ok) _logger.Warn($"DonwloadAttach failed, status: {res.Status}");
                return ok;
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "DonwloadAttach failed");
                return false;
            }
        }

        [Obsolete("Not implemented in WeChatFerry")]
        public RpcContact? GetContactInfo(string wxid)
        {
            try
            {
                var req = new Request { Func = Functions.FuncGetContactInfo, Str = wxid };
                var res = CallRPC(req);
                return res.Contacts?.Contacts?.FirstOrDefault();
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "GetContactInfo failed");
                return null;
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

        #endregion

        private Response CallRPC(Request request)
        {
            if (!_cmdSocket.IsValid()) throw new Exception("CMD socket is not valid");
            _cmdSocket.Send(request.ToByteArray());
            var msg = _cmdSocket.RecvMsg().Unwrap();
            var data = msg.AsSpan().ToArray();
            return Response.Parser.ParseFrom(data);
        }



    }
}

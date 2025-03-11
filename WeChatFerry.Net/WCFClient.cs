using Google.Protobuf;
using NLog;
using nng;
using System.Data;
using System.Diagnostics;
using System.Reflection;
using System.Xml;
using WeChatFerry.Net.Models;

namespace WeChatFerry.Net
{
    public class WCFClient(IPairSocket cmdSocket)
    {
        private readonly Logger _logger = LogManager.GetCurrentClassLogger();

        private Response CallRPC(Request request)
        {
            if (!cmdSocket.IsValid()) throw new Exception("CMD socket is not valid");
            cmdSocket.Send(request.ToByteArray());
            var msg = cmdSocket.RecvMsg().Unwrap();
            var data = msg.AsSpan().ToArray();
            return Response.Parser.ParseFrom(data);
        }

        #region Login
        public async Task<bool> WaitForLogin(int timeout = 60)
        {
            try
            {
                var sw = Stopwatch.StartNew();
                while (sw.Elapsed.TotalSeconds < timeout)
                {
                    if (IsLogin()) return true;
                    await Task.Delay(1000);
                }
                return false;
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "WaitForLogin failed");
                return false;
            }
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

        public string RefreshQrCode()
        {
            try
            {
                var req = new Request { Func = Functions.FuncRefreshQrcode };
                var res = CallRPC(req);
                return res.Str;
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "RefreshQrCode failed");
                return string.Empty;
            }
        }
        #endregion

        #region Enable/Disable Receive Messages
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

        #endregion

        #region Get Self Info
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

        #endregion

        #region DB
        public List<string> GetDBNames()
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

        public List<DbTable> GetDBTables(string db)
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
                return [];
            }
        }

        public DataTable? ExecDBQuery(string db, string sql)
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
        #endregion

        #region Send Messages
        public bool SendTxt(string receiver, string msg, List<string>? aters = null)
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
                        Aters = aters?.Count > 0 ? string.Join(",", aters) : ""
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

        #endregion

        #region Read Messages
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

        #region Tools
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
                return [];
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
                return [];
            }
        }
        #endregion

        #region Room
        public bool AddRoomMembers(string roomID, List<string> wxids)
        {
            try
            {
                var req = new Request { Func = Functions.FuncAddRoomMembers, M = new MemberMgmt { Roomid = roomID, Wxids = string.Join(",") } };
                var res = CallRPC(req);
                var ok = res.Status == 0;
                if (!ok) _logger.Warn($"AddRoomMembers failed, status: {res.Status}");
                return ok;
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "AddRoomMembers failed");
                return false;
            }
        }

        public bool DelRoomMembers(string roomID, List<string> wxids)
        {
            try
            {
                var req = new Request { Func = Functions.FuncDelRoomMembers, M = new MemberMgmt { Roomid = roomID, Wxids = string.Join(",") } };
                var res = CallRPC(req);
                var ok = res.Status == 0;
                if (!ok) _logger.Warn($"DelRoomMembers failed, status: {res.Status}");
                return ok;
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "DelRoomMembers failed");
                return false;
            }
        }

        public bool InvRoomMembers(string roomID, List<string> wxids)
        {
            try
            {
                var req = new Request { Func = Functions.FuncInvRoomMembers, M = new MemberMgmt { Roomid = roomID, Wxids = string.Join(",") } };
                var res = CallRPC(req);
                var ok = res.Status == 0;
                if (!ok) _logger.Warn($"FuncInvRoomMembers failed, status: {res.Status}");
                return ok;
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "FuncInvRoomMembers failed");
                return false;
            }
        }
        #endregion

        #region Obsolete
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

        [Obsolete("Not implemented in WeChatFerry")]
        public bool RevokeMsg(ulong id)
        {
            try
            {
                var req = new Request { Func = Functions.FuncRevokeMsg, Ui64 = id };
                var res = CallRPC(req);
                var ok = res.Status == 0;
                if (!ok) _logger.Warn($"RevokeMsg failed, status: {res.Status}");
                return ok;
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "RevokeMsg failed");
                return false;
            }
        }

        [Obsolete("Not implemented in WeChatFerry")]
        public OcrMsg? ExecOCR(string path)
        {
            try
            {
                var req = new Request { Func = Functions.FuncExecOcr, Str = path };
                var res = CallRPC(req);
                return res.Ocr;
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "ExecOCR failed");
                return null;
            }
        }
        #endregion

    }
}

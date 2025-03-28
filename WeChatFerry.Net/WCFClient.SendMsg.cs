namespace WeChatFerry.Net
{
    public partial class WCFClient
    {
        public bool RPCSendTxt(string receiver, string msg, List<string>? aters = null)
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
                if (!ok) _logger?.Warn($"SendTxt failed, status: {res.Status}");
                return ok;
            }
            catch (Exception ex)
            {
                _logger?.Error("SendTxt failed: {0}", ex);
                return false;
            }
        }

        public bool RPCSendImg(string receiver, string path)
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
                if (!ok) _logger?.Warn($"SendImg failed, status: {res.Status}");
                return ok;
            }
            catch (Exception ex)
            {
                _logger?.Error("SendImg failed: {0}", ex);
                return false;
            }
        }

        public bool RPCSendFile(string receiver, string path)
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
                if (!ok) _logger?.Warn($"SendFile failed, status: {res.Status}");
                return ok;
            }
            catch (Exception ex)
            {
                _logger?.Error("SendFile failed: {0}", ex);
                return false;
            }
        }

        public bool RPCSendEmotion(string receiver, string path)
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
                if (!ok) _logger?.Warn($"SendEmotion failed, status: {res.Status}");
                return ok;
            }
            catch (Exception ex)
            {
                _logger?.Error("SendEmotion failed: {0}", ex);
                return false;
            }
        }

        public bool RPCSendRichTxt(RichText richText)
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
                if (!ok) _logger?.Warn($"SendRichTxt failed, status: {res.Status}");
                return ok;
            }
            catch (Exception ex)
            {
                _logger?.Error("SendRichTxt failed: {0}", ex);
                return false;
            }
        }

        public bool RPCSendPatMsg(string roomID, string wxid)
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
                if (!ok) _logger?.Warn($"SendPat failed, status: {res.Status}");
                return ok;
            }
            catch (Exception ex)
            {
                _logger?.Error("SendPatMsg failed: {0}", ex);
                return false;
            }
        }

        public bool RPCSendXml(string receiver, string path, string content, ulong type)
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
                if (!ok) _logger?.Warn("SendXml failed, status: {0}", res.Status);
                return ok;
            }
            catch (Exception ex)
            {
                _logger?.Error("SendXml failed: {0}", ex);
                return false;
            }
        }

        public bool RPCForwardMsg(string receiver, ulong msgID)
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
                if (!ok) _logger?.Warn($"ForwardMsg failed, status: {res.Status}");
                return ok;
            }
            catch (Exception ex)
            {
                _logger?.Error("ForwardMsg failed: {0}", ex);
                return false;
            }
        }

        public bool RPCRevokeMsg(ulong id)
        {
            try
            {
                var req = new Request { Func = Functions.FuncRevokeMsg, Ui64 = id };
                var res = CallRPC(req);
                var ok = res.Status == 0;
                if (!ok) _logger?.Warn($"RevokeMsg failed, status: {res.Status}");
                return ok;
            }
            catch (Exception ex)
            {
                _logger?.Error("RevokeMsg failed: {0}", ex);
                return false;
            }
        }

        public void SendTxt(string receiver, string content, List<string>? aters = null) => _msgQueue.Enqueue(PendingMessage.CreateTxt(receiver, content, aters));

        public void SendImg(string receiver, string content) => _msgQueue.Enqueue(PendingMessage.CreateImg(receiver, content));

        public void SendFile(string receiver, string content) => _msgQueue.Enqueue(PendingMessage.CreateFile(receiver, content));

        public void SendEmotion(string receiver, string content) => _msgQueue.Enqueue(PendingMessage.CreateEmotion(receiver, content));

        public void SendRichTxt(string receiver, RichText richText) => _msgQueue.Enqueue(PendingMessage.CreateRichText(receiver, richText));

        public void SendPatMsg(string receiver, string patWxid) => _msgQueue.Enqueue(PendingMessage.CreatePatMsg(receiver, patWxid));

        public void SendForward(string receiver, ulong forwardMsgID) => _msgQueue.Enqueue(PendingMessage.CreateForward(receiver, forwardMsgID));

        public void SendXml(string receiver, string path, string content, ulong type) => _msgQueue.Enqueue(PendingMessage.CreateXml(receiver, path, content, type));
    }
}

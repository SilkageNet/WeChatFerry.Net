namespace WeChatFerry.Net
{
    public partial class WCFClient
    {
        public string RPCGetAudioMsg(ulong id, string dir)
        {
            try
            {
                var req = new Request { Func = Functions.FuncGetAudioMsg, Am = new AudioMsg { Id = id, Dir = dir } };
                var res = CallRPC(req);
                return res.Str;
            }
            catch (Exception ex)
            {
                _logger?.Error("GetAudioMsg failed: {0}", ex);
                return string.Empty;
            }
        }

        public async Task<string> RPCGetAudioMsgAsync(ulong id, string dir, CancellationTokenSource? cts = null)
        {
            try
            {
                var req = new Request { Func = Functions.FuncGetAudioMsg, Am = new AudioMsg { Id = id, Dir = dir } };
                var res = await CallRPCAsync(req, cts);
                return res.Str;
            }
            catch (Exception ex)
            {
                _logger?.Error("GetAudioMsg failed: {0}", ex);
                return string.Empty;
            }
        }

        public bool RPCDonwloadAttach(ulong id, string thumb, string extra)
        {
            try
            {
                var req = new Request { Func = Functions.FuncDownloadAttach, Att = new AttachMsg { Id = id, Thumb = thumb, Extra = extra } };
                var res = CallRPC(req);
                var ok = res.Status == 0;
                if (!ok) _logger?.Warn($"DonwloadAttach failed, status: {res.Status}");
                return ok;
            }
            catch (Exception ex)
            {
                _logger?.Error("DonwloadAttach failed: {0}", ex);
                return false;
            }
        }

        public async Task<bool> RPCDonwloadAttachAsync(ulong id, string thumb, string extra, CancellationTokenSource? cts = null)
        {
            try
            {
                var req = new Request { Func = Functions.FuncDownloadAttach, Att = new AttachMsg { Id = id, Thumb = thumb, Extra = extra } };
                var res = await CallRPCAsync(req, cts);
                var ok = res.Status == 0;
                if (!ok) _logger?.Warn($"DonwloadAttach failed, status: {res.Status}");
                return ok;
            }
            catch (Exception ex)
            {
                _logger?.Error("DonwloadAttach failed: {0}", ex);
                return false;
            }
        }

        public string RPCDecryptImage(string src, string dst)
        {
            try
            {
                var req = new Request { Func = Functions.FuncDecryptImage, Dec = new DecPath { Src = src, Dst = dst } };
                var res = CallRPC(req);
                return res.Str;
            }
            catch (Exception ex)
            {
                _logger?.Error("DecryptImage failed: {0}", ex);
                return string.Empty;
            }
        }

        public async Task<string> RPCDecryptImageAsync(string src, string dst, CancellationTokenSource? cts = null)
        {
            try
            {
                var req = new Request { Func = Functions.FuncDecryptImage, Dec = new DecPath { Src = src, Dst = dst } };
                var res = await CallRPCAsync(req, cts);
                return res.Str;
            }
            catch (Exception ex)
            {
                _logger?.Error("DecryptImage failed: {0}", ex);
                return string.Empty;
            }
        }

        public string DownloadImage(ulong id, string extra, string dst, int tryLimit = 30)
        {
            try
            {
                if (!RPCDonwloadAttach(id, "", extra))
                {
                    _logger?.Info("DonwloadAttach failed");
                    return string.Empty;
                }
                while (tryLimit-- > 0)
                {
                    var path = RPCDecryptImage(extra, dst);
                    if (!string.IsNullOrEmpty(path)) return path;
                    Thread.Sleep(1000);
                }
                return string.Empty;
            }
            catch (Exception ex)
            {
                _logger?.Error("DownloadImage failed: {0}", ex);
                return string.Empty;
            }
        }

        public async Task<string> DownloadImageAsync(ulong id, string extra, string dst, CancellationTokenSource? cts = null)
        {
            try
            {
                if (!await RPCDonwloadAttachAsync(id, "", extra, cts))
                {
                    _logger?.Info("DonwloadAttach failed");
                    return string.Empty;
                }
                var path = await RPCDecryptImageAsync(extra, dst, cts);
                return path;
            }
            catch (Exception ex)
            {
                _logger?.Error("DownloadImage failed: {0}", ex);
                return string.Empty;
            }
        }
    }
}
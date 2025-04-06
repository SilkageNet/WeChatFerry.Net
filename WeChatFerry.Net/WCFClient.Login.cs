using System.Diagnostics;

namespace WeChatFerry.Net
{
    public partial class WCFClient
    {
        public async Task<bool> WaitForLogin(int timeout = 60, CancellationTokenSource? cts = null)
        {
            try
            {
                var sw = Stopwatch.StartNew();
                while (sw.Elapsed.TotalSeconds < timeout)
                {
                    if(await RPCIsLoginAsync(cts)) return true;
                    await Task.Delay(1000);
                }
                return false;
            }
            catch (Exception ex)
            {
                _logger?.Error("WaitForLogin failed: {0}", ex);
                return false;
            }
        }

        public bool RPCIsLogin()
        {
            try
            {
                var req = new Request { Func = Functions.FuncIsLogin };
                var res = CallRPC(req);
                var ok = res.Status == 1;
                if (!ok) _logger?.Warn($"IsLogin failed, status: {res.Status}");
                return ok;
            }
            catch (Exception ex)
            {
                _logger?.Error("IsLogin failed: {0}", ex);
                return false;
            }
        }

        public async Task<bool> RPCIsLoginAsync(CancellationTokenSource? cts = null)
        {
            try
            {
                var req = new Request { Func = Functions.FuncIsLogin };
                var res = await CallRPCAsync(req, cts);
                var ok = res.Status == 1;
                if (!ok) _logger?.Warn($"IsLogin failed, status: {res.Status}");
                return ok;
            }
            catch (Exception ex)
            {
                _logger?.Error("IsLogin failed: {0}", ex);
                return false;
            }
        }

        public string RPCRefreshQrCode()
        {
            try
            {
                var req = new Request { Func = Functions.FuncRefreshQrcode };
                var res = CallRPC(req);
                return res.Str;
            }
            catch (Exception ex)
            {
                _logger?.Error("RefreshQrCode failed: {0}", ex);
                return string.Empty;
            }
        }

        public async Task<string> RPCRefreshQrCodeAsync(CancellationTokenSource? cts = null)
        {
            try
            {
                var req = new Request { Func = Functions.FuncRefreshQrcode };
                var res = await CallRPCAsync(req, cts);
                return res.Str;
            }
            catch (Exception ex)
            {
                _logger?.Error("RefreshQrCode failed: {0}", ex);
                return string.Empty;
            }
        }

        public string RPCGetSelfWxid()
        {
            try
            {
                var req = new Request { Func = Functions.FuncGetSelfWxid };
                var res = CallRPC(req);
                return res.Str;
            }
            catch (Exception ex)
            {
                _logger?.Error("GetSelfWxid failed: {0}", ex);
                return string.Empty;
            }
        }

        public async Task<string> RPCGetSelfWxidAsync(CancellationTokenSource? cts = null)
        {
            try
            {
                var req = new Request { Func = Functions.FuncGetSelfWxid };
                var res = await CallRPCAsync(req, cts);
                return res.Str;
            }
            catch (Exception ex)
            {
                _logger?.Error("GetSelfWxid failed: {0}", ex);
                return string.Empty;
            }
        }

        public UserInfo? RPCGetUserInfo()
        {
            try
            {
                var req = new Request { Func = Functions.FuncGetUserInfo };
                var res = CallRPC(req);
                return res.Ui;
            }
            catch (Exception ex)
            {
                _logger?.Error("GetUserInfo failed: {0}", ex);
                return null;
            }
        }

        public async Task<UserInfo?> RPCGetUserInfoAsync(CancellationTokenSource? cts = null)
        {
            try
            {
                var req = new Request { Func = Functions.FuncGetUserInfo };
                var res = await CallRPCAsync(req, cts);
                return res.Ui;
            }
            catch (Exception ex)
            {
                _logger?.Error("GetUserInfo failed: {0}", ex);
                return null;
            }
        }
    }
}

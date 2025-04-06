namespace WeChatFerry.Net
{
    public partial class WCFClient
    {
        public bool RPCEnableRecvTxt(bool enablePyq = false)
        {
            try
            {
                var req = new Request { Func = Functions.FuncEnableRecvTxt, Flag = enablePyq };
                var res = CallRPC(req);
                var ok = res.Status == 0;
                if (!ok) _logger?.Warn($"EnableRecvTxt failed, status: {res.Status}");
                return ok;
            }
            catch (Exception ex)
            {
                _logger?.Error("EnableRecvTxt failed: {0}", ex);
                return false;
            }
        }

        public async Task<bool> RPCEnableRecvTxtAsync(bool enablePyq = false, CancellationTokenSource? cts = null)
        {
            try
            {
                var req = new Request { Func = Functions.FuncEnableRecvTxt, Flag = enablePyq };
                var res = await CallRPCAsync(req, cts);
                var ok = res.Status == 0;
                if (!ok) _logger?.Warn($"EnableRecvTxt failed, status: {res.Status}");
                return ok;
            }
            catch (Exception ex)
            {
                _logger?.Error("EnableRecvTxt failed: {0}", ex);
                return false;
            }
        }

        public bool RPCDisableRecvTxt()
        {
            try
            {
                var req = new Request { Func = Functions.FuncDisableRecvTxt };
                var res = CallRPC(req);
                var ok = res.Status == 0;
                if (!ok) _logger?.Warn($"DisableRecvTxt failed, status: {res.Status}");
                return ok;
            }
            catch (Exception ex)
            {
                _logger?.Error("DisableRecvTxt failed: {0}", ex);
                return false;
            }
        }

        public async Task<bool> RPCDisableRecvTxtAsync(CancellationTokenSource? cts = null)
        {
            try
            {
                var req = new Request { Func = Functions.FuncDisableRecvTxt };
                var res = await CallRPCAsync(req, cts);
                var ok = res.Status == 0;
                if (!ok) _logger?.Warn($"DisableRecvTxt failed, status: {res.Status}");
                return ok;
            }
            catch (Exception ex)
            {
                _logger?.Error("DisableRecvTxt failed: {0}", ex);
                return false;
            }
        }
    }
}
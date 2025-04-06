namespace WeChatFerry.Net
{
    public partial class WCFClient
    {
        public bool RPCRefreshPyq(ulong id = 0)
        {
            try
            {
                var req = new Request { Func = Functions.FuncRefreshPyq, Ui64 = id };
                var res = CallRPC(req);
                var ok = res.Status == 1;
                if (!ok) _logger?.Warn($"RefreshPyq failed, status: {res.Status}");
                return ok;
            }
            catch (Exception ex)
            {
                _logger?.Error("RefreshPyq failed: {0}", ex);
                return false;
            }
        }

        public async Task<bool> RPCRefreshPyqAsync(ulong id = 0, CancellationTokenSource? cts = null)
        {
            try
            {
                var req = new Request { Func = Functions.FuncRefreshPyq, Ui64 = id };
                var res = await CallRPCAsync(req, cts);
                var ok = res.Status == 1;
                if (!ok) _logger?.Warn($"RefreshPyq failed, status: {res.Status}");
                return ok;
            }
            catch (Exception ex)
            {
                _logger?.Error("RefreshPyq failed: {0}", ex);
                return false;
            }
        }

        public Dictionary<int, string> RPCGetMsgTypes()
        {
            try
            {
                var req = new Request { Func = Functions.FuncGetMsgTypes };
                var res = CallRPC(req);
                return res.Types_?.Types_?.ToDictionary(x => x.Key, x => x.Value) ?? [];
            }
            catch (Exception ex)
            {
                _logger?.Error("GetMsgTypes failed: {0}", ex);
                return [];
            }
        }

        public async Task<Dictionary<int, string>> RPCGetMsgTypesAsync(CancellationTokenSource? cts = null)
        {
            try
            {
                var req = new Request { Func = Functions.FuncGetMsgTypes };
                var res = await CallRPCAsync(req, cts);
                return res.Types_?.Types_?.ToDictionary(x => x.Key, x => x.Value) ?? [];
            }
            catch (Exception ex)
            {
                _logger?.Error("GetMsgTypes failed: {0}", ex);
                return [];
            }
        }


        public bool RPCRecvTransfer(string wxid, string tfid, string taid)
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
                if (!ok) _logger?.Warn("RecvTransfer failed, status: {0}", res.Status);
                return ok;
            }
            catch (Exception ex)
            {
                _logger?.Error("RecvTransfer failed: {0}", ex);
                return false;
            }
        }

        public async Task<bool> RPCRecvTransferAsync(string wxid, string tfid, string taid, CancellationTokenSource? cts = null)
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
                var res = await CallRPCAsync(req, cts);
                var ok = res.Status == 0;
                if (!ok) _logger?.Warn("RecvTransfer failed, status: {0}", res.Status);
                return ok;
            }
            catch (Exception ex)
            {
                _logger?.Error("RecvTransfer failed: {0}", ex);
                return false;
            }
        }


        public OcrMsg? RPCExecOCR(string path)
        {
            try
            {
                var req = new Request { Func = Functions.FuncExecOcr, Str = path };
                var res = CallRPC(req);
                return res.Ocr;
            }
            catch (Exception ex)
            {
                _logger?.Error("ExecOCR failed: {0}", ex);
                return null;
            }
        }

        public async Task<OcrMsg?> RPCExecOCRAsync(string path, CancellationTokenSource? cts = null)
        {
            try
            {
                var req = new Request { Func = Functions.FuncExecOcr, Str = path };
                var res = await CallRPCAsync(req, cts);
                return res.Ocr;
            }
            catch (Exception ex)
            {
                _logger?.Error("ExecOCR failed: {0}", ex);
                return null;
            }
        }
    }
}
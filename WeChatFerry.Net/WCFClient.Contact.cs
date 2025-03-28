namespace WeChatFerry.Net
{
    public partial class WCFClient
    {
        public List<RpcContact> RPCGetContacts()
        {
            try
            {
                var req = new Request { Func = Functions.FuncGetContacts };
                var res = CallRPC(req);
                return [.. res.Contacts.Contacts];
            }
            catch (Exception ex)
            {
                _logger?.Error("GetContacts failed: {0}", ex);
                return [];
            }
        }

        public RpcContact? RPCGetContactInfo(string wxid)
        {
            try
            {
                var req = new Request { Func = Functions.FuncGetContactInfo, Str = wxid };
                var res = CallRPC(req);
                return res.Contacts?.Contacts?.FirstOrDefault();
            }
            catch (Exception ex)
            {
                _logger?.Error("GetContactInfo failed: {0}", ex);
                return null;
            }
        }
    }
}

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

        /// <summary>
        /// Get a contact by wxid.
        /// </summary>
        /// <param name="wxid"></param>
        /// <returns></returns>
        public Contact? GetContact(string wxid)
        {
            if (!_contacts.TryGetValue(wxid, out var contact)) return null;
            return contact;
        }

        /// <summary>
        /// Get all contacts.
        /// </summary>
        /// <returns></returns>
        public List<Contact> GetContacts() => [.. _contacts.Values];

        /// <summary>
        /// Refresh contacts.
        /// </summary>
        public void RefreshContacts()
        {
            var contacts = RPCGetContacts();
            if (contacts == null) return;
            contacts.ForEach(c =>
            {
                if (_contacts.TryGetValue(c.Wxid, out var contact)) contact.Update(c);
                else _contacts.TryAdd(c.Wxid, new Contact(c));
            });
        }

    }
}

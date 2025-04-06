using System.Xml;

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

        public async Task<List<RpcContact>> RPCGetContactsAsync(CancellationTokenSource? cts = null)
        {
            try
            {
                var req = new Request { Func = Functions.FuncGetContacts };
                var res = await CallRPCAsync(req, cts);
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

        public async Task<RpcContact?> RPCGetContactInfoAsync(string wxid, CancellationTokenSource? cts = null)
        {
            try
            {
                var req = new Request { Func = Functions.FuncGetContactInfo, Str = wxid };
                var res = await CallRPCAsync(req, cts);
                return res.Contacts?.Contacts?.FirstOrDefault();
            }
            catch (Exception ex)
            {
                _logger?.Error("GetContactInfo failed: {0}", ex);
                return null;
            }
        }

        public bool RPCAcceptFriend(string v3, string v4, int scene)
        {
            try
            {
                var req = new Request { Func = Functions.FuncAcceptFriend, V = new Verification { V3 = v3, V4 = v4, Scene = scene } };
                var res = CallRPC(req);
                var ok = res.Status == 1;
                if (!ok) _logger?.Warn("AcceptFriend failed, status: {0}", res.Status);
                return ok;
            }
            catch (Exception ex)
            {
                _logger?.Error("AddRoomMembers failed: {0}", ex);
                return false;
            }
        }

        public async Task<bool> RPCAcceptFriendAsync(string v3, string v4, int scene, CancellationTokenSource? cts = null)
        {
            try
            {
                var req = new Request { Func = Functions.FuncAcceptFriend, V = new Verification { V3 = v3, V4 = v4, Scene = scene } };
                var res = await CallRPCAsync(req, cts);
                var ok = res.Status == 1;
                if (!ok) _logger?.Warn("AcceptFriend failed, status: {0}", res.Status);
                return ok;
            }
            catch (Exception ex)
            {
                _logger?.Error("AddRoomMembers failed: {0}", ex);
                return false;
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

        /// <summary>
        /// Refresh contacts asynchronously.
        /// </summary>
        /// <param name="cts"></param>
        /// <returns></returns>
        public async Task RefreshContactsAsync(CancellationTokenSource? cts = null)
        {
            var contacts = await RPCGetContactsAsync(cts);
            if (contacts == null) return;
            contacts.ForEach(c =>
            {
                if (_contacts.TryGetValue(c.Wxid, out var contact)) contact.Update(c);
                else _contacts.TryAdd(c.Wxid, new Contact(c));
            });
        }

        /// <summary>
        /// Accept a friend request with the msg content.
        /// </summary>
        /// <param name="content"></param>
        /// <returns></returns>
        public bool AcceptFriend(string content)
        {
            try
            {
                var xml = new XmlDocument();
                xml.LoadXml(content);
                var v3 = xml.SelectSingleNode("/msg/@encryptusername")?.Value ?? "";
                var v4 = xml.SelectSingleNode("/msg/@ticket")?.Value ?? "";
                var scene = int.Parse(xml.SelectSingleNode("/msg/@scene")?.Value ?? "0");
                return RPCAcceptFriend(v3, v4, scene);
            }
            catch (Exception ex)
            {
                _logger?.Error("AcceptFriend failed: {0}", ex);
                return false;
            }
        }

        /// <summary>
        /// Accept a friend request with the msg content.
        /// </summary>
        /// <param name="content"></param>
        /// <param name="cts"></param>
        /// <returns></returns>
        public async Task<bool> AcceptFriendAsync(string content, CancellationTokenSource? cts = null)
        {
            try
            {
                var xml = new XmlDocument();
                xml.LoadXml(content);
                var v3 = xml.SelectSingleNode("/msg/@encryptusername")?.Value ?? "";
                var v4 = xml.SelectSingleNode("/msg/@ticket")?.Value ?? "";
                var scene = int.Parse(xml.SelectSingleNode("/msg/@scene")?.Value ?? "0");
                return await RPCAcceptFriendAsync(v3, v4, scene, cts);
            }
            catch (Exception ex)
            {
                _logger?.Error("AcceptFriend failed: {0}", ex);
                return false;
            }
        }
    }
}

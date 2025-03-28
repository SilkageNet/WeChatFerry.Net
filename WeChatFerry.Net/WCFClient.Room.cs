namespace WeChatFerry.Net
{
	public partial class WCFClient
	{
		public bool RPCAddRoomMembers(string roomID, List<string> wxids)
		{
			try
			{
				var req = new Request { Func = Functions.FuncAddRoomMembers, M = new MemberMgmt { Roomid = roomID, Wxids = string.Join(",", wxids) } };
				var res = CallRPC(req);
				var ok = res.Status == 0;
				if (!ok) _logger?.Warn("AddRoomMembers failed, status: {0}", res.Status);
				return ok;
			}
			catch (Exception ex)
			{
				_logger?.Error("AddRoomMembers failed: {0}", ex);
				return false;
			}
		}

		public bool RPCDelRoomMembers(string roomID, List<string> wxids)
		{
			try
			{
				var req = new Request { Func = Functions.FuncDelRoomMembers, M = new MemberMgmt { Roomid = roomID, Wxids = string.Join(",", wxids) } };
				var res = CallRPC(req);
				var ok = res.Status == 0;
				if (!ok) _logger?.Warn("DelRoomMembers failed, status: {0}", res.Status);
				return ok;
			}
			catch (Exception ex)
			{
				_logger?.Error("DelRoomMembers failed: {0}", ex);
				return false;
			}
		}

		public bool RPCInvRoomMembers(string roomID, List<string> wxids)
		{
			try
			{
				var req = new Request { Func = Functions.FuncInvRoomMembers, M = new MemberMgmt { Roomid = roomID, Wxids = string.Join(",", wxids) } };
				var res = CallRPC(req);
				var ok = res.Status == 0;
				if (!ok) _logger?.Warn("FuncInvRoomMembers failed, status: {0}", res.Status);
				return ok;
			}
			catch (Exception ex)
			{
				_logger?.Error("InvRoomMembers failed: {0}", ex);
				return false;
			}
		}
	}
}

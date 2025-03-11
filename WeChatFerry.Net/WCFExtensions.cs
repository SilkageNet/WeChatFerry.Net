namespace WeChatFerry.Net
{
    public partial class UserInfo
    {
        public RpcContact ToContact() => new()
        {
            Wxid = Wxid,
            Name = Name,
        };
    }
}

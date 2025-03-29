
namespace WeChatFerry.Net
{
    public enum ContactGender : int
    {
        Unknown = 0,
        Male = 1,
        Female = 2
    }

    public enum ContactType
    {
        Unknown = 0,
        Friend = 1,
        Group = 2,
        Public = 3,
        OpenIM = 4,
        Special = 5,
    }

    public class Contact(RpcContact contact)
    {
        private static readonly string[] SpecialContacts =
        [
            "fmessage", "newsapp", "weibo",
            "qqmail", "tmessage", "qmessage",
            "qqsync", "floatbottle", "lbsapp",
            "shakeapp", "medianote", "newsassistant",
            "voicevoip", "blogapp", "masssend",
            "weixin", "filehelper", "official",
            "notification_messages", "verifymsg", "voip",
            "webwx", "notification_messages", "mphelper"
        ];

        public string Wxid => contact.Wxid;

        public string Code => contact.Code;

        public string Remark => contact.Remark;

        public string Name => contact.Name;

        public string Country => contact.Country;

        public string Province => contact.Province;

        public string City => contact.City;

        public ContactGender Gender => (ContactGender)contact.Gender;

        private ContactType? _contactType;
        public ContactType ContactType
        {
            get
            {
                if (_contactType != null) return _contactType.Value;

                if (contact.Wxid.EndsWith("@chatroom")) _contactType = ContactType.Group;
                else if (contact.Wxid.EndsWith("@openim")) _contactType = ContactType.OpenIM;
                else if (contact.Wxid.StartsWith("gh_")) _contactType = ContactType.Public;
                else if (SpecialContacts.Contains(contact.Wxid)) _contactType = ContactType.Special;
                else _contactType = ContactType.Friend;

                return _contactType.Value;
            }
        }

        public void Update(RpcContact rpcContact) => contact = rpcContact;
    }
}

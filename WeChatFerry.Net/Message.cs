namespace WeChatFerry.Net
{
    public enum MessageType : uint
    {
        Pyq = 0,
        Text = 1,
        Image = 3,
        Voice = 34,
        ContactConfirm = 37,
        PossibleFriend = 40,
        BusinessCard = 42,
        Video = 43,
        Emoticon = 47,
        Location = 48,
        Link = 49,
        Voip = 50,
        WeChatInit = 51,
        VoipNotify = 52,
        VoipInvite = 53,
        MiniVideo = 62,
        WeChatRedPacket = 66,
        File = 495,
        Music = 496,
        Article = 259,
        GroupNote = 101,
        SyncNotice = 9999,
        System = 10000,
        Recall = 10002,
        SogouEmoticon = 1048625,
        Links = 16777265,
        RedPacket = 436207665,
        RedPacketFace = 536936497,
        VideoAccountVideo = 754974769,
        VideoAccountBusinessCard = 771751985,
        RefrenceMessage = 822083633,
        Pat = 922746929,
        VideoAccountLive = 973078577,
        GoodsLink = 974127153,
        VideoAccountLive2 = 975175729,
        MusicLink = 1040187441,
        File2 = 1090519089
    }

    public class Message(WxMsg raw)
    {
        public WxMsg Raw { get; } = raw;

        public bool IsSelf => Raw.IsSelf;

        public bool IsGroup => Raw.IsGroup;

        public ulong ID => Raw.Id;

        public MessageType Type => (MessageType)Raw.Type;

        public DateTime Time => DateTimeOffset.FromUnixTimeSeconds(Raw.Ts).DateTime;

        public string RoomID => Raw.Roomid;

        public string Content => Raw.Content;

        public string Sender => Raw.Sender;

        public string Sign => Raw.Sign;

        public string Thumb => Raw.Thumb;

        public string Extra => Raw.Extra;

        public string Xml => Raw.Xml;

        public override string ToString() => $"[{Type}] {Sender}: {Content}";
    }
}

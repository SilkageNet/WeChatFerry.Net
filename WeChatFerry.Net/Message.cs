namespace WeChatFerry.Net
{
    public class Message(Message.MessageType type, string receiver)
    {
        public enum MessageType
        {
            Txt, Img, File, Emotion, RichTxt, PatMsg, Forward
        }

        public MessageType Type { get; set; } = type;
        public string Receiver { get; set; } = receiver;
        public string? Content { get; set; }
        public List<string>? Aters { get; set; }
        public RichText? RichTxt { get; set; }
        public string? PatWxid { get; set; }
        public ulong? ForwardMsgID { get; set; }

        public bool SendWithClient(RPCCaller wcfClient)
        {
            switch (Type)
            {
                case MessageType.Txt:
                    return wcfClient.SendTxt(Receiver, Content!, Aters);
                case MessageType.Img:
                    return wcfClient.SendImg(Receiver, Content!);
                case MessageType.File:
                    return wcfClient.SendFile(Receiver, Content!);
                case MessageType.Emotion:
                    return wcfClient.SendEmotion(Receiver, Content!);
                case MessageType.RichTxt:
                    RichTxt!.Receiver = Receiver;
                    return wcfClient.SendRichTxt(RichTxt!);
                case MessageType.PatMsg:
                    return wcfClient.SendPatMsg(Receiver, PatWxid!);
                case MessageType.Forward:
                    return wcfClient.ForwardMsg(Receiver, ForwardMsgID!.Value);
                default:
                    return false;
            }
        }

        public static Message CreateTxt(string receiver, string content, List<string>? aters = null) => new(MessageType.Txt, receiver)
        {
            Content = content,
            Aters = aters
        };

        public static Message CreateImg(string receiver, string content) => new(MessageType.Img, receiver)
        {
            Content = content
        };

        public static Message CreateFile(string receiver, string content) => new(MessageType.File, receiver)
        {
            Content = content
        };

        public static Message CreateEmotion(string receiver, string content) => new(MessageType.Emotion, receiver)
        {
            Content = content
        };

        public static Message CreateRichText(string receiver, RichText richText) => new(MessageType.RichTxt, receiver)
        {
            RichTxt = richText
        };

        public static Message CreatePatMsg(string receiver, string patWxid) => new(MessageType.PatMsg, receiver)
        {
            PatWxid = patWxid
        };

        public static Message CreateForward(string receiver, ulong forwardMsgID) => new(MessageType.Forward, receiver)
        {
            ForwardMsgID = forwardMsgID
        };
    }
}

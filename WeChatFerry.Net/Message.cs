namespace WeChatFerry.Net
{
    public class Message
    {
        public enum MessageType
        {
            Txt, Img, File, Emotion, RichTxt, PatMsg, Forward
        }

        public required MessageType Type { get; init; }
        public required string Receiver { get; init; }
        public string? Content { get; init; }
        public List<string>? Aters { get; init; }
        public RichText? RichTxt { get; init; }
        public string? PatWxid { get; init; }
        public ulong? ForwardMsgID { get; init; }

        public static Message CreateTxt(string receiver, string content, List<string>? aters = null) => new()
        {
            Type = MessageType.Txt,
            Receiver = receiver,
            Content = content,
            Aters = aters
        };

        public static Message CreateImg(string receiver, string content) => new()
        {
            Type = MessageType.Img,
            Receiver = receiver,
            Content = content
        };

        public static Message CreateFile(string receiver, string content) => new()
        {
            Type = MessageType.File,
            Receiver = receiver,
            Content = content
        };

        public static Message CreateEmotion(string receiver, string content) => new()
        {
            Type = MessageType.Emotion,
            Receiver = receiver,
            Content = content
        };

        public static Message CreateRichText(string receiver, RichText richText) => new()
        {
            Type = MessageType.RichTxt,
            Receiver = receiver,
            RichTxt = richText
        };

        public static Message CreatePatMsg(string receiver, string patWxid) => new()
        {
            Type = MessageType.PatMsg,
            Receiver = receiver,
            PatWxid = patWxid
        };

        public static Message CreateForward(string receiver, ulong forwardMsgID) => new()
        {
            Type = MessageType.Forward,
            Receiver = receiver,
            ForwardMsgID = forwardMsgID
        };

        public bool SendWithClient(WCFClient wcfClient)
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
    }
}

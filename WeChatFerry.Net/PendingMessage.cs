namespace WeChatFerry.Net
{
    public class PendingMessage(PendingMessage.MessageType type, string receiver)
    {
        public enum MessageType
        {
            Txt, Img, File, Emotion, RichTxt, PatMsg, Forward, Xml
        }

        public MessageType Type { get; set; } = type;
        public string Receiver { get; set; } = receiver;
        public string? Content { get; set; }
        public List<string>? Aters { get; set; }
        public RichText? RichTxt { get; set; }
        public string? PatWxid { get; set; }
        public ulong? ForwardMsgID { get; set; }
        public XmlMsg? Xml { get; set; }

        public static PendingMessage CreateTxt(string receiver, string content, List<string>? aters = null) => new(MessageType.Txt, receiver)
        {
            Content = content,
            Aters = aters
        };

        public static PendingMessage CreateImg(string receiver, string content) => new(MessageType.Img, receiver)
        {
            Content = content
        };

        public static PendingMessage CreateFile(string receiver, string content) => new(MessageType.File, receiver)
        {
            Content = content
        };

        public static PendingMessage CreateEmotion(string receiver, string content) => new(MessageType.Emotion, receiver)
        {
            Content = content
        };

        public static PendingMessage CreateRichText(string receiver, RichText richText)
        {
            richText.Receiver = receiver;
            return new(MessageType.RichTxt, receiver)
            {
                RichTxt = richText
            };
        }

        public static PendingMessage CreatePatMsg(string receiver, string patWxid) => new(MessageType.PatMsg, receiver)
        {
            PatWxid = patWxid
        };

        public static PendingMessage CreateForward(string receiver, ulong forwardMsgID) => new(MessageType.Forward, receiver)
        {
            ForwardMsgID = forwardMsgID
        };

        public static PendingMessage CreateXml(string receiver, string path, string content, ulong type) => new(MessageType.Xml, receiver)
        {
            Xml = new XmlMsg
            {
                Receiver = receiver,
                Path = path,
                Content = content,
                Type = type
            }
        };
    }
}

namespace WeChatFerry.Net
{
    public interface ILogger
    {
        void Debug(string messageFormat, params object[] @params);
        void Info(string messageFormat, params object[] @params);
        void Warn(string messageFormat, params object[] @params);
        void Error(string messageFormat, params object[] @params);
        void Fatal(string messageFormat, params object[] @params);
    }
}

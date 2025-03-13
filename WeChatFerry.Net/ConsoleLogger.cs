namespace WeChatFerry.Net
{
    public class ConsoleLogger : ILogger
    {
        public void Debug(string messageFormat, params object[] @params) => Log("DEBUG", messageFormat, @params);

        public void Error(string messageFormat, params object[] @params) => Log("ERROR", messageFormat, @params);

        public void Fatal(string messageFormat, params object[] @params) => Log("FATAL", messageFormat, @params);

        public void Info(string messageFormat, params object[] @params) => Log("INFO", messageFormat, @params);

        public void Warn(string messageFormat, params object[] @params) => Log("WARN", messageFormat, @params);

        private static void Log(string level, string messageFormat, params object[] @params) => Console.WriteLine($"{level}: {messageFormat}", @params);
    }
}

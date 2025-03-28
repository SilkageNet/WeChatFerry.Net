namespace WeChatFerry.Net
{
    public class WCFClientOptions
    {  /// <summary>
       /// WCF server port, and message push port is port+1.
       /// </summary>
        public int Port { get; set; } = 6666;
        /// <summary>
        /// Find this SDKPath in the executable directory.
        /// </summary>
        public string SDKPath { get; set; } = string.Empty;
        /// <summary>
        /// If the Debug is true, must add spy_debug.dll in the SDKPath's directory.
        /// </summary>
        public bool Debug { get; set; }
        /// <summary>
        /// Disable WeChat upgrade.
        /// </summary>
        public bool DisableWeChatUpgrade { get; set; } = true;
        /// <summary>
        /// Message interval (milliseconds).
        /// </summary>
        public int MessageInterval { get; set; } = 2000;
        /// <summary>
        /// Message limit in minutes.
        /// </summary>
        public int MessageLimitInMinutes { get; set; } = 6;
        /// <summary>
        /// Logger.
        /// </summary>
        public ILogger? Logger { get; set; }

        private readonly Random _random = new();
        /// <summary>
        /// Random message interval (milliseconds).
        /// </summary>
        public int MessageIntervalRandom => _random.Next(MessageInterval / 2, MessageInterval * 2);
    }
}

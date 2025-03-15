using nng;
using System.Collections.Concurrent;
using System.Reflection;

namespace WeChatFerry.Net
{
    public class WCFClient : IDisposable
    {
        public class Options
        {
            /// <summary>
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

        protected readonly IAPIFactory<INngMsg> _factory = InitNngAPIFactory();
        protected readonly ConcurrentDictionary<string, RpcContact> _contacts = new();
        protected readonly ConcurrentQueue<Message> _msgQueue = new();
        protected readonly List<DateTime> _lastSendTimes = [];
        protected readonly Options _options;
        protected readonly ILogger? _logger;
        protected readonly SDK _sdk;

        protected RPCCaller? _caller;
        protected CancellationTokenSource? _cts;
        protected Task? _recvTask;
        protected Task? _sendTask;
        protected bool _started;

        public WCFClient(Options? options = null)
        {
            _options = options ?? new Options();
            _logger = _options.Logger ?? new ConsoleLogger();
            _sdk = new(_options.SDKPath);
        }

        /// <summary>
        /// The event will be triggered when receiving a message.
        /// </summary>
        public event EventHandler<WxMsg>? OnRecvMsg;

        /// <summary>
        /// WCFClient instance.
        /// </summary>
        public RPCCaller? RPCCaller => _caller;

        /// <summary>
        /// Start the robot.
        /// </summary>
        /// <param name="timeout"></param>
        /// <returns></returns>
        public async Task<bool> Start(int timeout = 60)
        {
            if (_started) return false;
            _cts = new CancellationTokenSource();
            // 0. init SDK
            var ret = _sdk.WxInit(_options.Debug, _options.Port);
            if (ret != 0)
            {
                _logger?.Error("Init SDK failed: {0}", ret);
                return false;
            }
            // 1. connect to WCF cmd server
            var cmdSocket = _factory.PairOpen().ThenDial($"tcp://127.0.0.1:{_options.Port}").Unwrap();
            _caller = new RPCCaller(cmdSocket);
            // 2. wait for logined
            var logined = await _caller.WaitForLogin(timeout);
            if (!logined)
            {
                _logger?.Error("Login failed");
                return false;
            }
            // 3. get all contacts
            var contacts = _caller.GetContacts();
            if (contacts == null)
            {
                _logger?.Error("Get contacts failed");
                return false;
            }
            _contacts.Clear();
            foreach (var contact in contacts) _contacts.TryAdd(contact.Wxid, contact);
            var selfUser = _caller.GetUserInfo();
            if (selfUser == null)
            {
                _logger?.Error("Get self user info failed");
                return false;
            }
            _contacts.TryAdd(selfUser.Wxid, selfUser.ToContact());
            // 4. enable receive message
            var ok = _caller.EnableRecvTxt();
            if (!ok) _logger?.Warn("Enable receive message failed"); // only warn, because it may be enabled
            // 5. connect to WCF message push server
            var msgSocket = _factory.PairOpen().ThenDial($"tcp://127.0.0.1:{_options.Port + 1}").Unwrap();
            // 6.[task 1]  loop receive message
            _recvTask = Task.Run(() => LoopReceive(msgSocket, _cts.Token), _cts.Token);
            // 7.[task 2] loop send message to control message frequency
            _sendTask = LoopSend(_cts.Token);
            _started = true;
            return true;
        }

        /// <summary>
        /// Stop the robot.
        /// </summary>
        public void Stop()
        {
            if (!_started) return;
            _started = false;
            _cts?.Cancel();
            //_sdk.WxDestroy(); // No destroy SDK, because it will cause the WeChat process not clean up.
            _recvTask?.Wait();
            _sendTask?.Wait();
        }

        /// <summary>
        /// Send a message, the message will be sent in the next loop.
        /// </summary>
        /// <param name="msg"></param>
        public void SendMsg(Message msg)
        {
            if (!_started) return;
            _msgQueue.Enqueue(msg);
        }

        /// <summary>
        /// Get a contact by wxid.
        /// </summary>
        /// <param name="wxid"></param>
        /// <returns></returns>
        public RpcContact? GetContact(string wxid)
        {
            if (!_contacts.TryGetValue(wxid, out var contact)) return null;
            return contact;
        }

        /// <summary>
        /// Loop receive message.
        /// </summary>
        /// <param name="msgSocket"></param>
        /// <param name="ct"></param>
        protected void LoopReceive(IPairSocket msgSocket, CancellationToken ct)
        {
            try
            {
                while (!ct.IsCancellationRequested)
                {
                    var msg = msgSocket.RecvMsg().Unwrap();
                    var data = msg.AsSpan().ToArray();
                    var res = Response.Parser.ParseFrom(data);
                    OnRecvMsg?.Invoke(this, res.Wxmsg);
                    _logger?.Debug("RecvMsg: {0}", res.Wxmsg);
                }
            }
            catch (Exception ex)
            {
                _logger?.Error("LoopRecvMsg failed: {0}", ex);
            }
        }

        /// <summary>
        /// Loop send message.
        /// </summary>
        /// <param name="ct"></param>
        /// <returns></returns>
        protected async Task LoopSend(CancellationToken ct)
        {
            try
            {
                if (_caller == null)
                {
                    _logger?.Warn("WCFClient is null");
                    return;
                }

                while (!ct.IsCancellationRequested)
                {
                    if (_msgQueue.TryDequeue(out var msg))
                    {
                        var messageLimitInMinutes = _options.MessageLimitInMinutes;
                        var messageIntervalRandom = _options.MessageIntervalRandom;
                        var messageInterval = _options.MessageInterval;
                        // if the number of messages sent in the last minute exceeds the limit, wait
                        while (true)
                        {
                            // remove the message sent more than 1 minute ago
                            _lastSendTimes.RemoveAll(t => DateTime.Now.Subtract(t).TotalMinutes > 1);
                            if (_lastSendTimes.Count < messageLimitInMinutes) break;
                            await Task.Delay(messageIntervalRandom, ct);
                        }
                        // if the message interval is less than the set value, wait
                        var lastSendTime = _lastSendTimes.LastOrDefault();
                        if (lastSendTime != default && DateTime.Now.Subtract(lastSendTime).TotalMilliseconds < messageInterval)
                        {
                            await Task.Delay(messageIntervalRandom, ct);
                        }
                        var ok = msg.SendWithClient(_caller);
                        if (!ok)
                        {
                            _logger?.Warn("Send message failed: {0}", msg);
                            continue;
                        }

                        _lastSendTimes.Add(DateTime.Now);
                        _logger?.Debug("SendMsg: {0}", msg);
                    }
                    await Task.Delay(_options.MessageIntervalRandom, ct);
                }
            }
            catch (Exception ex)
            {
                _logger?.Error("LoopSendMsg failed: {0}", ex);
            }
        }

        /// <summary>
        /// Initialize Nng API Factory.
        /// </summary>
        /// <returns></returns>
        private static IAPIFactory<INngMsg> InitNngAPIFactory()
        {
            var assembly = Assembly.GetExecutingAssembly();
            var assemblyPath = Path.GetDirectoryName(assembly.Location);
            var nngLoadContext = new NngLoadContext(assemblyPath);
            return NngLoadContext.Init(nngLoadContext, "nng.Factories.Latest.Factory");
        }

        public void Dispose()
        {
            Stop();
            GC.SuppressFinalize(this);
        }
    }
}

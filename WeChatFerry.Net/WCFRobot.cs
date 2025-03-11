using NLog;
using nng;
using System.Collections.Concurrent;
using System.Reflection;
using System.Text.Json;

namespace WeChatFerry.Net
{
    public class WCFRobot
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
            public string SDKPath { get; set; } = "sdk.dll";
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

            private readonly Random _random = new();
            /// <summary>
            /// Random message interval (milliseconds).
            /// </summary>
            public int MessageIntervalRandom => _random.Next(MessageInterval / 2, MessageInterval * 2);
        }

        protected readonly Logger _logger = LogManager.GetCurrentClassLogger();
        protected readonly IAPIFactory<INngMsg> _factory = InitNngAPIFactory();
        protected readonly ConcurrentDictionary<string, RpcContact> _contacts = new();
        protected readonly ConcurrentQueue<Message> _msgQueue = new();
        protected readonly List<DateTime> _lastSendTimes = [];
        protected readonly Options _options;
        protected readonly WCFSDK _wcfSDK;

        protected WCFClient? _wcfClient;
        protected CancellationTokenSource? _cts;
        protected Task? _recvTask;
        protected Task? _sendTask;
        protected bool _started;

        public WCFRobot(Options? options = null)
        {
            _options = options ?? new Options();
            _wcfSDK = new(_options.SDKPath);
        }

        /// <summary>
        /// The event will be triggered when receiving a message.
        /// </summary>
        public event EventHandler<WxMsg>? OnRecvMsg;

        /// <summary>
        /// Start the robot.
        /// </summary>
        /// <param name="cts"></param>
        /// <param name="timeout"></param>
        /// <returns></returns>
        public async Task<bool> Start(CancellationTokenSource? cts = null, int timeout = 60)
        {
            if (_started) return false;
            _cts = cts ?? new CancellationTokenSource();
            // 0. init SDK
            var ret = _wcfSDK.WxInitSDK(_options.Debug, _options.Port);
            if (ret != 0)
            {
                _logger.Error("Init SDK failed: {0}", ret);
                return false;
            }
            // 1. connect to WCF cmd server
            var cmdSocket = _factory.PairOpen().ThenDial($"tcp://127.0.0.1:{_options.Port}").Unwrap();
            _wcfClient = new WCFClient(cmdSocket);
            // 2. wait for logined
            var logined = await _wcfClient.WaitForLogin(timeout);
            if (!logined)
            {
                _logger.Error("Login failed");
                return false;
            }
            // 3. get all contacts
            var contacts = _wcfClient.GetContacts();
            if (contacts == null)
            {
                _logger.Error("Get contacts failed");
                return false;
            }
            _contacts.Clear();
            foreach (var contact in contacts) _contacts.TryAdd(contact.Wxid, contact);
            var selfUser = _wcfClient.GetUserInfo();
            if (selfUser == null)
            {
                _logger.Error("Get self user info failed");
                return false;
            }
            _contacts.TryAdd(selfUser.Wxid, selfUser.ToContact());
            // 4. enable receive message
            var ok = _wcfClient.EnableRecvTxt();
            if (!ok) _logger.Warn("Enable receive message failed"); // only warn, because it may be enabled
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
            _wcfSDK.WxDestroySDK();
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
                    _logger.Debug("RecvMsg: {0}", JsonSerializer.Serialize(res.Wxmsg));
                }
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "LoopRecvMsg failed");
            }
        }

        protected async Task LoopSend(CancellationToken ct)
        {
            try
            {
                if (_wcfClient == null)
                {
                    _logger.Warn("WCFClient is null");
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
                        var ok = msg.SendWithClient(_wcfClient);
                        if (!ok)
                        {
                            _logger.Warn("Send message failed: {0}", JsonSerializer.Serialize(msg));
                            continue;
                        }

                        _lastSendTimes.Add(DateTime.Now);
                        _logger.Debug("SendMsg: {0}", JsonSerializer.Serialize(msg));
                    }
                    await Task.Delay(_options.MessageIntervalRandom, ct);
                }
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "LoopSendMsg failed");
            }
        }

        private static IAPIFactory<INngMsg> InitNngAPIFactory()
        {
            var assembly = Assembly.GetExecutingAssembly();
            var assemblyPath = Path.GetDirectoryName(assembly.Location);
            var nngLoadContext = new NngLoadContext(assemblyPath);
            return NngLoadContext.Init(nngLoadContext, "nng.Factories.Latest.Factory");
        }
    }
}

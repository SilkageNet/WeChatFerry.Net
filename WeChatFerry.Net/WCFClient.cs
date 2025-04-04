using Google.Protobuf;
using nng;
using System.Collections.Concurrent;
using System.Reflection;

namespace WeChatFerry.Net
{
    public partial class WCFClient : IDisposable
    {
        protected readonly IAPIFactory<INngMsg> _factory = InitNngAPIFactory();
        protected readonly ConcurrentDictionary<string, Contact> _contacts = new();
        protected readonly ConcurrentQueue<PendingMessage> _msgQueue = new();
        protected readonly List<DateTime> _lastSendTimes = [];
        protected readonly WCFClientOptions _options;
        protected readonly ILogger? _logger;
        protected readonly SDK _sdk;

        protected IPairSocket? _cmdSocket;
        protected CancellationTokenSource? _cts;
        protected Task? _recvTask;
        protected Task? _sendTask;
        protected bool _started;

        public WCFClient(WCFClientOptions? options = null)
        {
            _options = options ?? new WCFClientOptions();
            _logger = _options.Logger ?? new ConsoleLogger();
            _sdk = new(_options.SDKPath);
        }

        /// <summary>
        /// The event will be triggered when receiving a message.
        /// </summary>
        public event EventHandler<Message>? OnRecvMsg;

        /// <summary>
        /// Start the robot.
        /// </summary>
        /// <param name="timeout"></param>
        /// <returns></returns>
        public async Task<bool> Start(int timeout = 60)
        {
            if (_started) return false;
            _cts = new CancellationTokenSource();
            // -1. disable WeChat upgrade
            if (_options.DisableWeChatUpgrade) WeChatRegistry.DisableUpgrade();
            // 0. init SDK
            var ret = _sdk.WxInit(_options.Debug, _options.Port);
            if (ret != 0)
            {
                _logger?.Error("Init SDK failed: {0}", ret);
                return false;
            }
            // 1. connect to WCF cmd server
            _cmdSocket = _factory.PairOpen().ThenDial($"tcp://127.0.0.1:{_options.Port}").Unwrap();
            // 2. wait for logined
            var logined = await WaitForLogin(timeout);
            if (!logined)
            {
                _logger?.Error("Login failed");
                return false;
            }
            // 3. get all contacts
            var contacts = RPCGetContacts();
            if (contacts == null)
            {
                _logger?.Error("Get contacts failed");
                return false;
            }
            _contacts.Clear();
            foreach (var contact in contacts) _contacts.TryAdd(contact.Wxid, new Contact(contact));
            var selfUser = RPCGetUserInfo();
            if (selfUser == null)
            {
                _logger?.Error("Get self user info failed");
                return false;
            }
            _contacts.TryAdd(selfUser.Wxid, new Contact(new RpcContact { Wxid = selfUser.Wxid, Name = selfUser.Name }));
            // 4. enable receive message
            var ok = RPCEnableRecvTxt();
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
                    OnRecvMsg?.Invoke(this, new Message(res.Wxmsg));
                    _logger?.Debug("RecvMsg: {0}", res.Wxmsg);
                }
            }
            catch (TaskCanceledException)
            {
                _logger?.Info("LoopRecvMsg canceled");
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
                if (_cmdSocket == null)
                {
                    _logger?.Warn("Cmd socket is null");
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
                        var ok = SendMessage(msg);
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
            catch (TaskCanceledException)
            {
                _logger?.Info("LoopSendMsg canceled");
            }
            catch (Exception ex)
            {
                _logger?.Error("LoopSendMsg failed: {0}", ex);
            }
        }

        /// <summary>
        /// Send message
        /// </summary>
        /// <param name="msg"></param>
        /// <returns></returns>
        private bool SendMessage(PendingMessage msg) => msg.Type switch
        {
            PendingMessage.MessageType.Txt => RPCSendTxt(msg.Receiver, msg.Content!, msg.Aters),
            PendingMessage.MessageType.Img => RPCSendImg(msg.Receiver, msg.Content!),
            PendingMessage.MessageType.File => RPCSendFile(msg.Receiver, msg.Content!),
            PendingMessage.MessageType.Emotion => RPCSendEmotion(msg.Receiver, msg.Content!),
            PendingMessage.MessageType.RichTxt => RPCSendRichTxt(msg.RichTxt!),
            PendingMessage.MessageType.PatMsg => RPCSendPatMsg(msg.Receiver, msg.PatWxid!),
            PendingMessage.MessageType.Forward => RPCForwardMsg(msg.Receiver, msg.ForwardMsgID!.Value),
            _ => false,
        };

        /// <summary>
        /// Call RPC.
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public Response CallRPC(Request request)
        {
            if (_cmdSocket == null || !_cmdSocket.IsValid()) throw new Exception("CMD socket is not valid");
            _cmdSocket.Send(request.ToByteArray());
            var msg = _cmdSocket.RecvMsg().Unwrap();
            var data = msg.AsSpan().ToArray();
            return Response.Parser.ParseFrom(data);
        }

        /// <summary>
        /// Dispose the object.
        /// </summary>
        public void Dispose()
        {
            Stop();
            GC.SuppressFinalize(this);
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
    }
}

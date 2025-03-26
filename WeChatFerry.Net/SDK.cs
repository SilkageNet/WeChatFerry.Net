using System.Reflection;
using System.Runtime.InteropServices;

namespace WeChatFerry.Net
{
    public class SDK
    {
        /// <summary>
        /// Inject SDK Delegate
        /// </summary>
        /// <param name="debug">Specify whether to enable debug mode</param>
        /// <param name="port">Specify the port number</param>
        /// <returns>0 if success, otherwise failed</returns>
        public delegate int WxInitDelegate(bool debug, int port);

        /// <summary>
        /// Destroy SDK Delegate
        /// </summary>
        /// <returns>0 if success, otherwise failed</returns>
        public delegate int WxDestroyDelegate();

        /// <summary>
        /// Inject SDK
        /// </summary>
        public WxInitDelegate WxInit;

        /// <summary>
        /// Destroy SDK
        /// </summary>
        public WxDestroyDelegate WxDestroy;

        public SDK(string sdkPath)
        {
            if (string.IsNullOrEmpty(sdkPath)) sdkPath = HuntForSDKPath();

            if (!File.Exists(sdkPath)) throw new Exception("SDK not found or not match WeChat version");

            var ptr = NativeLibrary.Load(sdkPath);
            if (ptr == IntPtr.Zero) throw new Exception("Load SDK failed");
            var initPtr = NativeLibrary.GetExport(ptr, "WxInitSDK");
            var destoryPtr = NativeLibrary.GetExport(ptr, "WxDestroySDK");

            WxInit = Marshal.GetDelegateForFunctionPointer<WxInitDelegate>(initPtr);
            WxDestroy = Marshal.GetDelegateForFunctionPointer<WxDestroyDelegate>(destoryPtr);
        }

        private static string HuntForSDKPath()
        {
            var dir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) ?? string.Empty;
            var wcfDir = Path.Combine(dir, "wcf");
            if (!Directory.Exists(wcfDir)) throw new Exception("WCF directory not found");

            var versions = Directory.GetDirectories(wcfDir)
                .Select(it =>
                {
                    var weChatVersionFile = Path.Combine(it, "WeChatVersion.txt");
                    if (!File.Exists(weChatVersionFile)) return null;
                    return new { Version = new Version(File.ReadAllText(weChatVersionFile)), Path = it };
                })
                .Where(it => it != null)
                .OrderByDescending(it => it!.Version);

            if (!versions.Any()) throw new Exception("WCF version not found");

            var versionDir = versions.FirstOrDefault()!.Path;

            var weChatVersion = WeChatRegistry.Version;
            if (weChatVersion != null)
            {
                var version = versions.FirstOrDefault(it => it!.Version == weChatVersion);
                if (version != null) versionDir = version.Path;
            }

            return Path.Combine(versionDir, "sdk.dll");
        }
    }
}

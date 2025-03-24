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
            if (string.IsNullOrEmpty(sdkPath))
            {
                var dir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) ?? string.Empty;
                var wcfDir = Path.Combine(dir, "wcf");
                if (!Directory.Exists(wcfDir)) throw new Exception("WCF directory not found");
                var versions = Directory.GetDirectories(wcfDir).OrderByDescending(x => new Version(Path.GetFileName(x).TrimStart('v')));
                var weChatVersion = WeChatRegistry.Version;
                var versionDir = (weChatVersion == null ? versions.FirstOrDefault() : versions.FirstOrDefault(x => new Version(Path.GetFileName(x).TrimStart('v')) == weChatVersion)) ?? throw new Exception("WCF version not found");
                sdkPath = Path.Combine(versionDir, "sdk.dll");
            }

            if (!File.Exists(sdkPath)) throw new Exception("SDK not found or not match WeChat version");

            var ptr = NativeLibrary.Load(sdkPath);
            if (ptr == IntPtr.Zero) throw new Exception("Load SDK failed");
            var initPtr = NativeLibrary.GetExport(ptr, "WxInitSDK");
            var destoryPtr = NativeLibrary.GetExport(ptr, "WxDestroySDK");

            WxInit = Marshal.GetDelegateForFunctionPointer<WxInitDelegate>(initPtr);
            WxDestroy = Marshal.GetDelegateForFunctionPointer<WxDestroyDelegate>(destoryPtr);
        }
    }
}

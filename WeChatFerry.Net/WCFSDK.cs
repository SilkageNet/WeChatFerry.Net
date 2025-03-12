using System.Reflection;
using System.Runtime.InteropServices;

namespace WeChatFerry.Net
{
    public class WCFSDK
    {
        /// <summary>
        /// Inject SDK Delegate
        /// </summary>
        /// <param name="debug">Specify whether to enable debug mode</param>
        /// <param name="port">Specify the port number</param>
        /// <returns>0 if success, otherwise failed</returns>
        public delegate int WxInitSDKDelegate(bool debug, int port);

        /// <summary>
        /// Destroy SDK Delegate
        /// </summary>
        /// <returns>0 if success, otherwise failed</returns>
        public delegate int WxDestroySDKDelegate();

        /// <summary>
        /// Inject SDK
        /// </summary>
        public WxInitSDKDelegate WxInitSDK;

        /// <summary>
        /// Destroy SDK
        /// </summary>
        public WxDestroySDKDelegate WxDestroySDK;

        public WCFSDK(string sdkPath)
        {
            if (string.IsNullOrEmpty(sdkPath) || !File.Exists(sdkPath))
            {
                var dir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) ?? string.Empty;
                var wcfDir = Path.Combine(dir, "wcf");
                if (!Directory.Exists(wcfDir)) throw new Exception("WCF directory not found");
                var maxVersionDir = Directory.GetDirectories(wcfDir).OrderByDescending(x => new Version(Path.GetFileName(x).TrimStart('v'))).FirstOrDefault();
                if (maxVersionDir == null) throw new Exception("WCF version not found");
                sdkPath = Path.Combine(maxVersionDir, "sdk.dll");
                if (!File.Exists(sdkPath)) throw new Exception("SDK not found");
            }

            var ptr = NativeLibrary.Load(sdkPath);
            if (ptr == nint.Zero) throw new Exception("Load SDK failed");

            var initPtr = NativeLibrary.GetExport(ptr, "WxInitSDK");
            var destoryPtr = NativeLibrary.GetExport(ptr, "WxDestroySDK");

            WxInitSDK = Marshal.GetDelegateForFunctionPointer<WxInitSDKDelegate>(initPtr);
            WxDestroySDK = Marshal.GetDelegateForFunctionPointer<WxDestroySDKDelegate>(destoryPtr);
        }
    }
}

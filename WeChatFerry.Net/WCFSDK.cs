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
            if (!File.Exists(sdkPath)) throw new FileNotFoundException("SDK path not found", sdkPath);

            var ptr = NativeLibrary.Load(sdkPath);
            if (ptr == nint.Zero) throw new Exception("Load SDK failed");

            var initPtr = NativeLibrary.GetExport(ptr, "WxInitSDK");
            var destoryPtr = NativeLibrary.GetExport(ptr, "WxDestroySDK");

            WxInitSDK = Marshal.GetDelegateForFunctionPointer<WxInitSDKDelegate>(initPtr);
            WxDestroySDK = Marshal.GetDelegateForFunctionPointer<WxDestroySDKDelegate>(destoryPtr);
        }
    }
}

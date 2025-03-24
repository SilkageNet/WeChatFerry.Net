using Microsoft.Win32;

namespace WeChatFerry.Net
{
    public static class WeChatRegistry
    {
        private const string WeChatKey = @"Software\Tencent\WeChat";

        public static string InstallPath
        {
            get
            {
                using var key = Registry.CurrentUser.OpenSubKey(WeChatKey);
                return key?.GetValue(nameof(InstallPath)) as string ?? string.Empty;
            }
        }

        public static bool NeedUpdateType
        {
            get
            {
                using var key = Registry.CurrentUser.OpenSubKey(WeChatKey);
                return key?.GetValue(nameof(NeedUpdateType)) as int? == 1;
            }
            set
            {
                using var key = Registry.CurrentUser.OpenSubKey(WeChatKey, true);
                key?.SetValue(nameof(NeedUpdateType), value ? 1 : 0, RegistryValueKind.DWord);
            }
        }

        public static Version? Version
        {
            get
            {
                using var key = Registry.CurrentUser.OpenSubKey(WeChatKey);
                return ParseVersion(key?.GetValue(nameof(Version)));
            }
        }

        public static Version? ParseVersion(object? value)
        {
            var v = value as int? ?? 0;
            if (v == 0) return null;
            v &= 0x0FFFFFFF;
            byte major = (byte)((v >> 24) & 0xFF);
            byte minor = (byte)((v >> 16) & 0xFF);
            byte patch = (byte)((v >> 8) & 0xFF);
            byte build = (byte)(v & 0xFF);
            return new Version(major, minor, patch, build);
        }
    }
}

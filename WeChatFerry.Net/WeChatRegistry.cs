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
                return key?.GetValue(nameof(NeedUpdateType)) as int? != 0;
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

        public static void DisableUpgrade()
        {
            try
            {
                // Modify registry
                NeedUpdateType = false;
                // Modify configEx.ini
                var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
                var wechatPath = Path.Combine(appDataPath, "Tencent", "WeChat", "All Users", "config", "configEx.ini");
                if (File.Exists(wechatPath))
                {
                    var lines = File.ReadAllLines(wechatPath);
                    for (int i = 0; i < lines.Length; i++)
                    {
                        if (lines[i].Contains("NeedUpdateType") && lines[i].Trim() != "NeedUpdateType=0")
                        {
                            lines[i] = "NeedUpdateType=0";
                            File.WriteAllLines(wechatPath, lines);
                        }
                    }
                }
                // Delete patch directory
                var programData = Environment.SpecialFolder.CommonApplicationData;
                var patchDir = Path.Combine(Environment.GetFolderPath(programData), "Tencent", "WeChat", "patch");
                if (Directory.Exists(patchDir))
                {
                    foreach (var dir in Directory.GetDirectories(patchDir)) Directory.Delete(dir, true);
                }
            }
            catch { }
        }
    }
}

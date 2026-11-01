using System;
using System.Net;
using System.Threading.Tasks;

namespace WASender
{
    /// <summary>
    /// Cek update OTA dari GitHub Releases. Non-blocking, gagal diam.
    /// Pakai: await AppUpdater.CheckAsync("linducip2208/wasendervb", currentVersion);
    /// </summary>
    public static class AppUpdater
    {
        public static string LastLatest { get; private set; } = "";
        public static System.DateTime LastCheck { get; private set; } = System.DateTime.MinValue;

        public static string StatusText()
        {
            try
            {
                if (LastCheck == System.DateTime.MinValue) return "Belum dicek.";
                if (!string.IsNullOrEmpty(LastLatest)) return "Update tersedia: v" + LastLatest;
                return "Sudah versi terbaru (cek " + LastCheck.ToString("HH:mm") + ").";
            }
            catch (Exception) { return ""; }
        }

        public static async Task<string> CheckAsync(string repo, string current)
        {
            try
            {
                using (var wc = new WebClient())
                {
                    wc.Headers.Add("User-Agent", "WASender");
                    string url = "https://api.github.com/repos/" + repo + "/releases/latest";
                    string json = await wc.DownloadStringTaskAsync(url);
                    var obj = Newtonsoft.Json.Linq.JObject.Parse(json);
                    string tag = (obj["tag_name"] ?? "").ToString().Trim().TrimStart('v', 'V');
                    LastCheck = System.DateTime.Now;
                    if (string.IsNullOrEmpty(tag)) { LastLatest = ""; return ""; }
                    if (string.IsNullOrEmpty(current)) { LastLatest = tag; return tag; }
                    if (!tag.Equals(current.Trim().TrimStart('v', 'V'), StringComparison.OrdinalIgnoreCase))
                    { LastLatest = tag; return tag; }
                    LastLatest = "";
                    return "";
                }
            }
            catch (Exception) { return ""; }
        }

        public static string CurrentVersion()
        {
            try { return System.Windows.Forms.Application.ProductVersion; }
            catch (Exception) { return ""; }
        }
    }
}

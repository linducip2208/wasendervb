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
                    if (string.IsNullOrEmpty(tag)) return "";
                    if (string.IsNullOrEmpty(current)) return tag;
                    if (!tag.Equals(current.Trim().TrimStart('v', 'V'), StringComparison.OrdinalIgnoreCase))
                        return tag;
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

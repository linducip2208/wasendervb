using System;
using System.IO;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace WASender
{
    public class NotifyCfg
    {
        public bool Enabled { get; set; }
        public string BotToken { get; set; }
        public string ChatId { get; set; }
        public bool OnDisconnect { get; set; } = true;
        public bool OnLowScore { get; set; } = true;
        public bool OnDailySummary { get; set; } = false;
    }

    public static class Notify
    {
        private static System.Collections.Generic.Dictionary<string, DateTime> _last = new System.Collections.Generic.Dictionary<string, DateTime>();

        public static string GetPath()
        {
            try { return Config.MigrateToApp("notify.json"); }
            catch (Exception) { return "notify.json"; }
        }

        public static NotifyCfg Load()
        {
            try
            {
                string p = GetPath();
                if (!File.Exists(p))
                {
                    var t = new NotifyCfg();
                    try { File.WriteAllText(p, JsonConvert.SerializeObject(t, Formatting.Indented)); } catch (Exception) { }
                    return t;
                }
                var c = JsonConvert.DeserializeObject<NotifyCfg>(File.ReadAllText(p));
                return c ?? new NotifyCfg();
            }
            catch (Exception) { return new NotifyCfg(); }
        }

        public static void Save(NotifyCfg c)
        {
            try
            {
                Config.BackupFile(GetPath());
                File.WriteAllText(GetPath(), JsonConvert.SerializeObject(c, Formatting.Indented));
            }
            catch (Exception) { }
        }

        private static bool Throttle(string key, double hours)
        {
            try
            {
                lock (_last)
                {
                    DateTime t;
                    if (_last.TryGetValue(key, out t) && (DateTime.Now - t).TotalHours < hours) return false;
                    _last[key] = DateTime.Now;
                    return true;
                }
            }
            catch (Exception) { return true; }
        }

        public static async Task<bool> SendAsync(string text, string throttleKey = "", double throttleHours = 0)
        {
            try
            {
                NotifyCfg c = Load();
                if (c == null || !c.Enabled) return false;
                if (string.IsNullOrWhiteSpace(c.BotToken) || string.IsNullOrWhiteSpace(c.ChatId)) return false;
                if (!string.IsNullOrEmpty(throttleKey) && !Throttle(throttleKey, throttleHours)) return false;
                string url = "https://api.telegram.org/bot" + c.BotToken.Trim() + "/sendMessage";
                var payload = new { chat_id = c.ChatId.Trim(), text = text ?? "" };
                string body = JsonConvert.SerializeObject(payload);
                System.Net.HttpWebRequest req = (System.Net.HttpWebRequest)System.Net.WebRequest.Create(url);
                req.Method = "POST";
                req.ContentType = "application/json";
                req.Timeout = 15000;
                byte[] bytes = System.Text.Encoding.UTF8.GetBytes(body);
                req.ContentLength = bytes.Length;
                using (System.IO.Stream s = await req.GetRequestStreamAsync())
                {
                    await s.WriteAsync(bytes, 0, bytes.Length);
                }
                using (System.Net.WebResponse r = await req.GetResponseAsync()) { }
                return true;
            }
            catch (Exception) { return false; }
        }
    }
}

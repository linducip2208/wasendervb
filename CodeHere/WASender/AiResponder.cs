using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace WASender
{
    /// <summary>
    /// Auto-reply AI dgn API key milik sendiri (BYOK, format OpenAI-compatible).
    /// Cara pakai: isi teks balasan rule dgn "[AI]" (opsional diikuti instruksi khusus).
    /// Contoh: "[AI]" atau "[AI] jawab sebagai CS ramah berbahasa Indonesia".
    /// Konfigurasi: %ProgramData%\WaSender\ai_settings.json (dibuat otomatis sbg template).
    /// Tidak mengubah UI apa pun.
    /// </summary>
    public class AiSettings
    {
        public bool Enabled { get; set; }
        public string BaseUrl { get; set; }
        public string ApiKey { get; set; }
        public string Model { get; set; }
        public string SystemPrompt { get; set; }
        public double Temperature { get; set; }
        public int MaxTokens { get; set; }
        public int TimeoutSeconds { get; set; }
        public bool CacheEnabled { get; set; } = true;
        public int MaxCacheEntries { get; set; } = 1000;
        public string FallbackText { get; set; }
    }

    public static class AiResponder
    {
        public const string AiMarker = "[AI]";

        public static bool IsAiMessage(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return false;
            return text.TrimStart().StartsWith(AiMarker, StringComparison.OrdinalIgnoreCase);
        }

        public static string GetExtraInstruction(string text)
        {
            try
            {
                string t = (text ?? "").Trim();
                if (!t.StartsWith(AiMarker, StringComparison.OrdinalIgnoreCase)) return "";
                return t.Substring(AiMarker.Length).Trim();
            }
            catch (Exception) { return ""; }
        }

        public static string GetSettingsPath()
        {
            try
            {
                string dir = Config.GetSysFolderPath();
                try
                {
                    string w = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), Config.WaSenderFolderName);
                    if (!System.IO.Directory.Exists(w)) System.IO.Directory.CreateDirectory(w);
                    dir = w;
                }
                catch (Exception) { }
                return System.IO.Path.Combine(dir, "ai_settings.json");
            }
            catch (Exception) { return "ai_settings.json"; }
        }

        public static AiSettings LoadSettings()
        {
            try
            {
                string path = GetSettingsPath();
                if (!File.Exists(path))
                {
                    var tpl = new AiSettings
                    {
                        Enabled = false,
                        BaseUrl = "https://api.openai.com/v1",
                        ApiKey = "ISI_DENGAN_API_KEY_ANDA",
                        Model = "gpt-4o-mini",
                        SystemPrompt = "Kamu adalah asisten WhatsApp yang ramah dan ringkas. Jawab dalam bahasa Indonesia kecuali diminta lain. Maksimal 3 kalimat pendek.",
                        Temperature = 0.7,
                        MaxTokens = 300,
                        TimeoutSeconds = 45,
                        CacheEnabled = true,
                        MaxCacheEntries = 1000,
                        FallbackText = "Maaf, saya sedang tidak bisa menjawab. Coba lagi nanti."
                    };
                    try { File.WriteAllText(path, JsonConvert.SerializeObject(tpl, Formatting.Indented)); }
                    catch (Exception) { }
                    return tpl;
                }
                string json = File.ReadAllText(path);
                var s = JsonConvert.DeserializeObject<AiSettings>(json);
                if (s == null) return new AiSettings { Enabled = false };
                if (string.IsNullOrEmpty(s.BaseUrl)) s.BaseUrl = "https://api.openai.com/v1";
                if (string.IsNullOrEmpty(s.Model)) s.Model = "gpt-4o-mini";
                if (s.Temperature < 0 || s.Temperature > 2) s.Temperature = 0.7;
                if (s.MaxTokens <= 0) s.MaxTokens = 300;
                if (s.TimeoutSeconds <= 0) s.TimeoutSeconds = 45;
                if (s.MaxCacheEntries <= 0) s.MaxCacheEntries = 1000;
                return s;
            }
            catch (Exception) { return new AiSettings { Enabled = false }; }
        }

        public static bool IsConfigured()
        {
            try
            {
                AiSettings cfg = LoadSettings();
                return cfg != null && cfg.Enabled
                    && !string.IsNullOrWhiteSpace(cfg.ApiKey)
                    && cfg.ApiKey != "ISI_DENGAN_API_KEY_ANDA";
            }
            catch (Exception) { return false; }
        }

        private static readonly object _histLock = new object();
        private static readonly Dictionary<string, List<KeyValuePair<string, string>>> _history =
            new Dictionary<string, List<KeyValuePair<string, string>>>();

        private static List<object> BuildMessages(string systemPrompt, string chatId, string incomingText)
        {
            var msgs = new List<object>();
            msgs.Add(new { role = "system", content = systemPrompt });
            try
            {
                if (!string.IsNullOrEmpty(chatId))
                {
                    lock (_histLock)
                    {
                        List<KeyValuePair<string, string>> h;
                        if (_history.TryGetValue(chatId, out h))
                        {
                            foreach (var kv in h)
                            {
                                string c = kv.Value ?? "";
                                if (c.Length > 800) c = c.Substring(0, 800);
                                msgs.Add(new { role = kv.Key, content = c });
                            }
                        }
                    }
                }
            }
            catch (Exception) { }
            msgs.Add(new { role = "user", content = incomingText });
            return msgs;
        }

        private static void PushHistory(string chatId, string user, string assistant)
        {
            try
            {
                if (string.IsNullOrEmpty(chatId)) return;
                lock (_histLock)
                {
                    List<KeyValuePair<string, string>> h;
                    if (!_history.TryGetValue(chatId, out h)) { h = new List<KeyValuePair<string, string>>(); _history[chatId] = h; }
                    h.Add(new KeyValuePair<string, string>("user", user ?? ""));
                    h.Add(new KeyValuePair<string, string>("assistant", assistant ?? ""));
                    while (h.Count > 6) h.RemoveAt(0);
                    if (_history.Count > 500) _history.Clear();
                }
            }
            catch (Exception) { }
        }

        public static string GetCachePath()
        {
            try { return System.IO.Path.Combine(System.IO.Path.GetDirectoryName(GetSettingsPath()), "ai_cache.json"); }
            catch (Exception) { return "ai_cache.json"; }
        }

        private static readonly object _cacheLock = new object();
        private static Dictionary<string, string> _cache = null;

        private static string NormQ(string s)
        {
            try
            {
                string q = (s ?? "").ToLowerInvariant().Trim();
                q = System.Text.RegularExpressions.Regex.Replace(q, @"\s+", " ");
                q = System.Text.RegularExpressions.Regex.Replace(q, @"[^\p{L}\p{N} ]", "");
                return q.Trim();
            }
            catch (Exception) { return (s ?? "").Trim(); }
        }

        private static Dictionary<string, string> LoadCache(int max)
        {
            try
            {
                lock (_cacheLock)
                {
                    if (_cache != null) return _cache;
                    _cache = new Dictionary<string, string>();
                    try
                    {
                        string path = GetCachePath();
                        if (System.IO.File.Exists(path))
                        {
                            var d = JsonConvert.DeserializeObject<Dictionary<string, string>>(System.IO.File.ReadAllText(path));
                            if (d != null)
                            {
                                foreach (var kv in d)
                                {
                                    if (_cache.Count >= max) break;
                                    if (!string.IsNullOrEmpty(kv.Key) && kv.Value != null) _cache[kv.Key] = kv.Value;
                                }
                            }
                        }
                    }
                    catch (Exception) { }
                    return _cache;
                }
            }
            catch (Exception) { return new Dictionary<string, string>(); }
        }

        private static void SaveCache()
        {
            try
            {
                Dictionary<string, string> snap;
                lock (_cacheLock) { snap = new Dictionary<string, string>(_cache ?? new Dictionary<string, string>()); }
                System.IO.File.WriteAllText(GetCachePath(), JsonConvert.SerializeObject(snap, Formatting.None));
            }
            catch (Exception) { }
        }

        private static string CacheKey(string model, string extra, string incomingText)
        {
            return (model ?? "") + "|" + (extra ?? "") + "|" + NormQ(incomingText);
        }

        private static bool HistoryEmpty(string chatId)
        {
            try
            {
                if (string.IsNullOrEmpty(chatId)) return true;
                lock (_histLock)
                {
                    List<KeyValuePair<string, string>> h;
                    return !_history.TryGetValue(chatId, out h) || h.Count == 0;
                }
            }
            catch (Exception) { return true; }
        }

        public static async Task<string> ReplyAsync(string incomingText, string extraInstruction = null, string chatId = null)
        {
            try
            {
                AiSettings cfg = LoadSettings();
                if (cfg == null || !cfg.Enabled) return null;
                if (string.IsNullOrWhiteSpace(cfg.ApiKey) || cfg.ApiKey == "ISI_DENGAN_API_KEY_ANDA") return cfg.FallbackText;
                if (string.IsNullOrWhiteSpace(incomingText)) return cfg.FallbackText;

                bool useCache = cfg.CacheEnabled && HistoryEmpty(chatId);
                string ckey = useCache ? CacheKey(cfg.Model, extraInstruction, incomingText) : null;
                if (useCache)
                {
                    try
                    {
                        var c = LoadCache(cfg.MaxCacheEntries);
                        string hit;
                        lock (_cacheLock) { c.TryGetValue(ckey, out hit); }
                        if (!string.IsNullOrWhiteSpace(hit))
                        {
                            PushHistory(chatId, incomingText, hit);
                            try { SendEngine.Log("AI cache hit: " + NormQ(incomingText).Substring(0, System.Math.Min(60, NormQ(incomingText).Length))); } catch (Exception) { }
                            return hit;
                        }
                    }
                    catch (Exception) { }
                }

                string systemPrompt = cfg.SystemPrompt ?? "";
                if (!string.IsNullOrWhiteSpace(extraInstruction)) systemPrompt = systemPrompt + "\nInstruksi tambahan: " + extraInstruction;

                var payload = new
                {
                    model = cfg.Model,
                    messages = BuildMessages(systemPrompt, chatId, incomingText).ToArray(),
                    temperature = cfg.Temperature,
                    max_tokens = cfg.MaxTokens
                };
                string body = JsonConvert.SerializeObject(payload);
                string url = cfg.BaseUrl.TrimEnd('/') + "/chat/completions";

                HttpWebRequest req = (HttpWebRequest)WebRequest.Create(url);
                req.Method = "POST";
                req.ContentType = "application/json";
                req.Headers["Authorization"] = "Bearer " + cfg.ApiKey;
                req.Timeout = cfg.TimeoutSeconds * 1000;
                req.ReadWriteTimeout = cfg.TimeoutSeconds * 1000;
                byte[] bytes = Encoding.UTF8.GetBytes(body);
                req.ContentLength = bytes.Length;
                using (Stream rs = await req.GetRequestStreamAsync())
                {
                    await rs.WriteAsync(bytes, 0, bytes.Length);
                }
                string respText;
                using (WebResponse resp = await req.GetResponseAsync())
                using (StreamReader sr = new StreamReader(resp.GetResponseStream(), Encoding.UTF8))
                {
                    respText = await sr.ReadToEndAsync();
                }
                dynamic obj = JsonConvert.DeserializeObject(respText);
                try
                {
                    string content = (string)obj.choices[0].message.content;
                    if (!string.IsNullOrWhiteSpace(content))
                    {
                        string ans = content.Trim();
                        PushHistory(chatId, incomingText, ans);
                        if (useCache)
                        {
                            try
                            {
                                var c = LoadCache(cfg.MaxCacheEntries);
                                lock (_cacheLock)
                                {
                                    if (!c.ContainsKey(ckey))
                                    {
                                        while (c.Count >= cfg.MaxCacheEntries)
                                        {
                                            string first = null;
                                            foreach (var k in c.Keys) { first = k; break; }
                                            if (first == null) break;
                                            c.Remove(first);
                                        }
                                        c[ckey] = ans;
                                    }
                                }
                                SaveCache();
                            }
                            catch (Exception) { }
                        }
                        return ans;
                    }
                }
                catch (Exception) { }
                try
                {
                    string err = (string)obj.error.message;
                    SendEngine.Log("AI error: " + err);
                }
                catch (Exception) { }
                return cfg.FallbackText;
            }
            catch (Exception ex)
            {
                try { SendEngine.Log("AI EX: " + ex.Message); }
                catch (Exception) { }
                try
                {
                    var cfg2 = LoadSettings();
                    return cfg2 != null ? cfg2.FallbackText : null;
                }
                catch (Exception) { return null; }
            }
        }
    }
}

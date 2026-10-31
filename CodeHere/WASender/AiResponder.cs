using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Data.SQLite;
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
        public bool CacheFuzzy { get; set; } = true;
        public System.Collections.Generic.List<AiSegment> Segments { get; set; } = new System.Collections.Generic.List<AiSegment>();
        public int CacheTtlDays { get; set; } = 30;
        public int MaxCacheEntries { get; set; } = 1000;
        public string FallbackText { get; set; }
    }

    public class AiSegment
    {
        public string Name { get; set; }
        public int HourFrom { get; set; }
        public int HourTo { get; set; }
        public string Keyword { get; set; }
        public string Mode { get; set; }
        public string Text { get; set; }
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
                        CacheFuzzy = true,
                        CacheTtlDays = 30,
                        Segments = new System.Collections.Generic.List<AiSegment>(),
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
                if (s.CacheTtlDays < 0) s.CacheTtlDays = 30;
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
                        if (!_history.TryGetValue(chatId, out h) || h.Count == 0)
                        {
                            try { DbLoadHistory(chatId); } catch (Exception) { }
                            _history.TryGetValue(chatId, out h);
                        }
                        if (h != null)
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
                try { DbSaveHistory(chatId); } catch (Exception) { }
            }
            catch (Exception) { }
        }

        private static readonly object _dbLock = new object();
        private static bool _dbReady = false;
        private static bool _ftsOk = false;

        public static string GetCachePath()
        {
            try { return System.IO.Path.Combine(System.IO.Path.GetDirectoryName(GetSettingsPath()), "ai_cache.db"); }
            catch (Exception) { return "ai_cache.db"; }
        }

        private static long NowUnix() { return (long)(System.DateTime.UtcNow - new System.DateTime(1970, 1, 1)).TotalSeconds; }

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

        private static System.Collections.Generic.List<string> Tokens(string norm)
        {
            var out_ = new System.Collections.Generic.List<string>();
            try
            {
                foreach (string w in (norm ?? "").Split(new char[] { ' ' }, System.StringSplitOptions.RemoveEmptyEntries))
                {
                    string x = w.Trim();
                    if (x.Length >= 3 && !out_.Contains(x)) out_.Add(x);
                }
            }
            catch (Exception) { }
            return out_;
        }

        private static void DbInit()
        {
            try
            {
                lock (_dbLock)
                {
                    if (_dbReady) return;
                    string path = GetCachePath();
                    try
                    {
                        string dir = System.IO.Path.GetDirectoryName(path);
                        if (!string.IsNullOrEmpty(dir) && !System.IO.Directory.Exists(dir)) System.IO.Directory.CreateDirectory(dir);
                    }
                    catch (Exception) { }
                    using (var c = new SQLiteConnection("Data Source=" + path + ";Version=3;Journal Mode=WAL;Synchronous=NORMAL;"))
                    {
                        c.Open();
                        using (var cmd = c.CreateCommand())
                        {
                            cmd.CommandText = "CREATE TABLE IF NOT EXISTS ai_cache(key TEXT PRIMARY KEY, question TEXT NOT NULL, norm TEXT NOT NULL, answer TEXT NOT NULL, model TEXT NOT NULL DEFAULT '', extra TEXT NOT NULL DEFAULT '', hits INTEGER NOT NULL DEFAULT 1, created_at INTEGER NOT NULL, last_hit INTEGER NOT NULL);"
                                + "CREATE INDEX IF NOT EXISTS idx_ai_cache_norm ON ai_cache(norm);"
                                + "CREATE TABLE IF NOT EXISTS ai_history(chat_id TEXT NOT NULL, seq INTEGER NOT NULL, role TEXT NOT NULL, content TEXT NOT NULL, PRIMARY KEY(chat_id, seq));"
                                + "CREATE INDEX IF NOT EXISTS idx_ai_history_chat ON ai_history(chat_id, seq);"
                                + "CREATE TABLE IF NOT EXISTS ai_override(nkey TEXT PRIMARY KEY, question TEXT NOT NULL, norm TEXT NOT NULL, answer TEXT NOT NULL, enabled INTEGER NOT NULL DEFAULT 1, updated_at INTEGER NOT NULL);"
                                + "CREATE TABLE IF NOT EXISTS ai_stats(date TEXT NOT NULL, chat_id TEXT NOT NULL, questions INTEGER NOT NULL DEFAULT 0, cache_hits INTEGER NOT NULL DEFAULT 0, ai_calls INTEGER NOT NULL DEFAULT 0, overrides INTEGER NOT NULL DEFAULT 0, PRIMARY KEY(date, chat_id));";
                            cmd.ExecuteNonQuery();
                        }
                        try
                        {
                            using (var cmd = c.CreateCommand())
                            {
                                cmd.CommandText = "CREATE VIRTUAL TABLE IF NOT EXISTS ai_cache_fts USING fts4(ckey, question);";
                                cmd.ExecuteNonQuery();
                            }
                            _ftsOk = true;
                        }
                        catch (Exception) { _ftsOk = false; }
                    }
                    try { ImportJsonCache(); } catch (Exception) { }
                    try { ImportJsonOverrides(); } catch (Exception) { }
                    _dbReady = true;
                }
            }
            catch (Exception) { }
        }

        private static void ImportJsonCache()
        {
            try
            {
                string dir = System.IO.Path.GetDirectoryName(GetCachePath());
                string jp = System.IO.Path.Combine(dir ?? "", "ai_cache.json");
                if (!System.IO.File.Exists(jp)) return;
                Dictionary<string, string> d = null;
                try { d = JsonConvert.DeserializeObject<Dictionary<string, string>>(System.IO.File.ReadAllText(jp)); } catch (Exception) { }
                if (d == null || d.Count == 0) return;
                long now = NowUnix();
                using (var c = new SQLiteConnection("Data Source=" + GetCachePath() + ";Version=3;"))
                {
                    c.Open();
                    using (var tx = c.BeginTransaction())
                    {
                        foreach (var kv in d)
                        {
                            try
                            {
                                if (string.IsNullOrEmpty(kv.Key) || string.IsNullOrWhiteSpace(kv.Value)) continue;
                                string[] parts = kv.Key.Split(new char[] { '|' }, 3);
                                string model = parts.Length > 0 ? parts[0] : "";
                                string extra = parts.Length > 1 ? parts[1] : "";
                                string norm = parts.Length > 2 ? parts[2] : kv.Key;
                                using (var cmd = c.CreateCommand())
                                {
                                    cmd.CommandText = "INSERT OR IGNORE INTO ai_cache(key,question,norm,answer,model,extra,hits,created_at,last_hit) VALUES(@k,@q,@n,@a,@m,@e,1,@t,@t);";
                                    cmd.Parameters.AddWithValue("@k", kv.Key);
                                    cmd.Parameters.AddWithValue("@q", norm);
                                    cmd.Parameters.AddWithValue("@n", norm);
                                    cmd.Parameters.AddWithValue("@a", kv.Value);
                                    cmd.Parameters.AddWithValue("@m", model);
                                    cmd.Parameters.AddWithValue("@e", extra);
                                    cmd.Parameters.AddWithValue("@t", now);
                                    cmd.ExecuteNonQuery();
                                }
                                if (_ftsOk)
                                {
                                    using (var cmd = c.CreateCommand())
                                    {
                                        cmd.CommandText = "INSERT OR IGNORE INTO ai_cache_fts(ckey,question) VALUES(@k,@q);";
                                        cmd.Parameters.AddWithValue("@k", kv.Key);
                                        cmd.Parameters.AddWithValue("@q", norm);
                                        cmd.ExecuteNonQuery();
                                    }
                                }
                            }
                            catch (Exception) { }
                        }
                        tx.Commit();
                    }
                }
                try { System.IO.File.Move(jp, jp + ".bak"); } catch (Exception) { }
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

        private static void ImportJsonOverrides()
        {
            try
            {
                string dir = System.IO.Path.GetDirectoryName(GetCachePath());
                string jp = System.IO.Path.Combine(dir ?? "", "ai_overrides.json");
                if (!System.IO.File.Exists(jp)) return;
                System.Collections.Generic.List<OverrideSeed> seeds = null;
                try { seeds = JsonConvert.DeserializeObject<System.Collections.Generic.List<OverrideSeed>>(System.IO.File.ReadAllText(jp)); } catch (Exception) { }
                if (seeds == null || seeds.Count == 0) return;
                long now = NowUnix();
                int n = 0;
                using (var c = new SQLiteConnection("Data Source=" + GetCachePath() + ";Version=3;"))
                {
                    c.Open();
                    using (var tx = c.BeginTransaction())
                    {
                        foreach (var s in seeds)
                        {
                            try
                            {
                                if (s == null || string.IsNullOrWhiteSpace(s.question) || string.IsNullOrWhiteSpace(s.answer)) continue;
                                string norm = NormQ(s.question);
                                if (norm.Length == 0) continue;
                                using (var cmd = c.CreateCommand())
                                {
                                    cmd.CommandText = "INSERT OR REPLACE INTO ai_override(nkey,question,norm,answer,enabled,updated_at) VALUES(@k,@q,@n,@a,@e,@t);";
                                    cmd.Parameters.AddWithValue("@k", "ovr|" + norm);
                                    cmd.Parameters.AddWithValue("@q", s.question.Trim());
                                    cmd.Parameters.AddWithValue("@n", norm);
                                    cmd.Parameters.AddWithValue("@a", s.answer);
                                    cmd.Parameters.AddWithValue("@e", (s.enabled == false) ? 0 : 1);
                                    cmd.Parameters.AddWithValue("@t", now);
                                    cmd.ExecuteNonQuery();
                                    n++;
                                }
                            }
                            catch (Exception) { }
                        }
                        tx.Commit();
                    }
                }
                try { SendEngine.Log("AI override diimpor: " + n + " dari ai_overrides.json"); } catch (Exception) { }
            }
            catch (Exception) { }
        }

        private class OverrideSeed
        {
            public string question { get; set; }
            public string answer { get; set; }
            public bool? enabled { get; set; }
        }

        private static double Overlap(System.Collections.Generic.List<string> a, System.Collections.Generic.List<string> b)
        {
            try
            {
                if (a == null || b == null || a.Count == 0 || b.Count == 0) return 0;
                int inter = 0;
                foreach (string x in a) if (b.Contains(x)) inter++;
                return (double)inter / (double)System.Math.Max(a.Count, b.Count);
            }
            catch (Exception) { return 0; }
        }

        private static string DbOverrideHit(string norm, System.Collections.Generic.List<string> toks)
        {
            try
            {
                if (string.IsNullOrEmpty(norm)) return null;
                DbInit();
                lock (_dbLock)
                {
                    using (var c = new SQLiteConnection("Data Source=" + GetCachePath() + ";Version=3;"))
                    {
                        c.Open();
                        using (var cmd = c.CreateCommand())
                        {
                            cmd.CommandText = "SELECT answer FROM ai_override WHERE nkey=@k AND enabled=1 LIMIT 1;";
                            cmd.Parameters.AddWithValue("@k", "ovr|" + norm);
                            object o = cmd.ExecuteScalar();
                            if (o != null && !string.IsNullOrWhiteSpace(o.ToString())) return o.ToString();
                        }
                        if (toks == null || toks.Count == 0) return null;
                        string best = null;
                        double bestScore = 0.5;
                        try
                        {
                            var use = toks.Count > 3 ? toks.GetRange(0, 3) : toks;
                            var sb = new System.Text.StringBuilder("SELECT norm,answer FROM ai_override WHERE enabled=1 AND (");
                            for (int i = 0; i < use.Count; i++)
                            {
                                if (i > 0) sb.Append(" OR ");
                                sb.Append("norm LIKE @t").Append(i);
                            }
                            sb.Append(") LIMIT 20;");
                            using (var cmd = c.CreateCommand())
                            {
                                cmd.CommandText = sb.ToString();
                                for (int i = 0; i < use.Count; i++) cmd.Parameters.AddWithValue("@t" + i, "%" + use[i] + "%");
                                using (var r = cmd.ExecuteReader())
                                {
                                    while (r.Read())
                                    {
                                        string onorm = r.IsDBNull(0) ? "" : r.GetString(0);
                                        string oans = r.IsDBNull(1) ? "" : r.GetString(1);
                                        if (string.IsNullOrWhiteSpace(oans)) continue;
                                        double s = Overlap(toks, Tokens(onorm));
                                        if (s > bestScore) { bestScore = s; best = oans; }
                                    }
                                }
                            }
                        }
                        catch (Exception) { }
                        return best;
                    }
                }
            }
            catch (Exception) { return null; }
        }

        private static void DbBump(string chat, int q, int h, int a, int o)
        {
            try
            {
                DbInit();
                string today = System.DateTime.Now.ToString("yyyy-MM-dd");
                lock (_dbLock)
                {
                    using (var c = new SQLiteConnection("Data Source=" + GetCachePath() + ";Version=3;"))
                    {
                        c.Open();
                        using (var cmd = c.CreateCommand())
                        {
                            cmd.CommandText = "INSERT INTO ai_stats(date,chat_id,questions,cache_hits,ai_calls,overrides) VALUES(@d,@c,@q,@h,@a,@o) ON CONFLICT(date,chat_id) DO UPDATE SET questions=questions+excluded.questions, cache_hits=cache_hits+excluded.cache_hits, ai_calls=ai_calls+excluded.ai_calls, overrides=overrides+excluded.overrides;";
                            cmd.Parameters.AddWithValue("@d", today);
                            cmd.Parameters.AddWithValue("@c", string.IsNullOrEmpty(chat) ? "?" : chat);
                            cmd.Parameters.AddWithValue("@q", q);
                            cmd.Parameters.AddWithValue("@h", h);
                            cmd.Parameters.AddWithValue("@a", a);
                            cmd.Parameters.AddWithValue("@o", o);
                            cmd.ExecuteNonQuery();
                        }
                    }
                }
            }
            catch (Exception) { }
        }

        private static void RefreshFaqFile()
        {
            try
            {
                DbInit();
                var sb = new System.Text.StringBuilder();
                sb.AppendLine("# FAQ populer (by hits) - " + System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
                lock (_dbLock)
                {
                    using (var c = new SQLiteConnection("Data Source=" + GetCachePath() + ";Version=3;"))
                    {
                        c.Open();
                        using (var cmd = c.CreateCommand())
                        {
                            cmd.CommandText = "SELECT question,hits,last_hit FROM ai_cache ORDER BY hits DESC LIMIT 20;";
                            using (var r = cmd.ExecuteReader())
                            {
                                int i = 1;
                                while (r.Read())
                                {
                                    string qq = r.IsDBNull(0) ? "" : r.GetString(0);
                                    long hh = r.IsDBNull(1) ? 0 : r.GetInt64(1);
                                    sb.AppendLine((i++) + ". [" + hh + "x] " + qq);
                                }
                            }
                        }
                    }
                }
                string dir = System.IO.Path.GetDirectoryName(GetCachePath());
                System.IO.File.WriteAllText(System.IO.Path.Combine(dir ?? "", "ai_faq.txt"), sb.ToString());
            }
            catch (Exception) { }
        }

        private static string ApplySegments(string ans, string incoming, AiSettings cfg)
        {
            try
            {
                if (string.IsNullOrEmpty(ans) || cfg == null || cfg.Segments == null) return ans;
                int h = System.DateTime.Now.Hour;
                foreach (var s in cfg.Segments)
                {
                    if (s == null || string.IsNullOrEmpty(s.Text)) continue;
                    if (h < s.HourFrom || h >= s.HourTo) continue;
                    if (!string.IsNullOrEmpty(s.Keyword) && (incoming ?? "").ToLowerInvariant().IndexOf(s.Keyword.ToLowerInvariant()) < 0) continue;
                    string mode = (s.Mode ?? "append").ToLowerInvariant();
                    if (mode == "prepend") ans = s.Text + "\n" + ans;
                    else if (mode == "replace") ans = s.Text;
                    else ans = ans + "\n" + s.Text;
                }
            }
            catch (Exception) { }
            return ans;
        }

        private static string DbExactHit(string ckey, int ttlDays)
        {
            try
            {
                DbInit();
                lock (_dbLock)
                {
                    using (var c = new SQLiteConnection("Data Source=" + GetCachePath() + ";Version=3;"))
                    {
                        c.Open();
                        string ans = null;
                        long created = 0;
                        using (var cmd = c.CreateCommand())
                        {
                            cmd.CommandText = "SELECT answer,created_at FROM ai_cache WHERE key=@k LIMIT 1;";
                            cmd.Parameters.AddWithValue("@k", ckey);
                            using (var r = cmd.ExecuteReader())
                            {
                                if (r.Read()) { ans = r.IsDBNull(0) ? null : r.GetString(0); created = r.IsDBNull(1) ? 0 : r.GetInt64(1); }
                            }
                        }
                        if (string.IsNullOrWhiteSpace(ans)) return null;
                        if (ttlDays > 0 && created > 0 && NowUnix() - created > (long)ttlDays * 86400L)
                        {
                            using (var cmd = c.CreateCommand())
                            {
                                cmd.CommandText = "DELETE FROM ai_cache WHERE key=@k; DELETE FROM ai_cache_fts WHERE ckey=@k;";
                                cmd.Parameters.AddWithValue("@k", ckey);
                                cmd.ExecuteNonQuery();
                            }
                            return null;
                        }
                        using (var cmd = c.CreateCommand())
                        {
                            cmd.CommandText = "UPDATE ai_cache SET hits=hits+1,last_hit=@t WHERE key=@k;";
                            cmd.Parameters.AddWithValue("@t", NowUnix());
                            cmd.Parameters.AddWithValue("@k", ckey);
                            cmd.ExecuteNonQuery();
                        }
                        return ans;
                    }
                }
            }
            catch (Exception) { return null; }
        }

        private static string DbFuzzyHit(string model, string extra, string norm, System.Collections.Generic.List<string> toks, int ttlDays)
        {
            try
            {
                if (toks == null || toks.Count == 0) return null;
                DbInit();
                var cands = new System.Collections.Generic.List<string[]>();
                lock (_dbLock)
                {
                    using (var c = new SQLiteConnection("Data Source=" + GetCachePath() + ";Version=3;"))
                    {
                        c.Open();
                        if (_ftsOk)
                        {
                            try
                            {
                                var sb = new System.Text.StringBuilder();
                                foreach (string tk in toks)
                                {
                                    if (sb.Length > 0) sb.Append(" OR ");
                                    sb.Append('"').Append(tk.Replace("\"", "\"\"")).Append('"');
                                }
                                using (var cmd = c.CreateCommand())
                                {
                                    cmd.CommandText = "SELECT ckey,question FROM ai_cache_fts WHERE question MATCH @q LIMIT 10;";
                                    cmd.Parameters.AddWithValue("@q", sb.ToString());
                                    using (var r = cmd.ExecuteReader())
                                    {
                                        while (r.Read()) cands.Add(new string[] { r.GetString(0), r.IsDBNull(1) ? "" : r.GetString(1) });
                                    }
                                }
                            }
                            catch (Exception) { _ftsOk = false; }
                        }
                        if (!_ftsOk && cands.Count == 0)
                        {
                            try
                            {
                                var use = toks.Count > 3 ? toks.GetRange(0, 3) : toks;
                                var sb = new System.Text.StringBuilder("SELECT key,norm FROM ai_cache WHERE ");
                                for (int i = 0; i < use.Count; i++)
                                {
                                    if (i > 0) sb.Append(" OR ");
                                    sb.Append("norm LIKE @t").Append(i);
                                }
                                sb.Append(" LIMIT 20;");
                                using (var cmd = c.CreateCommand())
                                {
                                    cmd.CommandText = sb.ToString();
                                    for (int i = 0; i < use.Count; i++) cmd.Parameters.AddWithValue("@t" + i, "%" + use[i] + "%");
                                    using (var r = cmd.ExecuteReader())
                                    {
                                        while (r.Read()) cands.Add(new string[] { r.GetString(0), r.IsDBNull(1) ? "" : r.GetString(1) });
                                    }
                                }
                            }
                            catch (Exception) { }
                        }
                        string bestKey = null;
                        double bestScore = 0.5;
                        foreach (var cd in cands)
                        {
                            try
                            {
                                var ct = Tokens(NormQ(cd[1]));
                                if (ct.Count == 0) continue;
                                int inter = 0;
                                foreach (string tk in toks) if (ct.Contains(tk)) inter++;
                                double score = (double)inter / (double)System.Math.Max(toks.Count, ct.Count);
                                if (score > bestScore) { bestScore = score; bestKey = cd[0]; }
                            }
                            catch (Exception) { }
                        }
                        if (bestKey == null) return null;
                        string ans = null;
                        long created = 0;
                        using (var cmd = c.CreateCommand())
                        {
                            cmd.CommandText = "SELECT answer,created_at FROM ai_cache WHERE key=@k LIMIT 1;";
                            cmd.Parameters.AddWithValue("@k", bestKey);
                            using (var r = cmd.ExecuteReader())
                            {
                                if (r.Read()) { ans = r.IsDBNull(0) ? null : r.GetString(0); created = r.IsDBNull(1) ? 0 : r.GetInt64(1); }
                            }
                        }
                        if (string.IsNullOrWhiteSpace(ans)) return null;
                        if (ttlDays > 0 && created > 0 && NowUnix() - created > (long)ttlDays * 86400L) return null;
                        using (var cmd = c.CreateCommand())
                        {
                            cmd.CommandText = "UPDATE ai_cache SET hits=hits+1,last_hit=@t WHERE key=@k;";
                            cmd.Parameters.AddWithValue("@t", NowUnix());
                            cmd.Parameters.AddWithValue("@k", bestKey);
                            cmd.ExecuteNonQuery();
                        }
                        return ans;
                    }
                }
            }
            catch (Exception) { return null; }
        }

        private static void DbStore(string ckey, string question, string norm, string ans, string model, string extra, int max)
        {
            try
            {
                DbInit();
                long now = NowUnix();
                lock (_dbLock)
                {
                    using (var c = new SQLiteConnection("Data Source=" + GetCachePath() + ";Version=3;"))
                    {
                        c.Open();
                        using (var tx = c.BeginTransaction())
                        {
                            using (var cmd = c.CreateCommand())
                            {
                                cmd.CommandText = "INSERT OR IGNORE INTO ai_cache(key,question,norm,answer,model,extra,hits,created_at,last_hit) VALUES(@k,@q,@n,@a,@m,@e,1,@t,@t);";
                                cmd.Parameters.AddWithValue("@k", ckey);
                                cmd.Parameters.AddWithValue("@q", question ?? norm ?? "");
                                cmd.Parameters.AddWithValue("@n", norm ?? "");
                                cmd.Parameters.AddWithValue("@a", ans);
                                cmd.Parameters.AddWithValue("@m", model ?? "");
                                cmd.Parameters.AddWithValue("@e", extra ?? "");
                                cmd.Parameters.AddWithValue("@t", now);
                                cmd.ExecuteNonQuery();
                            }
                            if (_ftsOk)
                            {
                                try
                                {
                                    using (var cmd = c.CreateCommand())
                                    {
                                        cmd.CommandText = "INSERT OR IGNORE INTO ai_cache_fts(ckey,question) VALUES(@k,@q);";
                                        cmd.Parameters.AddWithValue("@k", ckey);
                                        cmd.Parameters.AddWithValue("@q", norm ?? "");
                                        cmd.ExecuteNonQuery();
                                    }
                                }
                                catch (Exception) { _ftsOk = false; }
                            }
                            try
                            {
                                int cnt = 0;
                                using (var cmd = c.CreateCommand())
                                {
                                    cmd.CommandText = "SELECT COUNT(*) FROM ai_cache;";
                                    cnt = System.Convert.ToInt32(cmd.ExecuteScalar());
                                }
                                while (cnt >= max && max > 0)
                                {
                                    using (var cmd = c.CreateCommand())
                                    {
                                        cmd.CommandText = "SELECT key FROM ai_cache ORDER BY last_hit ASC LIMIT 1;";
                                        object o = cmd.ExecuteScalar();
                                        if (o == null) break;
                                        string old = o.ToString();
                                        using (var d = c.CreateCommand())
                                        {
                                            d.CommandText = "DELETE FROM ai_cache WHERE key=@k; DELETE FROM ai_cache_fts WHERE ckey=@k;";
                                            d.Parameters.AddWithValue("@k", old);
                                            d.ExecuteNonQuery();
                                        }
                                    }
                                    cnt--;
                                }
                            }
                            catch (Exception) { }
                            tx.Commit();
                        }
                    }
                }
            }
            catch (Exception) { }
        }

        private static void DbSaveHistory(string chatId)
        {
            try
            {
                if (string.IsNullOrEmpty(chatId)) return;
                DbInit();
                System.Collections.Generic.List<KeyValuePair<string, string>> snap;
                lock (_histLock)
                {
                    List<KeyValuePair<string, string>> h;
                    if (!_history.TryGetValue(chatId, out h)) return;
                    snap = new System.Collections.Generic.List<KeyValuePair<string, string>>(h);
                }
                lock (_dbLock)
                {
                    using (var c = new SQLiteConnection("Data Source=" + GetCachePath() + ";Version=3;"))
                    {
                        c.Open();
                        using (var tx = c.BeginTransaction())
                        {
                            using (var cmd = c.CreateCommand())
                            {
                                cmd.CommandText = "DELETE FROM ai_history WHERE chat_id=@c;";
                                cmd.Parameters.AddWithValue("@c", chatId);
                                cmd.ExecuteNonQuery();
                            }
                            int seq = 0;
                            foreach (var kv in snap)
                            {
                                using (var cmd = c.CreateCommand())
                                {
                                    cmd.CommandText = "INSERT INTO ai_history(chat_id,seq,role,content) VALUES(@c,@s,@r,@t);";
                                    cmd.Parameters.AddWithValue("@c", chatId);
                                    cmd.Parameters.AddWithValue("@s", seq++);
                                    cmd.Parameters.AddWithValue("@r", kv.Key);
                                    cmd.Parameters.AddWithValue("@t", kv.Value ?? "");
                                    cmd.ExecuteNonQuery();
                                }
                            }
                            tx.Commit();
                        }
                    }
                }
            }
            catch (Exception) { }
        }

        private static void DbLoadHistory(string chatId)
        {
            try
            {
                if (string.IsNullOrEmpty(chatId)) return;
                DbInit();
                var rows = new System.Collections.Generic.List<KeyValuePair<string, string>>();
                lock (_dbLock)
                {
                    using (var c = new SQLiteConnection("Data Source=" + GetCachePath() + ";Version=3;"))
                    {
                        c.Open();
                        using (var cmd = c.CreateCommand())
                        {
                            cmd.CommandText = "SELECT role,content FROM ai_history WHERE chat_id=@c ORDER BY seq DESC LIMIT 6;";
                            cmd.Parameters.AddWithValue("@c", chatId);
                            using (var r = cmd.ExecuteReader())
                            {
                                while (r.Read()) rows.Add(new KeyValuePair<string, string>(r.IsDBNull(0) ? "user" : r.GetString(0), r.IsDBNull(1) ? "" : r.GetString(1)));
                            }
                        }
                    }
                }
                rows.Reverse();
                if (rows.Count == 0) return;
                lock (_histLock)
                {
                    List<KeyValuePair<string, string>> h;
                    if (!_history.TryGetValue(chatId, out h)) { h = new System.Collections.Generic.List<KeyValuePair<string, string>>(); _history[chatId] = h; }
                    if (h.Count == 0) h.AddRange(rows);
                }
            }
            catch (Exception) { }
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
                System.Collections.Generic.List<string> qtoks = useCache ? Tokens(NormQ(incomingText)) : null;
                try
                {
                    string ovr = DbOverrideHit(NormQ(incomingText), Tokens(NormQ(incomingText)));
                    if (!string.IsNullOrWhiteSpace(ovr))
                    {
                        PushHistory(chatId, incomingText, ovr);
                        try { DbBump(chatId, 1, 0, 0, 1); } catch (Exception) { }
                        try { SendEngine.Log("AI override hit"); } catch (Exception) { }
                        return ApplySegments(ovr, incomingText, cfg);
                    }
                }
                catch (Exception) { }
                if (useCache)
                {
                    try
                    {
                        string hit = DbExactHit(ckey, cfg.CacheTtlDays);
                        if (string.IsNullOrWhiteSpace(hit) && cfg.CacheFuzzy) hit = DbFuzzyHit(cfg.Model, extraInstruction, NormQ(incomingText), qtoks, cfg.CacheTtlDays);
                        if (!string.IsNullOrWhiteSpace(hit))
                        {
                            PushHistory(chatId, incomingText, hit);
                            try { DbBump(chatId, 1, 1, 0, 0); } catch (Exception) { }
                            try { SendEngine.Log("AI cache hit: " + NormQ(incomingText).Substring(0, System.Math.Min(60, NormQ(incomingText).Length))); } catch (Exception) { }
                            return ApplySegments(hit, incomingText, cfg);
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
                            try { DbStore(ckey, incomingText, NormQ(incomingText), ans, cfg.Model, extraInstruction, cfg.MaxCacheEntries); }
                            catch (Exception) { }
                            try { RefreshFaqFile(); } catch (Exception) { }
                        }
                        try { DbBump(chatId, 1, 0, 1, 0); } catch (Exception) { }
                        return ApplySegments(ans, incomingText, cfg);
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

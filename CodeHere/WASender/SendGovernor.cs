using System;
using System.Data.SQLite;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Web.WebView2.WinForms;

namespace WASender
{
    public class SendLimits
    {
        public int DailyPerAccount { get; set; } = 500;
        public int MinScore { get; set; } = 40;
        public bool BlockOnLowScore { get; set; } = true;
    }

    /// <summary>Governor anti-banned: kuota harian per akun + blokir skor rendah.</summary>
    public static class SendGovernor
    {
        private static string _acctCache = "";
        private static DateTime _acctTime = DateTime.MinValue;

        public static string GetLimitsPath()
        {
            try { return Path.Combine(Config.GetAppDataFolder(), "send_limits.json"); }
            catch (Exception) { return "send_limits.json"; }
        }

        public static SendLimits LoadLimits()
        {
            try
            {
                string p = GetLimitsPath();
                if (!File.Exists(p))
                {
                    var t = new SendLimits();
                    try { File.WriteAllText(p, Newtonsoft.Json.JsonConvert.SerializeObject(t, Newtonsoft.Json.Formatting.Indented)); } catch (Exception) { }
                    return t;
                }
                var s = Newtonsoft.Json.JsonConvert.DeserializeObject<SendLimits>(File.ReadAllText(p));
                if (s == null) return new SendLimits();
                if (s.DailyPerAccount <= 0) s.DailyPerAccount = 500;
                if (s.MinScore < 0 || s.MinScore > 100) s.MinScore = 40;
                return s;
            }
            catch (Exception) { return new SendLimits(); }
        }

        public static void SaveLimits(SendLimits l)
        {
            try { File.WriteAllText(GetLimitsPath(), Newtonsoft.Json.JsonConvert.SerializeObject(l, Newtonsoft.Json.Formatting.Indented)); }
            catch (Exception) { }
        }

        private static string DbPath()
        {
            try { return Path.Combine(Config.GetAppDataFolder(), "wa_monitor.db"); }
            catch (Exception) { return "wa_monitor.db"; }
        }

        private static void EnsureQuotaTable()
        {
            try
            {
                using (var c = new SQLiteConnection("Data Source=" + DbPath() + ";Version=3;"))
                {
                    c.Open();
                    using (var cmd = c.CreateCommand())
                    {
                        cmd.CommandText = "CREATE TABLE IF NOT EXISTS send_quota(date TEXT NOT NULL, account TEXT NOT NULL, n INTEGER NOT NULL DEFAULT 0, PRIMARY KEY(date, account));";
                        cmd.ExecuteNonQuery();
                    }
                }
            }
            catch (Exception) { }
        }

        public static async Task<string> ResolveAccountAsync(object wv, object driver, int browserType)
        {
            try
            {
                if ((DateTime.Now - _acctTime).TotalMinutes < 5 && !string.IsNullOrEmpty(_acctCache)) return _acctCache;
                string a = "";
                try
                {
                    if (browserType == 2 && wv is WebView2)
                    {
                        a = await WPPHelper.getMyUserId((WebView2)wv);
                    }
                }
                catch (Exception) { }
                if (string.IsNullOrWhiteSpace(a)) a = "default";
                a = new string((a ?? "").Where(char.IsDigit).ToArray());
                if (string.IsNullOrEmpty(a)) a = "default";
                _acctCache = a;
                _acctTime = DateTime.Now;
                return a;
            }
            catch (Exception) { return "default"; }
        }

        public static int TodayCount(string account)
        {
            try
            {
                EnsureQuotaTable();
                using (var c = new SQLiteConnection("Data Source=" + DbPath() + ";Version=3;"))
                {
                    c.Open();
                    using (var cmd = c.CreateCommand())
                    {
                        cmd.CommandText = "SELECT n FROM send_quota WHERE date=@d AND account=@a LIMIT 1;";
                        cmd.Parameters.AddWithValue("@d", DateTime.Now.ToString("yyyy-MM-dd"));
                        cmd.Parameters.AddWithValue("@a", account ?? "default");
                        object o = cmd.ExecuteScalar();
                        if (o == null || o == DBNull.Value) return 0;
                        return Convert.ToInt32(o);
                    }
                }
            }
            catch (Exception) { return 0; }
        }

        public static void RecordSent(string account)
        {
            try
            {
                EnsureQuotaTable();
                using (var c = new SQLiteConnection("Data Source=" + DbPath() + ";Version=3;"))
                {
                    c.Open();
                    using (var cmd = c.CreateCommand())
                    {
                        cmd.CommandText = "INSERT INTO send_quota(date,account,n) VALUES(@d,@a,1) ON CONFLICT(date,account) DO UPDATE SET n=n+1;";
                        cmd.Parameters.AddWithValue("@d", DateTime.Now.ToString("yyyy-MM-dd"));
                        cmd.Parameters.AddWithValue("@a", account ?? "default");
                        cmd.ExecuteNonQuery();
                    }
                }
                _acctCache = account ?? "default";
                _acctTime = DateTime.Now;
            }
            catch (Exception) { }
        }

        public static async Task RecordSentAsync(object wv, object driver, int browserType)
        {
            try
            {
                string acct = await ResolveAccountAsync(wv, driver, browserType);
                RecordSent(acct);
            }
            catch (Exception) { }
        }

        public static System.Collections.Generic.List<string> TodayAll()
        {
            var out_ = new System.Collections.Generic.List<string>();
            try
            {
                EnsureQuotaTable();
                SendLimits lim = LoadLimits();
                using (var c = new SQLiteConnection("Data Source=" + DbPath() + ";Version=3;"))
                {
                    c.Open();
                    using (var cmd = c.CreateCommand())
                    {
                        cmd.CommandText = "SELECT account,n FROM send_quota WHERE date=@d ORDER BY n DESC LIMIT 50;";
                        cmd.Parameters.AddWithValue("@d", DateTime.Now.ToString("yyyy-MM-dd"));
                        using (var r = cmd.ExecuteReader())
                        {
                            while (r.Read()) out_.Add((r.IsDBNull(0) ? "?" : r.GetString(0)) + " | " + r.GetInt64(1) + " | " + lim.DailyPerAccount);
                        }
                    }
                }
            }
            catch (Exception) { }
            return out_;
        }

        public static int LastScore()
        {
            try
            {
                string db = DbPath();
                if (!File.Exists(db)) return -1;
                using (var c = new SQLiteConnection("Data Source=" + db + ";Version=3;"))
                {
                    c.Open();
                    using (var cmd = c.CreateCommand())
                    {
                        cmd.CommandText = "SELECT score FROM health ORDER BY ts DESC LIMIT 1;";
                        object o = cmd.ExecuteScalar();
                        if (o == null || o == DBNull.Value) return -1;
                        return Convert.ToInt32(o);
                    }
                }
            }
            catch (Exception) { return -1; }
        }

        public static async Task<Tuple<bool, string>> CanSendAsync(object wv, object driver, int browserType)
        {
            try
            {
                SendLimits lim = LoadLimits();
                string acct = await ResolveAccountAsync(wv, driver, browserType);
                int used = TodayCount(acct);
                if (used >= lim.DailyPerAccount)
                {
                    return Tuple.Create(false, "LIMIT harian tercapai (" + used + "/" + lim.DailyPerAccount + ")");
                }
                if (lim.BlockOnLowScore)
                {
                    int sc = LastScore();
                    if (sc >= 0 && sc < lim.MinScore)
                    {
                        return Tuple.Create(false, "Skor WA rendah (" + sc + "<" + lim.MinScore + ")");
                    }
                }
                return Tuple.Create(true, "");
            }
            catch (Exception) { return Tuple.Create(true, ""); }
        }
    }
}

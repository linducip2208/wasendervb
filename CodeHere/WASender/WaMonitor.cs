using Microsoft.Web.WebView2.WinForms;
using Newtonsoft.Json;
using OpenQA.Selenium;
using System;
using System.Data.SQLite;
using System.IO;
using System.Threading.Tasks;

namespace WASender
{
    /// <summary>
    /// Monitoring kesehatan sesi WhatsApp (tanpa ubah UI).
    /// Dijadwalkan dari worker Auto-Reply (tiap 5 menit): status koneksi, masa aktif,
    /// jumlah chat/unread/grup/kontak, baterai HP, skor kesehatan 0-100.
    /// Output: %ProgramData%\WaSender\wa_monitor.db, wa_status.txt, wa_health.log.
    /// </summary>
    public static class WaMonitor
    {
        private static readonly object _lock = new object();
        private static DateTime _lastTick = DateTime.MinValue;
        private static DateTime _sessionStart = DateTime.MinValue;
        private static int _disconnects = 0;
        private static string _lastSummaryDate = "";

        private const string JS_STATE =
            "(function(){var s='?',m='?';try{if(window.WPP&&WPP.conn){" +
            "try{if(WPP.conn.getConnectionState){s=String(WPP.conn.getConnectionState());}" +
            "else if(WPP.conn.isAuthenticated){s=WPP.conn.isAuthenticated()?'AUTH':'NOAUTH';}}catch(e){}" +
            "try{if(WPP.conn.getMyUserId){var u=WPP.conn.getMyUserId();m=String((u&&u._serialized)||u||'?');}" +
            "else if(WPP.contact&&WPP.contact.getMe){var me=WPP.contact.getMe();m=String((me&&(me.id&&(me.id._serialized||me.id.user)))||'?');}}catch(e){}" +
            "}}catch(e){}if(m.indexOf('object')>=0)m='?';return s+'|'+m;})()";

        private const string JS_COUNTS =
            "(function(){var a=[0,0,0,0];try{if(window.WPP&&WPP.chat){var cs=[];" +
            "try{cs=WPP.chat.list()||[];}catch(e){}a[0]=cs.length;var u=0,g=0;" +
            "for(var i=0;i<cs.length;i++){try{var c=cs[i];u+=(c.unreadCount||0);if(c.isGroup||c.groupMetadata)g++;}catch(e){}}" +
            "a[1]=u;a[3]=g;try{if(window.WPP&&WPP.contact){var ks=WPP.contact.list()||[];a[2]=ks.length;}}catch(e){}}" +
            "}catch(e){}return a.join('|');})()";

        private const string JS_POWER =
            "(function(){var b=-1,p='?';try{if(window.WPP&&WPP.conn){" +
            "try{if(WPP.conn.getBatteryLevel){var v=WPP.conn.getBatteryLevel();b=parseInt(v,10);if(isNaN(b))b=-1;}}catch(e){}" +
            "try{if(WPP.conn.isPhoneConnected){p=WPP.conn.isPhoneConnected()?'YES':'NO';}" +
            "else if(WPP.conn.isAuthenticated){p=WPP.conn.isAuthenticated()?'YES':'?';}}catch(e){}" +
            "}}catch(e){}return b+'|'+p;})()";

        private static string BaseDir()
        {
            try
            {
                string w = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), Config.WaSenderFolderName);
                if (!Directory.Exists(w)) Directory.CreateDirectory(w);
                return w;
            }
            catch (Exception) { return AppDomain.CurrentDomain.BaseDirectory; }
        }

        private static string DbPath() { return Path.Combine(BaseDir(), "wa_monitor.db"); }
        private static string StatusPath() { return Path.Combine(BaseDir(), "wa_status.txt"); }
        private static string LogPath() { return Path.Combine(BaseDir(), "wa_health.log"); }

        private static long NowUnix() { return (long)(DateTime.UtcNow - new DateTime(1970, 1, 1)).TotalSeconds; }

        private static void HLog(string line)
        {
            try
            {
                lock (_lock) { File.AppendAllText(LogPath(), DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + " | " + line + "\r\n"); }
            }
            catch (Exception) { }
        }

        private static string SelEval(object driver, string js)
        {
            try
            {
                var jse = driver as IJavaScriptExecutor;
                if (jse == null) return "";
                object o = jse.ExecuteScript("return (" + js + ");");
                return o != null ? o.ToString() : "";
            }
            catch (Exception) { return ""; }
        }

        private static async Task<string> WvEval(WebView2 wv, string js)
        {
            try
            {
                if (wv == null) return "";
                string raw = await wv.ExecuteScriptAsync(js);
                if (string.IsNullOrEmpty(raw)) return "";
                raw = raw.Trim();
                if (raw.StartsWith("\""))
                {
                    try { return JsonConvert.DeserializeObject<string>(raw) ?? ""; }
                    catch (Exception) { return raw.Trim('"'); }
                }
                if (raw == "null") return "";
                return raw;
            }
            catch (Exception) { return ""; }
        }

        private static string[] Split2(string s, char sep)
        {
            try
            {
                string[] p = (s ?? "").Split(sep);
                if (p.Length < 2) return new string[] { p.Length > 0 ? p[0] : "", "" };
                return new string[] { p[0], p[1] };
            }
            catch (Exception) { return new string[] { "", "" }; }
        }

        private static int ParseInt(string s)
        {
            try { return Convert.ToInt32(s); }
            catch (Exception) { return 0; }
        }

        public static async Task TickAsync(object driverOrWv, int browserType)
        {
            try
            {
                if ((DateTime.Now - _lastTick).TotalMinutes < 5) return;
                _lastTick = DateTime.Now;
                if (_sessionStart == DateTime.MinValue) _sessionStart = DateTime.Now;

                string state = "?", me = "?", counts = "0|0|0|0", pow = "-1|?";
                try
                {
                    if (browserType == 1)
                    {
                        state = Split2(SelEval(driverOrWv, JS_STATE), '|')[0];
                        me = Split2(SelEval(driverOrWv, JS_STATE), '|')[1];
                        counts = SelEval(driverOrWv, JS_COUNTS);
                        pow = SelEval(driverOrWv, JS_POWER);
                    }
                    else
                    {
                        WebView2 wv = driverOrWv as WebView2;
                        if (wv == null) return;
                        string[] sm = Split2(await WvEval(wv, JS_STATE), '|');
                        state = sm[0]; me = sm[1];
                        counts = await WvEval(wv, JS_COUNTS);
                        pow = await WvEval(wv, JS_POWER);
                    }
                }
                catch (Exception) { }

                string[] cp = (counts ?? "0|0|0|0").Split('|');
                int chats = cp.Length > 0 ? ParseInt(cp[0]) : 0;
                int unread = cp.Length > 1 ? ParseInt(cp[1]) : 0;
                int contacts = cp.Length > 2 ? ParseInt(cp[2]) : 0;
                int groups = cp.Length > 3 ? ParseInt(cp[3]) : 0;
                string[] pp = Split2(pow, '|');
                int batt = 0;
                try { batt = Convert.ToInt32(pp[0]); } catch (Exception) { batt = -1; }
                string phone = pp[1];

                bool okState = state == "CONNECTED" || state == "AUTH" || state == "OPEN" || state == "SYNCING";
                bool authed = okState || (state ?? "").Contains("AUTH");
                int score = (okState ? 40 : 0) + (authed ? 30 : 0)
                    + (batt < 0 ? 8 : (batt >= 50 ? 15 : (batt >= 20 ? 10 : 5)))
                    + (unread < 200 ? 15 : 5);
                int uptimeMin = (int)(DateTime.Now - _sessionStart).TotalMinutes;
                if (!okState && !string.IsNullOrEmpty(state) && state != "?")
                {
                    _disconnects++;
                    HLog("WARN terputus (state=" + state + ", total=" + _disconnects + ")");
                }

                try
                {
                    lock (_lock)
                    {
                        using (var c = new SQLiteConnection("Data Source=" + DbPath() + ";Version=3;"))
                        {
                            c.Open();
                            using (var cmd = c.CreateCommand())
                            {
                                cmd.CommandText = "CREATE TABLE IF NOT EXISTS health(ts INTEGER PRIMARY KEY, connected INT, auth INT, state TEXT, me TEXT, chats INT, unread INT, contacts INT, groups INT, battery INT, phone TEXT, uptime_min INT, score INT);";
                                cmd.ExecuteNonQuery();
                            }
                            using (var cmd = c.CreateCommand())
                            {
                                cmd.CommandText = "INSERT OR REPLACE INTO health(ts,connected,auth,state,me,chats,unread,contacts,groups,battery,phone,uptime_min,score) VALUES(@t,@co,@au,@s,@m,@ch,@u,@ct,@g,@b,@p,@up,@sc);";
                                cmd.Parameters.AddWithValue("@t", NowUnix());
                                cmd.Parameters.AddWithValue("@co", okState ? 1 : 0);
                                cmd.Parameters.AddWithValue("@au", authed ? 1 : 0);
                                cmd.Parameters.AddWithValue("@s", state ?? "?");
                                cmd.Parameters.AddWithValue("@m", me ?? "?");
                                cmd.Parameters.AddWithValue("@ch", chats);
                                cmd.Parameters.AddWithValue("@u", unread);
                                cmd.Parameters.AddWithValue("@ct", contacts);
                                cmd.Parameters.AddWithValue("@g", groups);
                                cmd.Parameters.AddWithValue("@b", batt);
                                cmd.Parameters.AddWithValue("@p", phone ?? "?");
                                cmd.Parameters.AddWithValue("@up", uptimeMin);
                                cmd.Parameters.AddWithValue("@sc", score);
                                cmd.ExecuteNonQuery();
                            }
                            try
                            {
                                using (var cmd = c.CreateCommand())
                                {
                                    cmd.CommandText = "DELETE FROM health WHERE ts NOT IN (SELECT ts FROM health ORDER BY ts DESC LIMIT 5000);";
                                    cmd.ExecuteNonQuery();
                                }
                            }
                            catch (Exception) { }
                        }
                    }
                }
                catch (Exception) { }

                try
                {
                    TimeSpan up = DateTime.Now - _sessionStart;
                    string txt = "WA Status " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "\r\n"
                        + "State: " + state + " | Akun: " + me + "\r\n"
                        + "Chat: " + chats + " (grup " + groups + ", unread " + unread + ") | Kontak: " + contacts + "\r\n"
                        + "Baterai HP: " + (batt < 0 ? "?" : batt + "%") + " | HP: " + phone + "\r\n"
                        + "Masa aktif sesi: " + ((int)up.TotalHours) + "j " + (up.Minutes) + "m | Putus: " + _disconnects + "x | Skor: " + score + "/100\r\n";
                    lock (_lock) { File.WriteAllText(StatusPath(), txt); }
                }
                catch (Exception) { }

                HLog("state=" + state + " chats=" + chats + " unread=" + unread + " groups=" + groups + " contacts=" + contacts + " batt=" + batt + " score=" + score);
                await DailySummaryAsync(score, chats, unread, uptimeMin);
            }
            catch (Exception) { }
        }

        private static async Task DailySummaryAsync(int score, int chats, int unread, int uptimeMin)
        {
            try
            {
                string today = DateTime.Now.ToString("yyyy-MM-dd");
                if (_lastSummaryDate == today) return;
                _lastSummaryDate = today;
                int q = 0, hits = 0, calls = 0, ovr = 0;
                string top = "";
                try
                {
                    string aiDb = Path.Combine(BaseDir(), "ai_cache.db");
                    if (File.Exists(aiDb))
                    {
                        using (var c = new SQLiteConnection("Data Source=" + aiDb + ";Version=3;"))
                        {
                            c.Open();
                            using (var cmd = c.CreateCommand())
                            {
                                cmd.CommandText = "SELECT COALESCE(SUM(questions),0),COALESCE(SUM(cache_hits),0),COALESCE(SUM(ai_calls),0),COALESCE(SUM(overrides),0) FROM ai_stats WHERE date=@d;";
                                cmd.Parameters.AddWithValue("@d", today);
                                using (var r = cmd.ExecuteReader())
                                {
                                    if (r.Read()) { q = Convert.ToInt32(r[0]); hits = Convert.ToInt32(r[1]); calls = Convert.ToInt32(r[2]); ovr = Convert.ToInt32(r[3]); }
                                }
                            }
                            try
                            {
                                using (var cmd = c.CreateCommand())
                                {
                                    cmd.CommandText = "SELECT question,hits FROM ai_cache ORDER BY hits DESC LIMIT 5;";
                                    using (var r = cmd.ExecuteReader())
                                    {
                                        int i = 1;
                                        while (r.Read()) { top += " [" + (i++) + "](" + r.GetInt32(1) + "x) " + (r.IsDBNull(0) ? "" : r.GetString(0)) + ";"; }
                                    }
                                }
                            }
                            catch (Exception) { }
                        }
                    }
                }
                catch (Exception) { }
                HLog("RINGKASAN " + today + ": tanya=" + q + " cache=" + hits + " ai=" + calls + " override=" + ovr
                    + " chats=" + chats + " unread=" + unread + " uptime=" + uptimeMin + "m putus=" + _disconnects + "x skor=" + score
                    + " top=[" + top + "]");
                try { WaReporter.WriteDailyReport(_disconnects); } catch (Exception) { }
                try { WaReporter.WriteDailyReport(_disconnects); } catch (Exception) { }
                await Task.Delay(1);
            }
            catch (Exception) { }
        }
    }
}

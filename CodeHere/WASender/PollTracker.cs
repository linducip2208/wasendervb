using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.IO;

namespace WASender
{
    /// <summary>Snapshot hasil poll + tren per opsi (disimpan tiap GetPollResults).</summary>
    public static class PollTracker
    {
        private static string DbPath()
        {
            try { return System.IO.Path.Combine(Config.GetAppDataFolder(), "ai_cache.db"); }
            catch (Exception) { return "ai_cache.db"; }
        }

        private static void Ensure()
        {
            try
            {
                using (var c = new SQLiteConnection("Data Source=" + DbPath() + ";Version=3;"))
                {
                    c.Open();
                    using (var cmd = c.CreateCommand())
                    {
                        cmd.CommandText = "CREATE TABLE IF NOT EXISTS poll_history(ts INTEGER NOT NULL, poll TEXT NOT NULL, opt TEXT NOT NULL, votes INTEGER NOT NULL DEFAULT 0);CREATE INDEX IF NOT EXISTS idx_poll ON poll_history(poll, ts);";
                        cmd.ExecuteNonQuery();
                    }
                    try
                    {
                        using (var cmd = c.CreateCommand())
                        {
                            cmd.CommandText = "DELETE FROM poll_history WHERE ts < @t;";
                            cmd.Parameters.AddWithValue("@t", (long)(DateTime.UtcNow - TimeSpan.FromDays(30)).Subtract(new DateTime(1970, 1, 1)).TotalSeconds);
                            cmd.ExecuteNonQuery();
                        }
                    }
                    catch (Exception) { }
                }
            }
            catch (Exception) { }
        }

        public static void Snapshot(List<global::Models.PollReportModel> polls)
        {
            try
            {
                if (polls == null || polls.Count == 0) return;
                Ensure();
                long now = (long)(DateTime.UtcNow - new DateTime(1970, 1, 1)).TotalSeconds;
                using (var c = new SQLiteConnection("Data Source=" + DbPath() + ";Version=3;"))
                {
                    c.Open();
                    using (var tx = c.BeginTransaction())
                    {
                        foreach (var p in polls)
                        {
                            if (p == null || p.options == null) continue;
                            string pn = p.PollName ?? "";
                            if (pn == "") continue;
                            HashSet<string> voters = new HashSet<string>();
                            foreach (var o in p.options)
                            {
                                if (o == null) continue;
                                int v = 0;
                                try { v = o.selectedVotesList != null ? o.selectedVotesList.Count : 0; } catch (Exception) { }
                                using (var cmd = c.CreateCommand())
                                {
                                    cmd.CommandText = "INSERT INTO poll_history(ts,poll,opt,votes) VALUES(@t,@p,@o,@v);";
                                    cmd.Parameters.AddWithValue("@t", now);
                                    cmd.Parameters.AddWithValue("@p", pn.Length > 200 ? pn.Substring(0, 200) : pn);
                                    string on = o.name ?? "";
                                    cmd.Parameters.AddWithValue("@o", on.Length > 200 ? on.Substring(0, 200) : on);
                                    cmd.Parameters.AddWithValue("@v", v);
                                    cmd.ExecuteNonQuery();
                                }
                                try
                                {
                                    if (o.selectedVotesList != null)
                                        foreach (var s in o.selectedVotesList)
                                        {
                                            try { if (s != null && !string.IsNullOrEmpty(s.sender)) voters.Add(s.sender); } catch (Exception) { }
                                        }
                                }
                                catch (Exception) { }
                            }
                            using (var cmd = c.CreateCommand())
                            {
                                cmd.CommandText = "INSERT INTO poll_history(ts,poll,opt,votes) VALUES(@t,@p,'_voters',@v);";
                                cmd.Parameters.AddWithValue("@t", now);
                                cmd.Parameters.AddWithValue("@p", pn.Length > 200 ? pn.Substring(0, 200) : pn);
                                cmd.Parameters.AddWithValue("@v", voters.Count);
                                cmd.ExecuteNonQuery();
                            }
                        }
                        tx.Commit();
                    }
                }
            }
            catch (Exception) { }
        }

        public static string TrendHtmlAll()
        {
            try
            {
                Ensure();
                List<string> polls = new List<string>();
                using (var c = new SQLiteConnection("Data Source=" + DbPath() + ";Version=3;"))
                {
                    c.Open();
                    using (var cmd = c.CreateCommand())
                    {
                        cmd.CommandText = "SELECT DISTINCT poll FROM poll_history WHERE ts >= @t ORDER BY poll LIMIT 20;";
                        cmd.Parameters.AddWithValue("@t", (long)(DateTime.UtcNow - TimeSpan.FromDays(7)).Subtract(new DateTime(1970, 1, 1)).TotalSeconds);
                        using (var r = cmd.ExecuteReader())
                        {
                            while (r.Read()) { string p = r.IsDBNull(0) ? "" : r.GetString(0); if (p != "") polls.Add(p); }
                        }
                    }
                }
                if (polls.Count == 0) return "";
                System.Text.StringBuilder sb = new System.Text.StringBuilder();
                sb.Append("<h3>Tren 7 hari (perubahan vote)</h3>");
                sb.Append("<table style='width:100%;border:1px solid black;' border='1'>");
                sb.Append("<thead><tr><th>Poll</th><th>Opsi</th><th>Awal</th><th>Terakhir</th><th>Selisih</th></tr></thead><tbody>");
                using (var c = new SQLiteConnection("Data Source=" + DbPath() + ";Version=3;"))
                {
                    c.Open();
                    foreach (string p in polls)
                    {
                        using (var cmd = c.CreateCommand())
                        {
                            cmd.CommandText = "SELECT opt, MIN(votes), MAX(votes), COUNT(*) FROM (SELECT opt, votes FROM poll_history WHERE poll=@p AND opt<>'_voters' AND ts>=@t ORDER BY ts) GROUP BY opt;";
                            cmd.Parameters.AddWithValue("@p", p);
                            cmd.Parameters.AddWithValue("@t", (long)(DateTime.UtcNow - TimeSpan.FromDays(7)).Subtract(new DateTime(1970, 1, 1)).TotalSeconds);
                            using (var r = cmd.ExecuteReader())
                            {
                                while (r.Read())
                                {
                                    string o = r.IsDBNull(0) ? "" : r.GetString(0);
                                    long a = r.IsDBNull(1) ? 0 : r.GetInt64(1);
                                    long b = r.IsDBNull(2) ? 0 : r.GetInt64(2);
                                    sb.Append("<tr><td>" + p + "</td><td>" + o + "</td><td>" + a + "</td><td>" + b + "</td><td>" + (b - a >= 0 ? "+" : "") + (b - a) + "</td></tr>");
                                }
                            }
                        }
                    }
                }
                sb.Append("</tbody></table>");
                return sb.ToString();
            }
            catch (Exception) { return ""; }
        }
    }
}

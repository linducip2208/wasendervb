using OfficeOpenXml;
using System;
using System.Data.SQLite;
using System.IO;

namespace WASender
{
    /// <summary>
    /// Laporan harian otomatis (tanpa ubah UI): dipicu sekali sehari dari WaMonitor.
    /// Output: %ProgramData%\WaSender\Reports\laporan-YYYY-MM-DD.txt + .xlsx
    /// (Ringkasan, FAQ Top, Per Nomor, Kesehatan).
    /// </summary>
    public static class WaReporter
    {
        private static string BaseDir()
        {
            try { return Config.GetAppDataFolder(); }
            catch (Exception) { return AppDomain.CurrentDomain.BaseDirectory; }
        }

        public static void WriteDailyReport(int disconnects)
        {
            try
            {
                string dir = Path.Combine(BaseDir(), "Reports");
                try { if (!Directory.Exists(dir)) Directory.CreateDirectory(dir); } catch (Exception) { }
                string today = DateTime.Now.ToString("yyyy-MM-dd");
                string aiDb = Path.Combine(BaseDir(), "ai_cache.db");
                string moDb = Path.Combine(BaseDir(), "wa_monitor.db");

                int q = 0, hits = 0, calls = 0, ovr = 0, cacheRows = 0, cacheHits = 0, ovrRows = 0;
                string topFaq = "";
                string perNomor = "";
                string healthRows = "";
                int hCh = 0, hUn = 0, hGr = 0, hCt = 0, hBt = -1, hSc = 0, hUp = 0, hDc = 0;
                string hState = "?", hMe = "?", hPhone = "?";
                double avgScore = 0;
                try
                {
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
                                    cmd.CommandText = "SELECT COUNT(*),COALESCE(SUM(hits),0) FROM ai_cache;";
                                    using (var r = cmd.ExecuteReader())
                                    {
                                        if (r.Read()) { cacheRows = Convert.ToInt32(r[0]); cacheHits = Convert.ToInt32(r[1]); }
                                    }
                                }
                            }
                            catch (Exception) { }
                            try
                            {
                                using (var cmd = c.CreateCommand())
                                {
                                    cmd.CommandText = "SELECT COUNT(*) FROM ai_override WHERE enabled=1;";
                                    using (var r = cmd.ExecuteReader()) { if (r.Read()) ovrRows = Convert.ToInt32(r[0]); }
                                }
                            }
                            catch (Exception) { }
                            try
                            {
                                using (var cmd = c.CreateCommand())
                                {
                                    cmd.CommandText = "SELECT question,hits FROM ai_cache ORDER BY hits DESC LIMIT 20;";
                                    using (var r = cmd.ExecuteReader())
                                    {
                                        int i = 1;
                                        while (r.Read()) { topFaq += (i++) + ". [" + r.GetInt64(1) + "x] " + (r.IsDBNull(0) ? "" : r.GetString(0)) + "\r\n"; }
                                    }
                                }
                            }
                            catch (Exception) { }
                            try
                            {
                                using (var cmd = c.CreateCommand())
                                {
                                    cmd.CommandText = "SELECT chat_id,questions,cache_hits,ai_calls,overrides FROM ai_stats WHERE date=@d ORDER BY questions DESC LIMIT 50;";
                                    cmd.Parameters.AddWithValue("@d", today);
                                    using (var r = cmd.ExecuteReader())
                                    {
                                        while (r.Read())
                                        {
                                            perNomor += (r.IsDBNull(0) ? "?" : r.GetString(0)) + "|" + r.GetInt64(1) + "|" + r.GetInt64(2) + "|" + r.GetInt64(3) + "|" + r.GetInt64(4) + "\n";
                                        }
                                    }
                                }
                            }
                            catch (Exception) { }
                        }
                    }
                }
                catch (Exception) { }
                try
                {
                    if (File.Exists(moDb))
                    {
                        using (var c = new SQLiteConnection("Data Source=" + moDb + ";Version=3;"))
                        {
                            c.Open();
                            long dayStart = (long)(new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day) - new DateTime(1970, 1, 1)).TotalSeconds;
                            try
                            {
                                using (var cmd = c.CreateCommand())
                                {
                                    cmd.CommandText = "SELECT state,me,chats,unread,contacts,groups,battery,phone,uptime_min,score FROM health ORDER BY ts DESC LIMIT 1;";
                                    using (var r = cmd.ExecuteReader())
                                    {
                                        if (r.Read())
                                        {
                                            hState = r.IsDBNull(0) ? "?" : r.GetString(0);
                                            hMe = r.IsDBNull(1) ? "?" : r.GetString(1);
                                            hCh = r.IsDBNull(2) ? 0 : r.GetInt32(2);
                                            hUn = r.IsDBNull(3) ? 0 : r.GetInt32(3);
                                            hCt = r.IsDBNull(4) ? 0 : r.GetInt32(4);
                                            hGr = r.IsDBNull(5) ? 0 : r.GetInt32(5);
                                            hBt = r.IsDBNull(6) ? -1 : r.GetInt32(6);
                                            hPhone = r.IsDBNull(7) ? "?" : r.GetString(7);
                                            hUp = r.IsDBNull(8) ? 0 : r.GetInt32(8);
                                            hSc = r.IsDBNull(9) ? 0 : r.GetInt32(9);
                                        }
                                    }
                                }
                            }
                            catch (Exception) { }
                            try
                            {
                                using (var cmd = c.CreateCommand())
                                {
                                    cmd.CommandText = "SELECT COALESCE(AVG(score),0),COALESCE(MIN(score),0),COALESCE(SUM(CASE WHEN connected=0 THEN 1 ELSE 0 END),0) FROM health WHERE ts>=@t;";
                                    cmd.Parameters.AddWithValue("@t", dayStart);
                                    using (var r = cmd.ExecuteReader())
                                    {
                                        if (r.Read()) { avgScore = Convert.ToDouble(r[0]); hDc = Convert.ToInt32(r[2]); }
                                    }
                                }
                            }
                            catch (Exception) { }
                            try
                            {
                                using (var cmd = c.CreateCommand())
                                {
                                    cmd.CommandText = "SELECT ts,state,chats,unread,groups,score FROM health WHERE ts>=@t ORDER BY ts DESC LIMIT 100;";
                                    cmd.Parameters.AddWithValue("@t", dayStart);
                                    using (var r = cmd.ExecuteReader())
                                    {
                                        while (r.Read())
                                        {
                                            DateTime dt = new DateTime(1970, 1, 1).AddSeconds(r.IsDBNull(0) ? 0 : r.GetInt64(0)).ToLocalTime();
                                            healthRows += dt.ToString("HH:mm") + "|" + (r.IsDBNull(1) ? "?" : r.GetString(1)) + "|" + (r.IsDBNull(2) ? 0 : r.GetInt32(2)) + "|" + (r.IsDBNull(3) ? 0 : r.GetInt32(3)) + "|" + (r.IsDBNull(4) ? 0 : r.GetInt32(4)) + "|" + (r.IsDBNull(5) ? 0 : r.GetInt32(5)) + "\n";
                                        }
                                    }
                                }
                            }
                            catch (Exception) { }
                        }
                    }
                }
                catch (Exception) { }

                double hitRate = q > 0 ? (double)(hits + ovr) * 100.0 / (double)q : 0.0;
                string txt = "LAPORAN HARIAN WASender - " + today + "\r\n"
                    + "Dibuat: " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "\r\n\r\n"
                    + "[1] KESEHATAN WHATSAPP\r\n"
                    + "State terakhir: " + hState + " | Akun: " + hMe + "\r\n"
                    + "Chat: " + hCh + " (grup " + hGr + ", unread " + hUn + ") | Kontak: " + hCt + "\r\n"
                    + "Baterai HP: " + (hBt < 0 ? "?" : hBt + "%") + " | HP: " + hPhone + "\r\n"
                    + "Skor terakhir: " + hSc + "/100 | Rata-rata hari ini: " + avgScore.ToString("0.0") + "\r\n"
                    + "Putus terdeteksi (di DB): " + hDc + "x | Putus sesi ini: " + disconnects + "x | Uptime: " + hUp + " mnt\r\n\r\n"
                    + "[2] AI HARI INI\r\n"
                    + "Pertanyaan: " + q + " | Cache hit: " + hits + " | Override: " + ovr + " | Panggilan API: " + calls + "\r\n"
                    + "Efisiensi (tanpa API): " + hitRate.ToString("0.0") + "%\r\n"
                    + "Isi cache: " + cacheRows + " entri (" + cacheHits + " total hit) | Override aktif: " + ovrRows + "\r\n\r\n"
                    + "[3] FAQ POPULER\r\n" + (string.IsNullOrEmpty(topFaq) ? "(belum ada)\r\n" : topFaq) + "\r\n"
                    + "[4] Nomor teraktif hari ini: lihat file Excel.\r\n";
                File.WriteAllText(Path.Combine(dir, "laporan-" + today + ".txt"), txt);

                try
                {
                    ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
                    using (ExcelPackage xl = new ExcelPackage(new FileInfo(Path.Combine(dir, "laporan-" + today + ".xlsx"))))
                    {
                        var ws0 = xl.Workbook.Worksheets.Add("Ringkasan");
                        string[,] kv = new string[,] {
                            {"Tanggal", today}, {"State WA", hState}, {"Akun", hMe},
                            {"Chat / Grup / Unread / Kontak", hCh + " / " + hGr + " / " + hUn + " / " + hCt},
                            {"Baterai HP", hBt < 0 ? "?" : hBt + "%"}, {"Skor terakhir / rata-rata", hSc + " / " + avgScore.ToString("0.0")},
                            {"Putus (DB/sesi)", hDc + " / " + disconnects}, {"Uptime (mnt)", hUp.ToString()},
                            {"Tanya / Cache / Override / API", q + " / " + hits + " / " + ovr + " / " + calls},
                            {"Efisiensi tanpa API", hitRate.ToString("0.0") + "%"},
                            {"Isi cache (entri/hit)", cacheRows + " / " + cacheHits}, {"Override aktif", ovrRows.ToString()}
                        };
                        ws0.Cells[1, 1].Value = "Laporan Harian WASender";
                        for (int i = 0; i < kv.GetLength(0); i++) { ws0.Cells[i + 2, 1].Value = kv[i, 0]; ws0.Cells[i + 2, 2].Value = kv[i, 1]; }
                        ws0.Cells.AutoFitColumns();

                        var ws1 = xl.Workbook.Worksheets.Add("FAQ Top20");
                        ws1.Cells[1, 1].Value = "No"; ws1.Cells[1, 2].Value = "Hits"; ws1.Cells[1, 3].Value = "Pertanyaan";
                        if (!string.IsNullOrEmpty(topFaq))
                        {
                            int r = 2;
                            foreach (string ln in topFaq.Split(new char[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries))
                            {
                                string s = ln;
                                int dot = s.IndexOf('.');
                                int br = s.IndexOf(']');
                                if (dot > 0 && br > dot)
                                {
                                    ws1.Cells[r, 1].Value = s.Substring(0, dot).Trim();
                                    ws1.Cells[r, 2].Value = s.Substring(dot + 1, br - dot - 1).Replace("[", "").Replace("x", "").Trim();
                                    ws1.Cells[r, 3].Value = s.Substring(br + 1).Trim();
                                    r++;
                                }
                            }
                        }
                        ws1.Cells.AutoFitColumns();

                        var ws2 = xl.Workbook.Worksheets.Add("Per Nomor");
                        ws2.Cells[1, 1].Value = "Nomor"; ws2.Cells[1, 2].Value = "Tanya"; ws2.Cells[1, 3].Value = "Cache"; ws2.Cells[1, 4].Value = "API"; ws2.Cells[1, 5].Value = "Override";
                        int rr2 = 2;
                        if (!string.IsNullOrEmpty(perNomor))
                        {
                            foreach (string ln in perNomor.Split(new char[] { '\n' }, StringSplitOptions.RemoveEmptyEntries))
                            {
                                string[] p = ln.Split('|');
                                if (p.Length == 5) { for (int k = 0; k < 5; k++) ws2.Cells[rr2, k + 1].Value = p[k]; rr2++; }
                            }
                        }
                        ws2.Cells.AutoFitColumns();

                        var ws3 = xl.Workbook.Worksheets.Add("Kesehatan");
                        ws3.Cells[1, 1].Value = "Jam"; ws3.Cells[1, 2].Value = "State"; ws3.Cells[1, 3].Value = "Chat"; ws3.Cells[1, 4].Value = "Unread"; ws3.Cells[1, 5].Value = "Grup"; ws3.Cells[1, 6].Value = "Skor";
                        int rr3 = 2;
                        if (!string.IsNullOrEmpty(healthRows))
                        {
                            foreach (string ln in healthRows.Split(new char[] { '\n' }, StringSplitOptions.RemoveEmptyEntries))
                            {
                                string[] p = ln.Split('|');
                                if (p.Length == 6) { for (int k = 0; k < 6; k++) ws3.Cells[rr3, k + 1].Value = p[k]; rr3++; }
                            }
                        }
                        ws3.Cells.AutoFitColumns();
                        xl.Save();
                    }
                }
                catch (Exception) { }
            }
            catch (Exception) { }
        }
    }
}

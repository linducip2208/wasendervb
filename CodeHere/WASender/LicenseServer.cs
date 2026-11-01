using Newtonsoft.Json.Linq;
using System;
using System.IO;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace WASender
{
    /// <summary>
    /// Lisensi online via panel https://license.pandusolusi.com.
    /// Alur: user input kode XXXX-XXXX-XXXX-XXXX di form Activate ->
    /// POST /api/v1/licenses/verify {code,hwid} -> bila valid, simpan lokal
    /// (online_license.json) + anggap produk aktif. Verifikasi ulang berkala
    /// di background; hanya respons "valid=false" eksplisit yg menonaktifkan,
    /// error jaringan TIDAK PERNAH menonaktifkan (grace offline).
    /// </summary>
    public class OnlineLicenseInfo
    {
        public string Code { get; set; } = "";
        public string Hwid { get; set; } = "";
        public string Customer { get; set; } = "";
        public string IssuedOn { get; set; } = "";
        public string ExpiresOn { get; set; } = "";
        public string CheckedAt { get; set; } = "";
    }

    public static class LicenseServer
    {
        public const string ServerUrl = "https://license.pandusolusi.com";

        private static string FilePath()
        {
            try
            {
                string dir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "WaSender");
                if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
                return Path.Combine(dir, "online_license.json");
            }
            catch (Exception) { return "online_license.json"; }
        }

        public static string CurrentHwid()
        {
            try { return (Security.FingerPrint.Value() ?? "").Trim().ToLowerInvariant(); }
            catch (Exception) { return ""; }
        }

        public static bool LooksLikePanelCode(string key)
        {
            try
            {
                string t = (key ?? "").Trim().ToUpperInvariant().Replace("-", "").Replace(" ", "");
                if (t.Length != 16) return false;
                foreach (char c in t)
                {
                    if (!((c >= 'A' && c <= 'Z') || (c >= '0' && c <= '9'))) return false;
                }
                return true;
            }
            catch (Exception) { return false; }
        }

        public static async Task<Tuple<bool, string, OnlineLicenseInfo>> VerifyAsync(string code, string hwid)
        {
            var info = new OnlineLicenseInfo();
            try
            {
                string norm = (code ?? "").Trim().ToUpperInvariant();
                var payload = new JObject();
                payload["code"] = norm;
                payload["hwid"] = (hwid ?? "").Trim().ToLowerInvariant();
                payload["label"] = Environment.MachineName ?? "";
                try { payload["app_version"] = System.Windows.Forms.Application.ProductVersion ?? ""; }
                catch (Exception) { }
                string json = payload.ToString(Newtonsoft.Json.Formatting.None);
                var req = (HttpWebRequest)WebRequest.Create(ServerUrl + "/api/v1/licenses/verify");
                req.Method = "POST";
                req.ContentType = "application/json";
                req.Timeout = 20000;
                req.UserAgent = "WASender";
                byte[] buf = Encoding.UTF8.GetBytes(json);
                using (var rs = await req.GetRequestStreamAsync()) { await rs.WriteAsync(buf, 0, buf.Length); }
                string resp;
                try
                {
                    using (var rp = (HttpWebResponse)await req.GetResponseAsync())
                    using (var sr = new StreamReader(rp.GetResponseStream(), Encoding.UTF8)) resp = await sr.ReadToEndAsync();
                }
                catch (WebException wx)
                {
                    // 403/404 = jawaban server (invalid). Tanpa respons = masalah jaringan (null).
                    try
                    {
                        var er = wx.Response as HttpWebResponse;
                        if (er == null) return null;
                        using (var sr = new StreamReader(er.GetResponseStream(), Encoding.UTF8)) resp = await sr.ReadToEndAsync();
                    }
                    catch (Exception) { return null; }
                }
                var obj = JObject.Parse(resp);
                bool valid = false;
                try { valid = obj.Value<bool>("valid"); } catch (Exception) { }
                string msg = "";
                try { msg = (obj.Value<string>("message") ?? ""); } catch (Exception) { }
                if (!valid) return Tuple.Create(false, string.IsNullOrEmpty(msg) ? "Kode lisensi tidak valid." : msg, info);
                info.Code = norm;
                info.Hwid = (hwid ?? "").Trim().ToLowerInvariant();
                try { info.Customer = obj.Value<string>("customer") ?? ""; } catch (Exception) { }
                try { info.IssuedOn = obj.Value<string>("issued_on") ?? ""; } catch (Exception) { }
                try { info.ExpiresOn = obj.Value<string>("expires_on") ?? ""; } catch (Exception) { }
                info.CheckedAt = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                return Tuple.Create(true, string.IsNullOrEmpty(msg) ? "License is valid." : msg, info);
            }
            catch (Exception) { return null; }
        }

        public static void Save(OnlineLicenseInfo info)
        {
            try
            {
                File.WriteAllText(FilePath(), Newtonsoft.Json.JsonConvert.SerializeObject(info));
            }
            catch (Exception) { }
        }

        public static OnlineLicenseInfo Load()
        {
            try
            {
                string p = FilePath();
                if (!File.Exists(p)) return null;
                var info = Newtonsoft.Json.JsonConvert.DeserializeObject<OnlineLicenseInfo>(File.ReadAllText(p));
                if (info == null || string.IsNullOrEmpty(info.Code)) return null;
                return info;
            }
            catch (Exception) { return null; }
        }

        public static void Clear()
        {
            try { string p = FilePath(); if (File.Exists(p)) File.Delete(p); }
            catch (Exception) { }
        }

        private static bool NotExpired(string expiresOn)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(expiresOn)) return true; // lifetime
                DateTime d;
                if (DateTime.TryParse(expiresOn, out d)) return d.Date >= DateTime.Now.Date;
                return true;
            }
            catch (Exception) { return true; }
        }

        /// <summary>Cek lokal (tanpa internet): HWID cocok + belum kedaluwarsa.</summary>
        public static bool IsActiveLocal()
        {
            try
            {
                var info = Load();
                if (info == null) return false;
                string hw = CurrentHwid();
                if (string.IsNullOrEmpty(hw)) return false;
                if (!string.Equals(info.Hwid ?? "", hw, StringComparison.OrdinalIgnoreCase)) return false;
                return NotExpired(info.ExpiresOn);
            }
            catch (Exception) { return false; }
        }

        public static string IssuedText()
        {
            try
            {
                var info = Load();
                if (info == null || string.IsNullOrWhiteSpace(info.IssuedOn)) return "";
                DateTime d;
                if (DateTime.TryParse(info.IssuedOn, out d)) return d.ToString("dd-MMM-yyyy");
                return info.IssuedOn;
            }
            catch (Exception) { return ""; }
        }

        public static string ExpiryText()
        {
            try
            {
                var info = Load();
                if (info == null) return "";
                if (string.IsNullOrWhiteSpace(info.ExpiresOn)) return "";
                DateTime d;
                if (DateTime.TryParse(info.ExpiresOn, out d)) return d.ToString("dd-MMM-yyyy");
                return info.ExpiresOn;
            }
            catch (Exception) { return ""; }
        }

        public static int RemainingDays()
        {
            try
            {
                var info = Load();
                if (info == null) return -1;
                if (string.IsNullOrWhiteSpace(info.ExpiresOn)) return int.MaxValue;
                DateTime d;
                if (!DateTime.TryParse(info.ExpiresOn, out d)) return -1;
                int sisa = (int)Math.Ceiling((d.Date - DateTime.Now.Date).TotalDays);
                return sisa < 0 ? 0 : sisa;
            }
            catch (Exception) { return -1; }
        }

        /// <summary>Cek cepat koneksi ke server lisensi (tanpa login). True bila server menjawab HTTP apa pun.</summary>
        public static async Task<bool> PingAsync()
        {
            try
            {
                var req = (HttpWebRequest)WebRequest.Create(ServerUrl + "/");
                req.Method = "GET";
                req.Timeout = 8000;
                req.UserAgent = "WASender";
                try
                {
                    using (var rp = (HttpWebResponse)await req.GetResponseAsync()) return true;
                }
                catch (WebException wx)
                {
                    // 4xx/5xx = server hidup, hanya path/akses yg ditolak. Timeout/DNS = mati.
                    if (wx.Response is HttpWebResponse) return true;
                    return false;
                }
            }
            catch (Exception) { return false; }
        }

        /// <summary>Refresh di background; hanya respons invalid eksplisit yg menghapus lisensi lokal.</summary>
        public static async Task RefreshAsync()
        {
            try
            {
                var info = Load();
                if (info == null) return;
                var r = await VerifyAsync(info.Code, CurrentHwid());
                if (r == null) return; // jaringan bermasalah: biarkan lisensi lokal
                if (r.Item1) Save(r.Item3);
                else Clear(); // server bilang invalid/suspend/expired
            }
            catch (Exception) { }
        }
    }
}

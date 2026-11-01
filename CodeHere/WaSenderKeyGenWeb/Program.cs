using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Configuration;
using System.IO;
using System.Net;
using System.Text;
using System.Web;

namespace WaSenderKeyGenWeb
{
    /// <summary>
    /// Keygen versi web (pengganti Form1 desktop di WaSenderKeyGen).
    /// Alur 1:1 dgn Form1: Activation Code + Valid Days + Product Purchase Code (machine info)
    /// -> KeySecurity.GenerateKeyActivate -> kunci lisensi.
    /// PENTING: jalan di server/PC developer saja, JANGAN expose publik.
    /// Akses dilindungi token (?token=...) yg diset di App.config.
    /// </summary>
    static class Program
    {
        static string Prefix = "http://127.0.0.1:8741/";
        static string Token = "ganti-token-ini";

        static void Main(string[] args)
        {
            try { if (!string.IsNullOrEmpty(ConfigurationManager.AppSettings["Prefix"])) Prefix = ConfigurationManager.AppSettings["Prefix"]; } catch (Exception) { }
            try { if (!string.IsNullOrEmpty(ConfigurationManager.AppSettings["Token"])) Token = ConfigurationManager.AppSettings["Token"]; } catch (Exception) { }
            if (Token == "ganti-token-ini")
            {
                Console.WriteLine("PERINGATAN: token masih default. Ganti di WaSenderKeyGenWeb.exe.config sebelum dipakai.");
            }
            if (!HttpListener.IsSupported)
            {
                Console.WriteLine("HttpListener tidak didukung di OS ini.");
                return;
            }
            var li = new HttpListener();
            try { li.Prefixes.Add(Prefix); }
            catch (Exception ex)
            {
                Console.WriteLine("Prefix salah (" + Prefix + "): " + ex.Message);
                Console.WriteLine("Untuk non-localhost jalankan sebagai Administrator / daftarkan via netsh http add urlacl.");
                return;
            }
            try { li.Start(); }
            catch (HttpListenerException ex)
            {
                Console.WriteLine("Gagal start (" + ex.ErrorCode + "): " + ex.Message);
                Console.WriteLine("Tips: ganti Prefix ke http://127.0.0.1:PORT/ atau jalankan as Administrator + netsh urlacl.");
                return;
            }
            Console.WriteLine("WaSenderKeyGenWeb jalan di " + Prefix);
            Console.WriteLine("Buka: " + Prefix.TrimEnd('/') + "/?token=" + Token);
            Console.WriteLine("Tekan Ctrl+C untuk berhenti.");
            while (true)
            {
                HttpListenerContext ctx = null;
                try { ctx = li.GetContext(); }
                catch (Exception) { break; }
                try { Handle(ctx); }
                catch (Exception ex) { try { Send(ctx, 500, "text/plain", "ERROR: " + ex.Message); } catch (Exception) { } }
            }
        }

        static bool Authorized(HttpListenerRequest req)
        {
            try { return string.Equals(req.QueryString["token"], Token, StringComparison.Ordinal); }
            catch (Exception) { return false; }
        }

        static void Handle(HttpListenerContext ctx)
        {
            var req = ctx.Request;
            if (!Authorized(req))
            {
                Send(ctx, 403, "text/plain; charset=utf-8", "Forbidden: token salah.");
                return;
            }
            if (req.HttpMethod == "GET")
            {
                Send(ctx, 200, "text/html; charset=utf-8", FormHtml("", "", "", ""));
                return;
            }
            if (req.HttpMethod == "POST" && (req.Url.AbsolutePath == "/" || req.Url.AbsolutePath == "/generate"))
            {
                string body;
                using (var sr = new StreamReader(req.InputStream, req.ContentEncoding ?? Encoding.UTF8)) body = sr.ReadToEnd();
                NameValueCollection f = HttpUtility.ParseQueryString(body ?? "");
                string activation = (f["activation"] ?? "").Trim();
                string daysRaw = (f["days"] ?? "").Trim();
                string machine = (f["machine"] ?? "").Trim();
                if (activation == "" || daysRaw == "" || machine == "")
                {
                    Send(ctx, 200, "text/html; charset=utf-8", FormHtml(activation, daysRaw, machine, "Isi ketiga field (Activation Code, Valid Days, Machine Info)."));
                    return;
                }
                int days;
                if (!int.TryParse(daysRaw, out days) || days <= 0 || days > 3650)
                {
                    Send(ctx, 200, "text/html; charset=utf-8", FormHtml(activation, daysRaw, machine, "Valid Days harus angka 1-3650."));
                    return;
                }
                string key;
                try
                {
                    // Sama persis dgn Form1.button1_Click desktop.
                    key = KeySecurity.KeySecurity.GenerateKeyActivate(activation, days, machine);
                }
                catch (Exception ex)
                {
                    Send(ctx, 200, "text/html; charset=utf-8", FormHtml(activation, daysRaw, machine, "Gagal generate: " + ex.Message));
                    return;
                }
                try { AuditLog(machine, days); } catch (Exception) { }
                Send(ctx, 200, "text/html; charset=utf-8", FormHtml(activation, daysRaw, machine, "", key));
                return;
            }
            Send(ctx, 404, "text/plain; charset=utf-8", "Not found.");
        }

        static void AuditLog(string machine, int days)
        {
            try
            {
                string dir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "issued");
                if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
                string fp = machine.Length > 16 ? machine.Substring(0, 8) + "..." : machine;
                File.AppendAllText(Path.Combine(dir, "web-issued.log"),
                    DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + " days=" + days + " machine=" + fp + Environment.NewLine);
            }
            catch (Exception) { }
        }

        static string H(string s)
        {
            if (s == null) return "";
            return s.Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;").Replace("\"", "&quot;");
        }

        static string FormHtml(string activation, string days, string machine, string notice, string key = "")
        {
            if (days == "") days = "365";
            var sb = new StringBuilder();
            sb.Append("<!DOCTYPE html><html><head><meta charset='utf-8'>");
            sb.Append("<title>WaSender KeyGen (Web - Developer Only)</title>");
            sb.Append("<style>body{font-family:Segoe UI,Arial;margin:30px;max-width:640px;}label{display:block;margin:12px 0 4px;font-weight:bold;}input[type=text]{width:100%;padding:8px;box-sizing:border-box;}textarea{width:100%;height:140px;padding:8px;box-sizing:border-box;}button{margin-top:14px;padding:10px 22px;font-size:15px;} .err{color:#a00;} .ok{color:#070;}</style>");
            sb.Append("</head><body>");
            sb.Append("<h2>WaSender KeyGen — Web (Developer Only)</h2>");
            if (!string.IsNullOrEmpty(notice)) sb.Append("<p class='err'>" + H(notice) + "</p>");
            sb.Append("<form method='post' action='/?token=" + H(Token) + "'>");
            sb.Append("<label>Activation Code</label><input type='text' name='activation' value='" + H(activation) + "'>");
            sb.Append("<label>Valid For (Days)</label><input type='text' name='days' value='" + H(days) + "'>");
            sb.Append("<label>Product Purchase Code / Machine Info</label><input type='text' name='machine' value='" + H(machine) + "'>");
            sb.Append("<br><button type='submit'>Generate Key</button></form>");
            if (!string.IsNullOrEmpty(key))
            {
                sb.Append("<h3 class='ok'>License Key:</h3>");
                sb.Append("<textarea readonly onclick='this.select()'>" + H(key) + "</textarea>");
                sb.Append("<p>Salin kunci di atas dan kirim ke customer.</p>");
            }
            sb.Append("</body></html>");
            return sb.ToString();
        }

        static void Send(HttpListenerContext ctx, int code, string contentType, string body)
        {
            byte[] buf = Encoding.UTF8.GetBytes(body ?? "");
            ctx.Response.StatusCode = code;
            ctx.Response.ContentType = contentType;
            ctx.Response.ContentLength64 = buf.Length;
            ctx.Response.OutputStream.Write(buf, 0, buf.Length);
            ctx.Response.OutputStream.Close();
        }
    }
}

using Microsoft.Web.WebView2.WinForms;
using System;

namespace WASender
{
    /// <summary>
    /// Kebijakan browser: WebView2 satu-satunya jalur WA Web.
    /// WebBrowser IE hanya untuk preview HTML lokal. Selenium/chromedriver hanya untuk scraper lama.
    /// </summary>
    public static class BrowserPolicy
    {
        public const int BrowserNone = 0;
        public const int BrowserSelenium = 1;
        public const int BrowserWebView2 = 2;

        public static bool IsWebView2(int browserType) { return browserType == BrowserWebView2; }

        public static void EnsureWebView2(object wv)
        {
            if (!(wv is WebView2))
                throw new InvalidOperationException("WA Web wajib WebView2. Terima: " + (wv == null ? "null" : wv.GetType().Name));
        }

        public static void KillLegacyDrivers()
        {
            try { _Config.KillChromeDriverProcess(); } catch (Exception) { }
        }

        /// <summary>
        /// Migrasi satu-arah: tulis ulang file settings browserType 0/1 -> 2.
        /// Membuat 100+ cabang `browserType==1` (Selenium) mati total tanpa hapus kode.
        /// </summary>
        public static void EnforceWebView2SettingsFile()
        {
            try
            {
                string p = Config.GetGeneralSettingsFilePath();
                if (string.IsNullOrEmpty(p) || !System.IO.File.Exists(p)) return;
                string json = System.IO.File.ReadAllText(p);
                string fixed_ = System.Text.RegularExpressions.Regex.Replace(
                    json, @"""browserType""\s*:\s*1\b", "\"browserType\": 2");
                fixed_ = System.Text.RegularExpressions.Regex.Replace(
                    fixed_, @"""browserType""\s*:\s*0\b", "\"browserType\": 2");
                if (fixed_ != json) System.IO.File.WriteAllText(p, fixed_);
            }
            catch (Exception) { }
        }

        public static int EffectiveBrowserType(int t) { return (t == 1 || t == 0) ? 2 : t; }
    }
}

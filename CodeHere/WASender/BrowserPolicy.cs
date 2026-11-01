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
    }
}

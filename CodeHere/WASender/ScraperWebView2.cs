using Microsoft.Web.WebView2.WinForms;
using System;
using System.Threading.Tasks;

namespace WASender
{
    /// <summary>
    /// Pengganti ChromeDriver/Selenium untuk scraper (GMap, web, sosmed).
    /// Semua navigasi + ekstraksi via WebView2 ExecuteScriptAsync, tanpa chromedriver.exe.
    /// </summary>
    public static class ScraperWebView2
    {
        public static async Task<string> EvalAsync(WebView2 wv, string js)
        {
            try { return await wv.ExecuteScriptAsync(js); }
            catch (Exception) { return null; }
        }

        public static string Unwrap(string raw)
        {
            try { return WPPHelperUnwrap(raw); }
            catch (Exception) { return raw; }
        }

        private static string WPPHelperUnwrap(string raw)
        {
            if (raw == null) return null;
            string s = raw.Trim();
            if (s.StartsWith("\"") && s.EndsWith("\"") && s.Length >= 2)
            {
                try { return Newtonsoft.Json.JsonConvert.DeserializeObject<string>(s); }
                catch (Exception) { }
                s = s.Substring(1, s.Length - 2);
            }
            return System.Text.RegularExpressions.Regex.Unescape(s);
        }

        public static async Task<string> GetHtmlAsync(WebView2 wv)
        {
            try
            {
                string raw = await EvalAsync(wv, "document.documentElement.outerHTML");
                return Unwrap(raw);
            }
            catch (Exception) { return ""; }
        }

        public static async Task<string> QueryTextAsync(WebView2 wv, string cssSelector)
        {
            try
            {
                string js = "(function(){try{var e=document.querySelector("
                    + Newtonsoft.Json.JsonConvert.SerializeObject(cssSelector)
                    + ");return e?e.innerText:'';}catch(x){return '';}})()";
                return Unwrap(await EvalAsync(wv, js));
            }
            catch (Exception) { return ""; }
        }

        public static async Task<string> QueryAttrAsync(WebView2 wv, string cssSelector, string attr)
        {
            try
            {
                string js = "(function(){try{var e=document.querySelector("
                    + Newtonsoft.Json.JsonConvert.SerializeObject(cssSelector)
                    + ");return e?e.getAttribute("
                    + Newtonsoft.Json.JsonConvert.SerializeObject(attr)
                    + "):'';}catch(x){return '';}})()";
                return Unwrap(await EvalAsync(wv, js));
            }
            catch (Exception) { return ""; }
        }

        public static async Task NavigateAsync(WebView2 wv, string url)
        {
            try
            {
                var tcs = new TaskCompletionSource<bool>();
                EventHandler<Microsoft.Web.WebView2.Core.CoreWebView2NavigationCompletedEventArgs> h = null;
                h = (s, e) => { try { wv.NavigationCompleted -= h; tcs.TrySetResult(true); } catch (Exception) { } };
                wv.NavigationCompleted += h;
                wv.Source = new Uri(url);
                await Task.WhenAny(tcs.Task, Task.Delay(20000));
                try { wv.NavigationCompleted -= h; } catch (Exception) { }
            }
            catch (Exception) { try { wv.Source = new Uri(url); } catch (Exception) { } }
        }
    }
}

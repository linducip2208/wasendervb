using Microsoft.Web.WebView2.WinForms;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace WASender
{
    /// <summary>
    /// Ekstraksi GMap via WebView2 JS (pengganti worker_DoWork Selenium).
    /// Legacy XPath dipertahankan sebagai JS XPath evaluator agar selector lama tetap pakai.
    /// Pemakaian: var items = await GMapWebView2Extractor.ExtractAsync(wv, 20);
    /// </summary>
    public static class GMapWebView2Extractor
    {
        public class GMapItem
        {
            public string Name { get; set; } = "";
            public string Address { get; set; } = "";
            public string Website { get; set; } = "";
            public string Phone { get; set; } = "";
            public string Rating { get; set; } = "";
        }

        private static async Task<string> EvalUnwrap(WebView2 wv, string js)
        {
            try
            {
                string raw = await wv.ExecuteScriptAsync(js);
                if (string.IsNullOrEmpty(raw)) return "";
                try
                {
                    string s = JsonConvert.DeserializeObject<string>(raw);
                    return s ?? "";
                }
                catch (Exception) { return raw.Trim('\"'); }
            }
            catch (Exception) { return ""; }
        }

        private static string XPathTextJs(string xpath)
        {
            string xp = (xpath ?? "").Replace("\\", "\\\\").Replace("'", "\\'");
            return "(function(){try{var r=document.evaluate('" + xp + "',document,null,XPathResult.FIRST_ORDERED_NODE_TYPE,null).singleNodeValue;"
                + "return r?(r.innerText||r.textContent||''):'';}catch(e){return '';}})()";
        }

        public static async Task<List<GMapItem>> ExtractAsync(WebView2 wv, int max = 120)
        {
            var out_ = new List<GMapItem>();
            try
            {
                // 1) Coba pola modern Google Maps (2024+): feed item + panel detail.
                string jsList = "(function(){try{var a=[];"
                    + "var els=document.querySelectorAll('div.Nv2PK, div.tIxNaf, a.hfpxzc, div[role=\"article\"]');"
                    + "for(var i=0;i<els.length && a.length<" + max + ";i++){"
                    + "var t=(els[i].innerText||'').split('\\n');"
                    + "var name=t.length?t[0]:'';"
                    + "if(name)a.push({name:name});}"
                    + "return JSON.stringify(a);}catch(e){return '[]';}})()";
                string raw = await EvalUnwrap(wv, jsList);
                try
                {
                    var arr = JsonConvert.DeserializeObject<List<Dictionary<string, string>>>(raw);
                    if (arr != null)
                        foreach (var d in arr)
                            out_.Add(new GMapItem { Name = d.ContainsKey("name") ? d["name"] : "" });
                }
                catch (Exception) { }

                // 2) Fallback XPath lama untuk panel detail yang terbuka.
                if (out_.Count == 0)
                {
                    string name = await EvalUnwrap(wv, XPathTextJs(XPathStore.GMap_Heading));
                    if (string.IsNullOrEmpty(name)) name = await EvalUnwrap(wv, XPathTextJs(XPathStore.GMap_Header));
                    if (!string.IsNullOrEmpty(name))
                    {
                        out_.Add(new GMapItem
                        {
                            Name = name,
                            Address = await EvalUnwrap(wv, XPathTextJs(XPathStore.GMap_Address)),
                            Website = await EvalUnwrap(wv, "(function(){try{var e=document.evaluate(\""
                                + XPathStore.GMap_WebSite.Replace("\"", "\\\"")
                                + "\",document,null,XPathResult.FIRST_ORDERED_NODE_TYPE,null).singleNodeValue;"
                                + "return e?e.href:'';}catch(x){return '';}})()"),
                            Rating = await EvalUnwrap(wv, XPathTextJs(XPathStore.GMap_Rating)),
                        });
                    }
                }
            }
            catch (Exception) { }
            return out_;
        }
        /// <summary>Scroll bertahap otomatis (12 langkah + event scroll) agar lazy-load Google terpancing.
        /// Sekali lompat ke bawah sering diabaikan — makanya kemarin harus scroll manual.</summary>
        public static async Task<bool> ScrollResultsAsync(WebView2 wv)
        {
            try
            {
                long lastH = 0;
                int still = 0;
                for (int i = 0; i < 12; i++)
                {
                    string js = "(function(){try{"
                        + "var c=document.querySelector('div[role=\"feed\"]')||document.querySelector('div.m6QErb')||document.querySelector('#search')||document.querySelector('#rso')||document.querySelector('div[role=\"main\"]')||document.scrollingElement||document.documentElement;"
                        + "if(!c)return 'NOFEED';"
                        + "var h=c.scrollHeight;"
                        + "c.scrollTop=c.scrollTop+900;"
                        + "try{c.dispatchEvent(new Event('scroll',{bubbles:true}));}catch(x){}"
                        + "try{window.scrollBy(0,900);}catch(x){}"
                        + "return 'H:'+h;"
                        + "}catch(e){return 'ERR';}})()";
                    string r = await EvalUnwrap(wv, js);
                    long h = 0;
                    try { h = long.Parse((r ?? "").Replace("H:", "")); } catch (Exception) { }
                    if (h > 0) { if (h <= lastH) still++; else { still = 0; lastH = h; } }
                    if (still >= 3) break; // tinggi tak bertambah 3x = mentok
                    await Task.Delay(900);
                }
                try { await EvalUnwrap(wv, "(function(){try{var c=document.querySelector('div[role=\"feed\"]')||document.querySelector('div.m6QErb')||document.scrollingElement;if(c)c.scrollTop=c.scrollHeight;window.scrollTo(0,document.body?document.body.scrollHeight:999999);return 'OK';}catch(e){return 'ERR';}})()"); }
                catch (Exception) { }
                await Task.Delay(1500);
                return true;
            }
            catch (Exception) { return true; }
        }

        /// <summary>Klik halaman berikutnya (paginator lama maupun tombol Next/Berikutnya baru). Return true bila diklik.</summary>
        public static async Task<bool> ClickNextAsync(WebView2 wv)
        {
            try
            {
                string js = "(function(){try{"
                    + "function vis(e){if(!e)return false;var r=e.getBoundingClientRect();return r.width>0&&r.height>0;}"
                    + "function click(e){try{e.scrollIntoView({block:'center'});}catch(x){}try{e.click();return true;}catch(x){return false;}}"
                    + "var sels=['a#pnnext','[aria-label=\"Next\"]','[aria-label=\"Halaman berikutnya\"]','[aria-label=\"Berikutnya\"]','button[aria-label*=\"Next\"]','a[aria-label*=\"Next\"]'];"
                    + "for(var i=0;i<sels.length;i++){try{var e=document.querySelector(sels[i]);if(e&&vis(e)&&click(e))return 'CLICKED:'+sels[i];}catch(x){}}"
                    + "var els=document.querySelectorAll('a,button');"
                    + "for(var i=0;i<els.length;i++){try{var e=els[i];var t=((e.innerText||'')+' '+(e.getAttribute('aria-label')||'')).toLowerCase();"
                    + "if((t.indexOf('berikutnya')>-1||(t.indexOf('next')>-1&&t.length<40))&&vis(e)&&click(e))return 'CLICKED:text';}catch(x){}}"
                    + "return 'NO_NEXT';"
                    + "}catch(e){return 'ERR';}})()";
                string before = "";
                try { before = await EvalUnwrap(wv, "location.href"); } catch (Exception) { }
                string r = await EvalUnwrap(wv, js);
                if (r == null || !r.StartsWith("CLICKED")) return false;
                await Task.Delay(4000);
                string after = "";
                try { after = await EvalUnwrap(wv, "location.href"); } catch (Exception) { }
                return !string.Equals(before, after) || r.Length > 0;
            }
            catch (Exception) { return false; }
        }
    }
}

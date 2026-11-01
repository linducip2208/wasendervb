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

        public static async Task<List<GMapItem>> ExtractAsync(WebView2 wv, int max = 30)
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
    }
}

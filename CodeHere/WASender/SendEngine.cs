using Microsoft.Web.WebView2.WinForms;
using Newtonsoft.Json;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace WASender
{
    /// <summary>
    /// Mesin kirim buatan sendiri (pengganti fungsi kirim DLL vendor yg mati).
    /// Prinsip: buka chat via link resmi -> kirim -> VERIFIKASI outbox bertambah.
    /// True HANYA jika pesan benar masuk chat (pasti sinkron ke HP).
    /// </summary>
    public static class SendEngine
    {
        public static void Log(string msg)
        {
            try
            {
                string dir = Config.GetProcessLoggerFolderPath();
                System.IO.File.AppendAllText(System.IO.Path.Combine(dir, "single_send.log"),
                    System.DateTime.Now.ToString("HH:mm:ss") + " [SE] " + msg + System.Environment.NewLine);
            }
            catch (Exception) { }
        }

        private static async Task<string> Eval(WebView2 wv, string script)
        {
            try { return await wv.ExecuteScriptAsync(script); }
            catch (Exception ex) { return "EX:" + ex.GetType().Name; }
        }

        public static async Task<int> OutboxCount(WebView2 wv)
        {
            try
            {
                string r = await Eval(wv, "document.querySelectorAll('#main div.message-out').length");
                if (r == null) { return -1; }
                int n;
                if (int.TryParse(r.Trim().Trim('"'), out n)) { return n; }
            }
            catch (Exception) { }
            return -1;
        }

        public static async Task<bool> OutboxIncreased(WebView2 wv, int before, int seconds = 12)
        {
            try
            {
                for (int i = 0; i < seconds / 2; i++)
                {
                    await Task.Delay(2000);
                    int after = await OutboxCount(wv);
                    if (after >= 0 && before >= 0 && after > before) { return true; }
                }
            }
            catch (Exception ex) { Log("outbox EX " + ex.Message); }
            return false;
        }

        private static async Task<bool> EnsureFmark(WebView2 wv)
        {
            try
            {
                string has = await Eval(wv, "typeof window.FMARK === 'object' ? 'yes' : 'no'");
                if (has != null && has.Contains("yes")) { return true; }
                string path = System.IO.Path.Combine(Config.GetSysFolderPath(), "wapi.js");
                if (!System.IO.File.Exists(path)) { return false; }
                await wv.ExecuteScriptAsync(System.IO.File.ReadAllText(path));
                await Task.Delay(3000);
                has = await Eval(wv, "typeof window.FMARK === 'object' ? 'yes' : 'no'");
                return has != null && has.Contains("yes");
            }
            catch (Exception ex) { Log("ensureFmark EX " + ex.Message); return false; }
        }

        private static async Task<string> PollMailbox(WebView2 wv, string box, int tries = 10)
        {
            try
            {
                for (int i = 0; i < tries; i++)
                {
                    await Task.Delay(1500);
                    string cur = await Eval(wv, "window." + box);
                    if (cur == null) { continue; }
                    string v = cur.Trim().Trim('"');
                    try { v = System.Text.RegularExpressions.Regex.Unescape(v); } catch (Exception) { }
                    if (v.StartsWith("\"") && v.EndsWith("\"") && v.Length > 2) { v = v.Substring(1, v.Length - 2); }
                    if (v != "PENDING" && v != "") { return v; }
                }
            }
            catch (Exception ex) { Log("poll EX " + ex.Message); }
            return "TIMEOUT";
        }

        /// <summary>Buka chat via link resmi. Return OK | NOTONWA | LOGGEDOUT | FAIL</summary>
        public static async Task<string> OpenChat(WebView2 wv, string digits)
        {
            try
            {
                string alive = await Eval(wv, "('ALIVE:' + location.href).substring(0,120)");
                Log("openchat pre=" + (alive ?? "null"));
                await wv.ExecuteScriptAsync("window.location.href = " + JsonConvert.SerializeObject("https://web.whatsapp.com/send?phone=" + digits) + "; 'NAV';");
                for (int i = 0; i < 15; i++)
                {
                    await Task.Delay(2000);
                    string st = await Eval(wv,
                        "try{"
                        + "var box=document.querySelector('#main footer [contenteditable=\"true\"]')||document.querySelector('[data-lexical-editor]');"
                        + "if(box) return 'CHAT';"
                        + "if(document.querySelector('canvas[aria-label=\"Scan me!\"]')||document.querySelector('canvas[aria-label^=\"Scan\"]')) return 'LOGGEDOUT';"
                        + "var t=(document.body&&document.body.innerText||'').substring(0,2000);"
                        + "if(/not on WhatsApp|invalid phone|tidak valid|tidak terdaftar|tidak ada di WhatsApp/i.test(t)) return 'NOTONWA';"
                        + "if(/to use WhatsApp on your computer|scan.*qr|pindai.*qr/i.test(t)) return 'LOGGEDOUT';"
                        + "return 'WAIT:' + (document.title||'').substring(0,40);"
                        + "}catch(e){return 'ERR';}");
                    string s = ((st ?? "").Trim().Trim('"') ?? "");
                    if (s.StartsWith("EX:")) { Log("openchat poll EX " + s); continue; }
                    if (i % 4 == 0) { Log("openchat poll " + i + "=" + (s.Length > 60 ? s.Substring(0, 60) : s)); }
                    if (s.StartsWith("CHAT")) { return "OK"; }
                    if (s.StartsWith("NOTONWA")) { return "NOTONWA"; }
                    if (s.StartsWith("LOGGEDOUT")) { return "LOGGEDOUT"; }
                }
                return "FAIL";
            }
            catch (Exception ex) { Log("openchat EX " + ex.Message); return "FAIL"; }
        }

        /// <summary>Kirim teks via DOM UI resmi (chat harus sudah terbuka).</summary>
        public static async Task<bool> SendTextDom(WebView2 wv, string text)
        {
            try
            {
                string setJs = "(function(){"
                    + "var box=document.querySelector('#main footer [contenteditable=\"true\"]')||document.querySelector('[data-lexical-editor] [contenteditable=\"true\"]')||document.querySelector('[data-lexical-editor]')||document.querySelector('[contenteditable=\"true\"]');"
                    + "if(!box) return 'NOBOX';"
                    + "try{box.scrollIntoView({block:'center'});}catch(e){}"
                    + "box.focus();"
                    + "try{document.execCommand('selectAll',false,null);}catch(e){}"
                    + "var T=" + JsonConvert.SerializeObject(text ?? "") + ";"
                    + "var ok=false;"
                    + "try{ ok=document.execCommand('insertText',false,T); }catch(e){}"
                    + "if(!ok){ try{"
                    + "var bi=new InputEvent('beforeinput',{bubbles:true,cancelable:true,inputType:'insertText',data:T});"
                    + "box.dispatchEvent(bi);"
                    + "if(bi.defaultPrevented){ ok=true; }"
                    + "else { box.textContent=T; box.dispatchEvent(new InputEvent('input',{bubbles:true,inputType:'insertText',data:T})); box.dispatchEvent(new Event('change',{bubbles:true})); }"
                    + "}catch(e){} }"
                    + "var cur=(box.textContent||'');"
                    + "return (cur && cur.trim().length>0)?'SET:'+cur.substring(0,40):'EMPTY';"
                    + "})();";
                string setRes = await Eval(wv, setJs);
                Log("dom set -> " + (setRes ?? "null"));
                if (setRes == null || !setRes.Contains("SET:")) { return false; }
                await Task.Delay(1000);
                string clickJs = "(function(){"
                    + "var sels=['#main footer button[aria-label=\"Send\"]','footer button[aria-label=\"Send\"]','button span[data-icon=\"send\"]','span[data-icon=\"send\"]','button[data-tab=\"11\"]'];"
                    + "for(var i=0;i<sels.length;i++){try{var b=document.querySelector(sels[i]);if(b){var btn=b.tagName==='BUTTON'?b:(b.closest('button')||b);btn.click();return 'CLICKED:'+sels[i];}}catch(e){}}"
                    + "try{var box2=document.querySelector('#main footer [contenteditable=\"true\"]')||document.querySelector('[contenteditable=\"true\"]');if(box2){box2.focus();box2.dispatchEvent(new KeyboardEvent('keydown',{key:'Enter',keyCode:13,which:13,bubbles:true}));return 'ENTER';}}catch(e){}"
                    + "return 'NOBTN';"
                    + "})();";
                string clickRes = await Eval(wv, clickJs);
                Log("dom click -> " + (clickRes ?? "null"));
                return clickRes != null && (clickRes.Contains("CLICKED") || clickRes.Contains("ENTER"));
            }
            catch (Exception ex) { Log("domtext EX " + ex.Message); return false; }
        }

        /// <summary>Kirim teks via FMARK internal. Return tuple (ok, detail).</summary>
        public static async Task<Tuple<bool, string>> SendTextFmark(WebView2 wv, string digits, string text)
        {
            try
            {
                if (!await EnsureFmark(wv)) { return Tuple.Create(false, "nofmark"); }
                string chatIdJs = JsonConvert.SerializeObject(digits + "@c.us");
                string chkJs = "window.__fmarkExists='PENDING';"
                    + "FMARK.queryExists(" + chatIdJs + ")"
                    + ".then(function(r){window.__fmarkExists='EXISTS:'+JSON.stringify(r).substring(0,160);})"
                    + ".catch(function(e){window.__fmarkExists='CHECKFAIL:'+((e&&e.message)||e);});'DISPATCHED';";
                await Eval(wv, chkJs);
                string chk = await PollMailbox(wv, "__fmarkExists");
                Log("fmark exists=" + chk);
                if (chk == "false" || chk == "null" || chk == "EXISTS:false" || chk == "EXISTS:null") { return Tuple.Create(false, "notonwa"); }
                string js = "window.__fmarkText='PENDING';"
                    + "FMARK.sendTextMessage(" + chatIdJs + "," + JsonConvert.SerializeObject(text ?? "")
                    + ").then(function(r){window.__fmarkText='OK:'+JSON.stringify(r).substring(0,300);})"
                    + ".catch(function(e){window.__fmarkText='FAIL:'+((e&&e.message)||e);});'DISPATCHED';";
                await Eval(wv, js);
                string res = await PollMailbox(wv, "__fmarkText", 14);
                Log("fmark text -> " + res);
                return Tuple.Create(res.StartsWith("OK:{"), res);
            }
            catch (Exception ex) { Log("fmarktext EX " + ex.Message); return Tuple.Create(false, "ex"); }
        }

        /// <summary>Kirim 1 file via FMARK internal.</summary>
        public static async Task<Tuple<bool, string>> SendMediaFmark(WebView2 wv, string digits, string filePath, string caption)
        {
            try
            {
                if (!System.IO.File.Exists(filePath)) { return Tuple.Create(false, "nofile"); }
                if (!await EnsureFmark(wv)) { return Tuple.Create(false, "nofmark"); }
                byte[] bytes = System.IO.File.ReadAllBytes(filePath);
                string ext = (System.IO.Path.GetExtension(filePath) ?? "").ToLower();
                string ct = System.Web.MimeMapping.GetMimeMapping(filePath);
                if (ext == ".mp4") { ct = "video/mp4"; }
                string b64 = "data:" + ct + ";base64," + Convert.ToBase64String(bytes);
                string chatIdJs = JsonConvert.SerializeObject(digits + "@c.us");
                string js = "window.__fmarkMedia='PENDING';"
                    + "FMARK.sendFileMessage(" + chatIdJs + "," + JsonConvert.SerializeObject(b64)
                    + ",{filename:" + JsonConvert.SerializeObject(System.IO.Path.GetFileName(filePath))
                    + ",caption:" + JsonConvert.SerializeObject(caption ?? "") + "})"
                    + ".then(function(r){window.__fmarkMedia='OK:'+JSON.stringify(r).substring(0,300);})"
                    + ".catch(function(e){window.__fmarkMedia='FAIL:'+((e&&e.message)||e);});'DISPATCHED';";
                await Eval(wv, js);
                string res = await PollMailbox(wv, "__fmarkMedia", 16);
                Log("fmark media -> " + (res ?? "").Substring(0, Math.Min(160, (res ?? "").Length)));
                return Tuple.Create(res.StartsWith("OK:{"), res);
            }
            catch (Exception ex) { Log("fmarkmedia EX " + ex.Message); return Tuple.Create(false, "ex"); }
        }
    }
}

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

        private static async Task<bool> EnsureWpp(WebView2 wv)
        {
            try
            {
                string has = await Eval(wv, "typeof window.WPP === 'object' && window.WPP.chat ? 'yes' : 'no'");
                if (has != null && has.Contains("yes")) { return true; }
                Log("WPP missing, injecting wppconnect-wa.js");
                string[] cands = new string[]
                {
                    System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "wppconnect-wa.js"),
                    System.IO.Path.Combine(Config.GetSysFolderPath(), "wppconnect-wa.js")
                };
                foreach (string p in cands)
                {
                    try
                    {
                        if (System.IO.File.Exists(p))
                        {
                            await wv.ExecuteScriptAsync(System.IO.File.ReadAllText(p));
                            await Task.Delay(3000);
                            has = await Eval(wv, "typeof window.WPP === 'object' && window.WPP.chat ? 'yes' : 'no'");
                            if (has != null && has.Contains("yes")) { return true; }
                        }
                    }
                    catch (Exception ex) { Log("WPP inject EX " + ex.Message); }
                }
                return false;
            }
            catch (Exception ex) { Log("WPP ensure EX " + ex.Message); return false; }
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

        /// <summary>Buka chat via kotak SEARCH (tanpa navigasi/reload - anti hang). Return OK|NOTFOUND|FAIL.</summary>
        public static async Task<string> OpenChatViaSearch(WebView2 wv, string digits)
        {
            try
            {
                string clean = System.Text.RegularExpressions.Regex.Replace(digits ?? "", "[^0-9]", "");
                if (string.IsNullOrEmpty(clean)) { return "FAIL"; }
                string findBox = "(function(){"
                    + "var sels=['#side div[contenteditable=\"true\"][data-tab=\"3\"]','#side [contenteditable=\"true\"]','#side input[type=\"text\"]','#side input'];"
                    + "for(var i=0;i<sels.length;i++){try{var b=document.querySelector(sels[i]);if(b){b.scrollIntoView({block:'center'});b.focus();return 'BOX';}}catch(e){}}"
                    + "return 'NOBOX';})();";
                string found = await Eval(wv, findBox);
                Log("search box -> " + (found ?? "null"));
                try
                {
                    string domState = await Eval(wv, "(function(){var r={};r.side=!!document.querySelector('#side');r.paneside=!!document.querySelector('#pane-side');r.sideKids=document.querySelector('#side')?document.querySelector('#side').querySelectorAll('*').length:0;r.cells=document.querySelectorAll('[data-testid=\"cell-frame-container\"]').length;r.opts=document.querySelectorAll('[role=\"option\"]').length;r.inputs=Array.from(document.querySelectorAll('input')).map(function(e){return (e.type||'')+':'+((e.getAttribute('aria-label')||e.name||e.id||e.className)||'').toString().substring(0,30);}).join('|');r.cedit=document.querySelectorAll('[contenteditable=\"true\"]').length;r.title=document.title;return JSON.stringify(r);})();");
                    Log("domstate=" + (domState ?? "null"));
                }
                catch (Exception exD) { Log("domstate EX " + exD.Message); }
                string foundClean = ((found ?? "").Trim().Trim('"') ?? "");
                if (foundClean != "BOX") { return "FAIL"; }
                string typeJs = "(function(){var q=" + JsonConvert.SerializeObject(clean) + ";"
                    + "var box=document.querySelector('#side div[contenteditable=\"true\"][data-tab=\"3\"]')||document.querySelector('#side [contenteditable=\"true\"]')||document.querySelector('#side input');"
                    + "if(!box) return 'NOBOX2';box.focus();"
                    + "try{document.execCommand('selectAll',false,null);}catch(e){try{box.select();}catch(e2){}}"
                    + "var ok=false;try{ok=document.execCommand('insertText',false,q);}catch(e){}"
                    + "if(!ok){try{if(box.tagName==='INPUT'){box.value=q;}else{box.textContent=q;}box.dispatchEvent(new InputEvent('input',{bubbles:true}));box.dispatchEvent(new Event('change',{bubbles:true}));}catch(e){}}"
                    + "var cur=(box.value!==undefined&&box.value!==null&&box.value!=='')?box.value:(box.textContent||'');"
                    + "return (cur&&cur.trim().length>0)?'TYPED:'+cur.substring(0,40):'NOTYPED';})();";
                string typed = await Eval(wv, typeJs);
                Log("search type -> " + (typed ?? "null"));
                string typedClean = ((typed ?? "").Trim().Trim('"') ?? "");
                if (!typedClean.StartsWith("TYPED:")) { await ClearSearch(wv); return "FAIL"; }
                await Task.Delay(3500);
                string clickJs = "(function(){var q=" + JsonConvert.SerializeObject(clean) + ";"
                    + "function txt(el){return ((el.innerText||el.textContent)||'');}"
                    + "var opts=Array.from(document.querySelectorAll('#side [role=\"option\"]'));"
                    + "if(!opts.length){opts=Array.from(document.querySelectorAll('#side div[data-testid=\"cell-frame-container\"]'));} "
                    + "var num=q.replace(/\\D/g,'');"
                    + "var all=opts.slice(0,5).map(function(o){return txt(o).substring(0,60);}).join(' || ');"
                    + "for(var i=0;i<opts.length;i++){try{var t=txt(opts[i]).replace(/\\D/g,'');"
                    + "if(t&&num&&(t.indexOf(num)>-1||num.indexOf(t)>-1)){opts[i].click();return 'CLICKED:'+i+'/'+opts.length;}}catch(e){}}"
                    + "return 'NORESULT opts='+opts.length+':'+all.substring(0,300);})();";
                string clicked = await Eval(wv, clickJs);
                Log("search click -> " + (clicked ?? "null"));
                string clickedClean = ((clicked ?? "").Trim().Trim('"') ?? "");
                if (!clickedClean.StartsWith("CLICKED:"))
                {
                    // Tidak ada opsi cocok: coba Enter (buka hasil pertama) + catat isi hasil utk diagnosis.
                    try
                    {
                        string dbg = await Eval(wv, "(function(){var els=document.querySelectorAll('#side [role=\"option\"], #side div[data-testid=\"cell-frame-container\"]');var o=[];for(var i=0;i<Math.min(3,els.length);i++){o.push(((els[i].innerText||els[i].textContent)||'').substring(0,60));}return 'N='+els.length+':'+o.join(' || ');})();");
                        Log("search options -> " + (dbg ?? "null"));
                        await Eval(wv, "(function(){var box=document.querySelector('#side input[type=\"text\"]')||document.querySelector('#side [contenteditable=\"true\"]');if(box){box.focus();var e=new KeyboardEvent('keydown',{key:'Enter',keyCode:13,which:13,bubbles:true});box.dispatchEvent(e);}return 'ENTER';})();");
                        await Task.Delay(3000);
                    }
                    catch (Exception exE) { Log("search enter EX " + exE.Message); }
                }
                // Verifikasi via header chat (berlaku utk klik cocok MAUPUN Enter).
                // JANGAN klaim OK bila header tidak cocok nomor (hindari kirim ke chat salah).
                for (int i = 0; i < 5; i++)
                {
                    await Task.Delay(2000);
                    string st = await Eval(wv, "(function(){var q=" + JsonConvert.SerializeObject(clean) + ";"
                        + "var h=document.querySelector('#main header');var ht=h?((h.innerText||h.textContent)||''):'';"
                        + "var box=document.querySelector('#main footer [contenteditable=\"true\"]')||document.querySelector('[data-lexical-editor]');"
                        + "var hd=ht.replace(/\\D/g,'');"
                        + "if(box&&hd&&(hd.indexOf(q)>-1||(q.indexOf(hd)>-1&&hd.length>5)))return 'OK:'+ht.substring(0,40);"
                        + "return box?'OPEN-OTHER':'WAIT';})();");
                    string s = ((st ?? "").Trim().Trim('"') ?? "");
                    if (s == "null" || s == "") { continue; }
                    Log("search verify=" + s.Substring(0, System.Math.Min(50, s.Length)));
                    if (s.StartsWith("OK:")) { await ClearSearch(wv); return "OK"; }
                }
                await ClearSearch(wv);
                // Header tidak cocok nomor -> JANGAN klaim OK (hindari kirim ke chat salah).
                return "NOTFOUND";
            }
            catch (Exception ex) { Log("search EX " + ex.Message); return "FAIL"; }
        }

        private static async Task ClearSearch(WebView2 wv)
        {
            try
            {
                await Eval(wv, "(function(){var box=document.querySelector('#side div[contenteditable=\"true\"][data-tab=\"3\"]')||document.querySelector('#side [contenteditable=\"true\"]');if(!box)return 'NOBOX';box.focus();try{document.execCommand('selectAll',false,null);document.execCommand('delete',false,null);}catch(e){box.textContent='';box.dispatchEvent(new InputEvent('input',{bubbles:true}));}box.blur();return 'CLEARED';})();");
            }
            catch (Exception) { }
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

        /// <summary>Resolve chatId via WPP (LID-aware). Return JID string atau digits@c.us.</summary>
    public static async Task<string> ResolveChatId(WebView2 wv, string digits)
    {
        try
        {
            if (!await EnsureWpp(wv)) { return digits + "@c.us"; }
            string chatIdJs = JsonConvert.SerializeObject(digits + "@c.us");
            string js = "window.__resolve='PENDING';"
                + "(function(){try{"
                + "if(WPP.contact&&WPP.contact.queryExists){WPP.contact.queryExists(" + chatIdJs + ").then(function(r){try{var w=r&&r.wid?r.wid:(r&&r.id?r.id:r);var s=(w&&(w._serialized||w.toString?w.toString():String(w)))||JSON.stringify(r);window.__resolve='OK:'+s;}catch(e){window.__resolve='OK:'+JSON.stringify(r);}}).catch(function(e){window.__resolve='FAIL:'+(e&&e.message);});}else{window.__resolve='NOAPI';}"
                + "}catch(e){window.__resolve='EX:'+e.message;}})();'DISPATCHED';";
            await Eval(wv, js);
            string res = await PollMailbox(wv, "__resolve", 6);
            Log("resolve=" + res);
            if (res.StartsWith("OK:"))
            {
                string v = res.Substring(3);
                if (v.Contains("@lid") || v.Contains("@c.us") || v.Contains("@g.us")) { return v.Trim().Trim('"'); }
            }
            return digits + "@c.us";
        }
        catch (Exception ex) { Log("resolve EX " + ex.Message); return digits + "@c.us"; }
    }

    /// <summary>Verifikasi TERKIRIM via API resmi WPP (baca pesan terakhir di chat).</summary>
        public static async Task<Tuple<bool, string>> ConfirmDelivered(WebView2 wv, string digits, string text)
        {
            try
            {
                string chatId = await ResolveChatId(wv, digits);
                string chatIdJs = JsonConvert.SerializeObject(chatId);
                string snippet = JsonConvert.SerializeObject((text ?? "").Length > 20 ? text.Substring(0, 20) : (text ?? ""));
                string js = "window.__verify='PENDING';"
                    + "WPP.chat.getMessages(" + chatIdJs + ",{count:10})"
                    + ".then(function(ms){try{"
                    + "var list=(ms&&ms.map)?ms:((ms&&ms.models)?ms.models:[]);"
                    + "for(var i=list.length-1;i>=0;i--){var m=list[i]||{};var body='';try{body=m.body||m.caption||'';}catch(e){}"
                    + "var ack=0;try{ack=m.ack||0;}catch(e){}"
                    + "var fromme=false;try{fromme=!!(m.fromMe||(m.id&&m.id.fromMe));}catch(e){}"
                    + "if(body&&body.indexOf(" + snippet + ")>-1&&fromme){window.__verify='FOUND ack='+ack;return;}}"
                    + "window.__verify='NOTFOUND n='+list.length;"
                    + "}catch(e){window.__verify='ERR:'+e.message;}})"
                    + ".catch(function(e){window.__verify='FAIL:'+(e&&e.message);});'DISPATCHED';";
                await Eval(wv, js);
                string res = await PollMailbox(wv, "__verify", 8);
                Log("confirm -> " + res);
                if (res.StartsWith("FOUND"))
                {
                    int ack = 0;
                    try { ack = int.Parse(new string(res.SkipWhile(c => !char.IsDigit(c)).TakeWhile(char.IsDigit).ToArray())); } catch (Exception) { }
                    return Tuple.Create(ack >= 1, res);
                }
                return Tuple.Create(false, res);
            }
            catch (Exception ex) { Log("confirm EX " + ex.Message); return Tuple.Create(false, "ex"); }
        }

        /// <summary>Kirim teks via WPP (wa-js terbaru, ganti FMARK yg basi). Return (ok, detail).</summary>
        public static async Task<Tuple<bool, string>> SendTextWpp(WebView2 wv, string digits, string text)
        {
            try
            {
                if (!await EnsureWpp(wv)) { return Tuple.Create(false, "nowpp"); }
                string chatIdJs = JsonConvert.SerializeObject(digits + "@c.us");
                await Eval(wv, "try{WPP.chat.openChatBottom(" + chatIdJs + ").catch(function(){});}catch(e){}");
                await Task.Delay(1500);
                string js = "window.__wppText='PENDING';"
                    + "WPP.chat.sendTextMessage(" + chatIdJs + "," + JsonConvert.SerializeObject(text ?? "")
                    + ").then(function(r){window.__wppText='OK:'+JSON.stringify(r).substring(0,300);})"
                    + ".catch(function(e){window.__wppText='FAIL:'+((e&&e.message)||e);});'DISPATCHED';";
                await Eval(wv, js);
                string res = await PollMailbox(wv, "__wppText", 16);
                Log("wpp text -> " + res);
                if (!res.StartsWith("OK:{")) { return Tuple.Create(false, res); }
                var cf = await ConfirmDelivered(wv, digits, text);
                Log("wpp confirm=" + cf.Item1);
                return Tuple.Create(cf.Item1, res + " | " + cf.Item2);
            }
            catch (Exception ex) { Log("wpptext EX " + ex.Message); return Tuple.Create(false, "ex"); }
        }

        /// <summary>Kirim 1 file via WPP (wa-js terbaru).</summary>
        public static async Task<Tuple<bool, string>> SendMediaWpp(WebView2 wv, string digits, string filePath, string caption)
        {
            try
            {
                if (!System.IO.File.Exists(filePath)) { return Tuple.Create(false, "nofile"); }
                if (!await EnsureWpp(wv)) { return Tuple.Create(false, "nowpp"); }
                byte[] bytes = System.IO.File.ReadAllBytes(filePath);
                string ext = (System.IO.Path.GetExtension(filePath) ?? "").ToLower();
                string ct = System.Web.MimeMapping.GetMimeMapping(filePath);
                if (ext == ".mp4") { ct = "video/mp4"; }
                string b64 = "data:" + ct + ";base64," + System.Convert.ToBase64String(bytes);
                string chatIdJs = JsonConvert.SerializeObject(digits + "@c.us");
                await Eval(wv, "try{WPP.chat.openChatBottom(" + chatIdJs + ").catch(function(){});}catch(e){}");
                await Task.Delay(1500);
                string js = "window.__wppMedia='PENDING';"
                    + "WPP.chat.sendFileMessage(" + chatIdJs + "," + JsonConvert.SerializeObject(b64)
                    + ",{filename:" + JsonConvert.SerializeObject(System.IO.Path.GetFileName(filePath))
                    + ",caption:" + JsonConvert.SerializeObject(caption ?? "") + "})"
                    + ".then(function(r){window.__wppMedia='OK:'+JSON.stringify(r).substring(0,300);})"
                    + ".catch(function(e){window.__wppMedia='FAIL:'+((e&&e.message)||e);});'DISPATCHED';";
                await Eval(wv, js);
                string res = await PollMailbox(wv, "__wppMedia", 20);
                Log("wpp media -> " + (res ?? "").Substring(0, System.Math.Min(160, (res ?? "").Length)));
                if (!res.StartsWith("OK:{")) { return Tuple.Create(false, res); }
                var cfm = await ConfirmDelivered(wv, digits, caption);
                Log("wpp media confirm=" + cfm.Item1);
                return Tuple.Create(cfm.Item1, res + " | " + cfm.Item2);
            }
            catch (Exception ex) { Log("wppmedia EX " + ex.Message); return Tuple.Create(false, "ex"); }
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

using Microsoft.Web.WebView2.WinForms;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace WASender
{
    /// <summary>
    /// Pengganti WPPHelper.dll (vendor tutup, tidak kompatibel dgn WA Web saat ini).
    /// Implementasi memakai WPP wa-js yg masih dirawat (wppconnect-wa.js) + verifikasi DOM.
    /// UI tidak diubah; semua pemanggil lama tetap kompil tanpa perubahan.
    /// </summary>
    public static partial class WPPHelper
    {
        public static string GroupJoinFaildReson { get; set; }

        #region ===== eksekusi JS =====
        internal static async Task<string> Eval(WebView2 wv, string script)
        {
            try { return await wv.ExecuteScriptAsync(script); }
            catch (Exception) { return null; }
        }

        internal static string Unwrap(string raw)
        {
            if (raw == null) return null;
            string s = raw.Trim();
            if (s.StartsWith("\"") && s.EndsWith("\"") && s.Length >= 2)
            {
                try { return JsonConvert.DeserializeObject<string>(s); } catch (Exception) { }
                s = s.Substring(1, s.Length - 2);
            }
            return System.Text.RegularExpressions.Regex.Unescape(s);
        }

        internal static async Task<bool> EnsureWpp(WebView2 wv)
        {
            try
            {
                string has = await Eval(wv, "typeof window.WPP === 'object' && window.WPP.chat ? 'yes' : 'no'");
                if (has != null && has.Contains("yes")) return true;
                string[] cands = new string[]
                {
                    System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "wppconnect-wa.js"),
                    System.IO.Path.Combine(Config.GetSysFolderPath(), "wppconnect-wa.js"),
                    System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "wapi_gmaps.js")
                };
                foreach (string p in cands)
                {
                    try
                    {
                        if (!System.IO.File.Exists(p)) continue;
                        await wv.ExecuteScriptAsync(System.IO.File.ReadAllText(p));
                        await Task.Delay(2500);
                        has = await Eval(wv, "typeof window.WPP === 'object' && window.WPP.chat ? 'yes' : 'no'");
                        if (has != null && has.Contains("yes")) return true;
                    }
                    catch (Exception) { }
                }
                return false;
            }
            catch (Exception) { return false; }
        }

        internal static async Task<string> Mailbox(WebView2 wv, string name, int tries = 8)
        {
            try
            {
                for (int i = 0; i < tries; i++)
                {
                    await Task.Delay(1500);
                    string cur = await Eval(wv, "window." + name);
                    if (cur == null) continue;
                    string v = Unwrap(cur);
                    if (v != null && v != "" && v != "PENDING" && v != "null") return v;
                }
            }
            catch (Exception) { }
            return "TIMEOUT";
        }
        #endregion

        #region ===== lisensi & init =====
        public static void CheckExecutingAssembly()
        {
            // Pengganti cek lisensi DLL vendor: pakai lisensi lokal (KeySecurity).
            // Melempar jika tidak aktif, sama seperti perilaku lama (return true = aktif).
            try
            {
                if (!Config.IsProductActive())
                {
                    throw new Exception("Product is not activated");
                }
            }
            catch (Exception)
            {
                throw;
            }
        }

        public static async Task<bool> isWPPinjected(WebView2 wv)
        {
            try
            {
                string r = await Eval(wv, "typeof window.WPP === 'object' ? 'yes' : (typeof window.FMARK === 'object' ? 'fmark' : 'no')");
                return r != null && (r.Contains("yes") || r.Contains("fmark"));
            }
            catch (Exception) { return false; }
        }

        public static async Task<bool> isWPPinjectedAsync(WebView2 wv)
        {
            return await isWPPinjected(wv);
        }

        public static async Task<string> InjectWapi(WebView2 wv, string sysFolder)
        {
            try
            {
                if (await isWPPinjected(wv)) return "already";
                string[] cands = new string[]
                {
                    System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "wppconnect-wa.js"),
                    System.IO.Path.Combine(sysFolder ?? "", "wppconnect-wa.js"),
                    System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "wapi.js"),
                    System.IO.Path.Combine(sysFolder ?? "", "wapi.js")
                };
                foreach (string p in cands)
                {
                    try
                    {
                        if (!System.IO.File.Exists(p)) continue;
                        await wv.ExecuteScriptAsync(System.IO.File.ReadAllText(p));
                        await Task.Delay(2500);
                        if (await isWPPinjected(wv)) return "ok:" + System.IO.Path.GetFileName(p);
                    }
                    catch (Exception) { }
                }
                return "failed";
            }
            catch (Exception ex) { return "error:" + ex.Message; }
        }

        public static async Task InjectWapiSync(WebView2 wv, string sysFolder)
        {
            await InjectWapi(wv, sysFolder);
        }

        public static async Task<bool> isWaInited(WebView2 wv)
        {
            try
            {
                string r = await Eval(wv,
                    "(function(){try{"
                    + "if(window.WPP&&WPP.conn){"
                    + "try{if(WPP.conn.isAuthenticated&&WPP.conn.isAuthenticated())return 'AUTH';}catch(e){}"
                    + "try{if(WPP.conn.isMainReady&&WPP.conn.isMainReady())return 'READY';}catch(e){}"
                    + "}"
                    + "if(document.querySelector('canvas[aria-label=\"Scan me!\"]'))return 'QR';"
                    + "if(document.querySelector('#side [contenteditable]')||document.querySelector('#side input'))return 'SIDE';"
                    + "return 'UNKNOWN';}catch(e){return 'ERR';}})();");
                string v = Unwrap(r) ?? "";
                return v == "AUTH" || v == "READY" || v == "SIDE";
            }
            catch (Exception) { return false; }
        }

        public static async Task<bool> isWaInitedHandled(WebView2 wv)
        {
            return await isWaInited(wv);
        }

        public static async Task<bool> isAuthenticated(WebView2 wv)
        {
            try
            {
                string r = await Eval(wv, "try{(window.WPP&&WPP.conn&&WPP.conn.isAuthenticated&&WPP.conn.isAuthenticated())?'yes':(document.querySelector('#pane-side, #side')?'maybe':'no')}catch(e){'err'}");
                string v = (Unwrap(r) ?? "").Trim().Trim('"');
                if (v == "yes") return true;
                if (v == "maybe") return true;
                return await isWaInited(wv);
            }
            catch (Exception) { return false; }
        }

        public static async Task isListReady(WebView2 wv)
        {
            try
            {
                for (int i = 0; i < 20; i++)
                {
                    string r = null;
                    try { r = await Eval(wv, "(function(){try{var c=document.querySelectorAll('#side [data-testid=\"cell-frame-container\"]').length;var o=document.querySelectorAll('#side [role=\"option\"]').length;return 'C:'+c+'/O:'+o;}catch(e){return 'ERR';}})();"); } catch (Exception) { }
                    try
                    {
                        string s = (Unwrap(r) ?? "").Trim().Trim('"');
                        int bar = s.IndexOf('/');
                        if (s.StartsWith("C:") && bar > 2)
                        {
                            int c = 0, o = 0;
                            int.TryParse(s.Substring(2, bar - 2), out c);
                            int.TryParse(s.Substring(bar + 3), out o);
                            if (c + o > 0) return;
                        }
                    }
                    catch (Exception) { }
                    await Task.Delay(1500);
                }
            }
            catch (Exception) { }
        }

        public static async Task<string> getMyUserId(WebView2 wv)
        {
            try
            {
                if (!await EnsureWpp(wv)) return "";
                string r = await Eval(wv, "(function(){try{var fns=[function(){return WPP.conn.getMyUserId();},function(){return WPP.conn.getMyUserWid();},function(){return WPP.conn.getMyUserLid();}];for(var i=0;i<fns.length;i++){try{var v=fns[i]();if(v)return String(v.id||v.user||v);}catch(e){}}return '';}catch(e){return '';}})();");
                return (Unwrap(r) ?? "").Trim().Trim('"');
            }
            catch (Exception) { return ""; }
        }
        #endregion

        #region ===== chat open / find =====
        private static string ChatJs(string digitsOrId)
        {
            string id = (digitsOrId ?? "").Trim();
            if (!id.Contains("@")) id = new string(id.Where(char.IsDigit).ToArray()) + "@c.us";
            return JsonConvert.SerializeObject(id);
        }

        public static async Task<bool> findChatLongSync(WebView2 wv, string number)
        {
            try
            {
                if (!await EnsureWpp(wv)) return false;
                string chatIdJs = JsonConvert.SerializeObject((number ?? "").Contains("@") ? number : new string((number ?? "").Where(char.IsDigit).ToArray()) + "@c.us");
                string js = "window.__fc='PENDING';"
                    + "(function(){try{"
                    + "WPP.chat.openChatBottom(" + chatIdJs + ").then(function(){window.__fc='OPENED';}).catch(function(e){window.__fc='FAIL:'+(e&&e.message);});"
                    + "}catch(e){window.__fc='EX:'+e.message;}})();'DISPATCHED';";
                await Eval(wv, js);
                string res = await Mailbox(wv, "__fc", 8);
                return res.StartsWith("OPENED");
            }
            catch (Exception) { return false; }
        }

        public static bool findChatLong(WebView2 wv, string number)
        {
            try
            {
                var t = findChatLongSync(wv, number);
                t.Wait(15000);
                return t.Result;
            }
            catch (Exception) { return false; }
        }

        public static bool getChat(WebView2 wv, string number)
        {
            try { return System.Threading.Tasks.Task.Run(async () => await getChatAsyncImpl(wv, number)).GetAwaiter().GetResult(); }
            catch (Exception) { return false; }
        }

        private static async Task<bool> getChatAsyncImpl(WebView2 wv, string number)
        {
            try
            {
                if (!await EnsureWpp(wv)) return false;
                string chatIdJs = JsonConvert.SerializeObject((number ?? "").Contains("@") ? number : new string((number ?? "").Where(char.IsDigit).ToArray()) + "@c.us");
                string r = await Eval(wv, "(function(){try{var c=WPP.chat.get(" + chatIdJs + ");return c?'FOUND':'MISS';}catch(e){return 'ERR';}})();");
                return (Unwrap(r) ?? "").Contains("FOUND");
            }
            catch (Exception) { return false; }
        }

        public static async Task<bool> getChatSync(WebView2 wv, string number)
        {
            return await getChatAsyncImpl(wv, number);
        }

        public static bool openChatAtBottomLong(WebView2 wv, string number)
        {
            try { return System.Threading.Tasks.Task.Run(async () => await openChatAtBottomLongAsync(wv, number)).GetAwaiter().GetResult(); }
            catch (Exception) { return false; }
        }

        private static async Task<bool> openChatAtBottomLongAsync(WebView2 wv, string number)
        {
            return await findChatLongSync(wv, number);
        }

        public static async Task<bool> openChatAtBottomLongGroupSync(WebView2 wv, string groupId)
        {
            try
            {
                if (!await EnsureWpp(wv)) return false;
                string chatIdJs = JsonConvert.SerializeObject(groupId ?? "");
                string js = "window.__ocg='PENDING';"
                    + "(function(){try{WPP.chat.openChatBottom(" + chatIdJs + ").then(function(){window.__ocg='OPENED';}).catch(function(e){window.__ocg='FAIL';});}catch(e){window.__ocg='EX';}})();'DISPATCHED';";
                await Eval(wv, js);
                string res = await Mailbox(wv, "__ocg", 8);
                return res.StartsWith("OPEN");
            }
            catch (Exception) { return false; }
        }

        public static async Task<bool> OpenChatopenChatBottomAsync(WebView2 wv, string number)
        {
            return await findChatLongSync(wv, number);
        }
        #endregion

        #region ===== validasi nomor =====
        public static async Task<string> queryExists(WebView2 wv, string number)
        {
            try
            {
                if (!await EnsureWpp(wv)) return number;
                string digits = new string((number ?? "").Where(char.IsDigit).ToArray());
                string js = "window.__qe='PENDING';"
                    + "(function(){try{WPP.contact.queryExists('" + digits + "@c.us').then(function(r){window.__qe='R:'+JSON.stringify(r).substring(0,120);}).catch(function(e){window.__qe='FAIL';});}catch(e){window.__qe='EX';}})();'DISPATCHED';";
                await Eval(wv, js);
                string res = await Mailbox(wv, "__qe", 6);
                if (res.StartsWith("R:") && !res.Contains("false") && !res.Contains("null")) return digits;
                return "";
            }
            catch (Exception) { return ""; }
        }

        public static async Task<string> queryExistsSync(WebView2 wv, string number)
        {
            return await queryExists(wv, number);
        }

        public static async Task<int> validateNumberThreeInt(WebView2 wv, string number)
        {
            try
            {
                string r = await queryExists(wv, number);
                return string.IsNullOrEmpty(r) ? 0 : 1;
            }
            catch (Exception) { return 0; }
        }

        public static async Task<System.Collections.Generic.List<global::WASender.Models.NumberFilterFReturnModel>> validateNumber_ThreeSync(System.Collections.IList lisr, int from, int to, WebView2 wv)
        {
            return await ValidateList(wv, lisr, from, to);
        }

        public static async Task<System.Collections.Generic.List<global::WASender.Models.NumberFilterFReturnModel>> validateNumber3(System.Collections.IList lisr, int from, int to, WebView2 wv)
        {
            return await ValidateList(wv, lisr, from, to);
        }

        private static async Task<System.Collections.Generic.List<global::WASender.Models.NumberFilterFReturnModel>> ValidateList(WebView2 wv, System.Collections.IList lisr, int from, int to)
        {
            var outList = new List<global::WASender.Models.NumberFilterFReturnModel>();
            try
            {
                if (!await EnsureWpp(wv)) return outList;
                foreach (var item in lisr)
                {
                    try
                    {
                        string num = "";
                        try
                        {
                            dynamic c = item;
                            try { num = (string)c.number; }
                            catch (Exception) { num = item != null ? item.ToString() : ""; }
                        }
                        catch (Exception) { num = item != null ? item.ToString() : ""; }
                        num = new string((num ?? "").Where(char.IsDigit).ToArray());
                        bool ok = false;
                        if (!string.IsNullOrEmpty(num))
                        {
                            try
                            {
                                string js = "window.__vq='PENDING';"
                                    + "(function(){try{WPP.contact.queryExists('" + num + "@c.us').then(function(r){try{var wid=r&&r.wid?r.wid:r;window.__vq='R:'+JSON.stringify(wid).substring(0,160);}catch(e){window.__vq='R:unknown';}}).catch(function(){window.__vq='R:false';});}catch(e){window.__vq='EX';}})();'DISPATCHED';";
                                await Eval(wv, js);
                                string res = await Mailbox(wv, "__vq", 5);
                                ok = res.StartsWith("R:") && !res.Contains("false") && res != "R:null";
                            }
                            catch (Exception) { ok = false; }
                        }
                        bool biz = false;
                        if (ok)
                        {
                            try
                            {
                                var bp = await getBusinessProfile(wv, num + "@c.us");
                                biz = bp != null && (!string.IsNullOrEmpty(bp.name) || !string.IsNullOrEmpty(bp.website) || !string.IsNullOrEmpty(bp.email));
                            }
                            catch (Exception) { biz = false; }
                        }
                        outList.Add(new global::WASender.Models.NumberFilterFReturnModel { id = num, numberExists = ok, isBusiness = biz, canReceiveMessage = ok, status = ok ? 200 : 404 });
                        await Task.Delay(1200);
                    }
                    catch (Exception) { }
                }
            }
            catch (Exception) { }
            return outList;
        }

        public static async Task<bool> checkNumberPreswentInGroup(WebView2 wv, string number, string groupId)
        {
            try
            {
                string digits = new string((number ?? "").Where(char.IsDigit).ToArray());
                var members = await GetGroupMembers(groupId, wv);
                foreach (var m in members)
                {
                    try
                    {
                        string n = new string(((m.number ?? "") + "").Where(char.IsDigit).ToArray());
                        if (!string.IsNullOrEmpty(n) && (n == digits || n.EndsWith(digits) || digits.EndsWith(n))) return true;
                    }
                    catch (Exception) { }
                }
            }
            catch (Exception) { }
            return false;
        }
        #endregion


    #region ===== status / presence =====
        public static void markIsComposing(WebView2 wv, string chatId, int duration)
        {
            try
            {
                string id = (chatId ?? "").Contains("@") ? chatId : new string((chatId ?? "").Where(char.IsDigit).ToArray()) + "@c.us";
                string js = "try{if(window.WPP&&WPP.chat&&WPP.chat.markIsComposing){WPP.chat.markIsComposing(" + JsonConvert.SerializeObject(id) + ");}}catch(e){}";
                var _ = wv.ExecuteScriptAsync(js);
            }
            catch (Exception) { }
        }

        public static void markIsRead(WebView2 wv, string chatId)
        {
            try
            {
                string id = (chatId ?? "").Contains("@") ? chatId : new string((chatId ?? "").Where(char.IsDigit).ToArray()) + "@c.us";
                string js = "try{if(window.WPP&&WPP.chat&&(WPP.chat.markIsRead||WPP.chat.markMessageAsRead)){var f=WPP.chat.markIsRead||WPP.chat.markMessageAsRead;f(" + JsonConvert.SerializeObject(id) + ");}}catch(e){}";
                var _ = wv.ExecuteScriptAsync(js);
            }
            catch (Exception) { }
        }

        public static void sendSeen(WebView2 wv, string number)
        {
            markIsRead(wv, number);
        }

        public static async Task<bool> GetGroup__x_canSend(WebView2 wv, string groupId)
        {
            try
            {
                if (!await EnsureWpp(wv)) return true;
                string js = "(function(){try{if(window.WPP&&WPP.group){"
                    + "try{if(WPP.group.iAmAdmin&&WPP.group.iAmAdmin(" + JsonConvert.SerializeObject(groupId ?? "") + "))return 'ADMIN';}catch(e){}"
                    + "try{if(WPP.group.iAmRestrictedMember&&WPP.group.iAmRestrictedMember(" + JsonConvert.SerializeObject(groupId ?? "") + "))return 'RESTRICTED';}catch(e){}"
                    + "}return 'OK';}catch(e){return 'ERR';}})();";
                string r = await Eval(wv, js);
                string v = (Unwrap(r) ?? "");
                return !v.Contains("RESTRICTED");
            }
            catch (Exception) { return true; }
        }
        #endregion
    }
}

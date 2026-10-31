using Newtonsoft.Json;



using Newtonsoft.Json.Linq;



using OpenQA.Selenium;



using System;



using System.Collections.Generic;



using System.Linq;



using System.Threading;



using System.Threading.Tasks;







namespace WASender



{



    /// <summary>



    /// Pengganti WAPIHelper.dll (Selenium/ChromeDriver) dgn implementasi terbuka di atas WPP wa-js.



    /// </summary>



    public static class WAPIHelper



    {



        public static string GroupJoinFaildReson { get; set; }







        internal static object RawEval(object driver, string script)



        {



            try



            {



                var js = driver as IJavaScriptExecutor;



                if (js == null) return null;



                return js.ExecuteScript(script);



            }



            catch (Exception) { return null; }



        }







        internal static string Eval(object driver, string script)



        {



            try



            {



                object r = RawEval(driver, script);



                if (r == null) return "null";



                if (r is string) return "\"" + ((string)r).Replace("\\", "\\\\").Replace("\"", "\\\"") + "\"";



                try { return JsonConvert.SerializeObject(r); }



                catch (Exception) { return r.ToString(); }



            }



            catch (Exception) { return null; }



        }







        private static string Unwrap(string raw)



        {



            if (raw == null) return null;



            string s = raw.Trim();



            if (s.StartsWith("\"") && s.EndsWith("\"") && s.Length >= 2)



            {



                try { return JsonConvert.DeserializeObject<string>(s); }



                catch (Exception) { s = s.Substring(1, s.Length - 2); }



            }



            return s;



        }







        private static bool EnsureWpp(object driver)



        {



            try



            {



                string has = Unwrap(Eval(driver, "typeof window.WPP === 'object' && window.WPP.chat ? 'yes' : (typeof window.WAPI !== 'undefined' || typeof window.FMARK !== 'undefined' ? 'fmark' : 'no')"));



                if (has != null && (has.Contains("yes") || has.Contains("fmark"))) return true;



                string[] cands = new string[]



                {



                    System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "wppconnect-wa.js"),



                    System.IO.Path.Combine(Config.GetSysFolderPath(), "wppconnect-wa.js"),



                    System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "wapi.js"),



                    System.IO.Path.Combine(Config.GetSysFolderPath(), "wapi.js")



                };



                foreach (string p in cands)



                {



                    try



                    {



                        if (!System.IO.File.Exists(p)) continue;



                        RawEval(driver, System.IO.File.ReadAllText(p));



                        Thread.Sleep(2500);



                        has = Unwrap(Eval(driver, "typeof window.WPP === 'object' && window.WPP.chat ? 'yes' : 'no'"));



                        if (has != null && has.Contains("yes")) return true;



                    }



                    catch (Exception) { }



                }



                return false;



            }



            catch (Exception) { return false; }



        }







        private static string Mailbox(object driver, string name, int tries = 8)



        {



            try



            {



                for (int i = 0; i < tries; i++)



                {



                    Thread.Sleep(1500);



                    string cur = Eval(driver, "window." + name);



                    string v = Unwrap(cur);



                    if (!string.IsNullOrEmpty(v) && v != "PENDING" && v != "null") return v;



                }



            }



            catch (Exception) { }



            return "TIMEOUT";



        }







        public static void CheckExecutingAssembly()



        {



            try



            {



                if (!Config.IsProductActive()) throw new Exception("Product is not activated");



            }



            catch (Exception) { throw; }



        }







        public static bool IsWAPIInjected(object driver)



        {



            try



            {



                string r = Unwrap(Eval(driver, "typeof window.WPP === 'object' ? 'yes' : (typeof window.FMARK !== 'undefined' ? 'fmark' : 'no')"));



                return r != null && (r.Contains("yes") || r.Contains("fmark"));



            }



            catch (Exception) { return false; }



        }







        public static void injectWapi(object driver, string sysFolder)



        {



            try



            {



                if (IsWAPIInjected(driver)) return;



                string[] cands = new string[]



                {



                    System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "wppconnect-wa.js"),



                    sysFolder != null ? System.IO.Path.Combine(sysFolder, "wppconnect-wa.js") : null,



                    System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "wapi.js"),



                    sysFolder != null ? System.IO.Path.Combine(sysFolder, "wapi.js") : null



                };



                foreach (string p in cands)



                {



                    try



                    {



                        if (string.IsNullOrEmpty(p) || !System.IO.File.Exists(p)) continue;



                        RawEval(driver, System.IO.File.ReadAllText(p));



                        Thread.Sleep(2000);



                        if (IsWAPIInjected(driver)) return;



                    }



                    catch (Exception) { }



                }



            }



            catch (Exception) { }



        }







        public static bool isWaInited(object driver)



        {



            try



            {



                string r = Unwrap(Eval(driver, "(function(){try{if(window.WPP&&WPP.conn){try{if(WPP.conn.isAuthenticated&&WPP.conn.isAuthenticated())return 'AUTH';}catch(e){}try{if(WPP.conn.isMainReady&&WPP.conn.isMainReady())return 'READY';}catch(e){}}if(document.querySelector('#side [contenteditable]')||document.querySelector('#side input'))return 'SIDE';return 'NO';}catch(e){return 'ERR';}})();"));



                return r == "AUTH" || r == "READY" || r == "SIDE";



            }



            catch (Exception) { return false; }



        }







        public static bool isAuthenticated(object driver)



        {



            try



            {



                string r = Unwrap(Eval(driver, "try{(window.WPP&&WPP.conn&&WPP.conn.isAuthenticated&&WPP.conn.isAuthenticated())?'yes':(document.querySelector('#pane-side, #side')?'maybe':'no')}catch(e){'err'}"));



                if (r == "yes" || r == "maybe") return true;



                return isWaInited(driver);



            }



            catch (Exception) { return false; }



        }







        public static async Task isListReady(object driver)



        {



            try



            {



                for (int i = 0; i < 20; i++)



                {



                    string r = Unwrap(Eval(driver, "(function(){try{return 'C:'+document.querySelectorAll('#side [data-testid=\"cell-frame-container\"]').length;}catch(e){return 'ERR';}})();"));



                    if (r != null && !r.Contains("ERR")) return;



                    await Task.Delay(1500);



                }



            }



            catch (Exception) { }



        }







        private static string ChatId(object to)



        {



            string s = (to ?? "").ToString();



            if (string.IsNullOrEmpty(s)) return "";



            if (s.Contains("@")) return s;



            return new string(s.Where(char.IsDigit).ToArray()) + "@c.us";



        }







        public static bool findChatLong(object driver, string number)



        {



            try



            {



                if (!EnsureWpp(driver)) return false;



                string js = "window.__fcl='PENDING';"



                    + "(function(){try{WPP.chat.openChatBottom(" + JsonConvert.SerializeObject(ChatId(number)) + ").then(function(){window.__fcl='OPENED';}).catch(function(e){window.__fcl='FAIL';});}catch(e){window.__fcl='EX';}})();'DISPATCHED';";



                RawEval(driver, js);



                string res = Mailbox(driver, "__fcl", 8);



                return res.StartsWith("OPEN");



            }



            catch (Exception) { return false; }



        }







        public static async Task<bool> findChatLongSync(object driver, string number)



        {



            return await Task.Run(() => findChatLong(driver, number));



        }







        public static bool getChat(object driver, string number)



        {



            try



            {



                if (!EnsureWpp(driver)) return false;



                string r = Unwrap(Eval(driver, "(function(){try{var c=WPP.chat.get(" + JsonConvert.SerializeObject(ChatId(number)) + ");return c?'FOUND':'MISS';}catch(e){return 'ERR';}})();"));



                return r != null && r.Contains("FOUND");



            }



            catch (Exception) { return false; }



        }







        public static async Task<bool> getChatAsync(object driver, string number)



        {



            return await Task.Run(() => getChat(driver, number));



        }







        public static void OpenChatopenChatBottom(object driver, string number)



        {



            try { findChatLong(driver, number); }



            catch (Exception) { }



        }







        public static string queryExists(object driver, string number)



        {



            try



            {



                if (!EnsureWpp(driver)) return number;



                string num = new string((number ?? "").Where(char.IsDigit).ToArray());



                string r = Unwrap(Eval(driver, "(function(){try{WPP.contact.queryExists(" + JsonConvert.SerializeObject(num + "@c.us") + ").then(function(x){window.__qe2=JSON.stringify(x);}).catch(function(){window.__qe2='false';});return 'SENT';}catch(e){return 'EX';}})();"));



                Thread.Sleep(4000);



                string res = Unwrap(Eval(driver, "window.__qe2"));



                if (!string.IsNullOrEmpty(res) && res != "false" && res != "null" && res.Contains(num)) return number;



                return number;



            }



            catch (Exception) { return number; }



        }







        public static bool validateNumber(object driver, string number)



        {



            try



            {



                string r = queryExists(driver, number);



                return !string.IsNullOrEmpty(r);



            }



            catch (Exception) { return false; }



        }







        public static async Task<List<WASender.Models.NumberFilterFReturnModel>> validateNumber_threeSync(object driver, System.Collections.IList lisr, int from, int to)



        {



            return await ValidateList(driver, lisr);



        }







        public static async Task<List<WASender.Models.NumberFilterFReturnModel>> validateNumber3(object driver, System.Collections.IList lisr, int from, int to)



        {



            return await ValidateList(driver, lisr);



        }







        private static async Task<List<WASender.Models.NumberFilterFReturnModel>> ValidateList(object driver, System.Collections.IList lisr)



        {



            var outList = new List<WASender.Models.NumberFilterFReturnModel>();



            try



            {



                if (!EnsureWpp(driver)) return outList;



                foreach (var item in lisr)



                {



                    try
                    {
                        string num = "";
                        try { dynamic c0 = item; try { num = (string)c0.number; } catch (Exception) { num = item != null ? item.ToString() : ""; } }
                        catch (Exception) { num = item != null ? item.ToString() : ""; }
                        num = new string((num ?? "").Where(char.IsDigit).ToArray());
                        bool ok = false;
                        if (!string.IsNullOrEmpty(num))
                        {
                            try
                            {
                                RawEval(driver, "window.__vq='PENDING';(function(){try{WPP.contact.queryExists('" + num + "@c.us').then(function(r){try{window.__vq='R:'+JSON.stringify(r&&r.wid?r.wid:r).substring(0,120);}catch(e){window.__vq='R:unknown';}}).catch(function(){window.__vq='R:false';});}catch(e){window.__vq='EX';}})();'x';");
                                Thread.Sleep(2500);
                                string res = Unwrap(Eval(driver, "window.__vq"));
                                ok = res != null && res.StartsWith("R:") && !res.Contains("false") && res != "R:null";
                            }
                            catch (Exception) { ok = false; }
                        }
                        outList.Add(new WASender.Models.NumberFilterFReturnModel { id = num, numberExists = ok, isBusiness = false, canReceiveMessage = ok, status = ok ? 200 : 404 });
                    }
                    catch (Exception) { }
                    await Task.Delay(1200);



                }



            }



            catch (Exception) { }



            return outList;



        }







        public static bool checkNumberPreswentInGroup(object driver, string number, string groupId)



        {



            try



            {



                var members = GetGroupMembers("", driver);



                string num = new string((number ?? "").Where(char.IsDigit).ToArray());



                foreach (var m in members)



                {



                    try



                    {



                        string mn = "";



                        try { mn = m.number ?? ""; }



                        catch (Exception) { continue; }



                        mn = new string((mn ?? "").Where(char.IsDigit).ToArray());



                        if (!string.IsNullOrEmpty(mn) && (mn == num || mn.EndsWith(num) || num.EndsWith(mn))) return true;



                    }



                    catch (Exception) { }



                }



            }



            catch (Exception) { }



            return false;



        }







        public static string addParticipants(object driver, string groupId, string number, bool isAdmin, string msg)



        {



            try



            {



                if (!EnsureWpp(driver)) return "nowpp";



                string digits = new string((number ?? "").Where(char.IsDigit).ToArray());



                string js = "window.__adp='PENDING';"



                    + "(function(){try{WPP.group.addParticipants(" + JsonConvert.SerializeObject(groupId ?? "") + "," + JsonConvert.SerializeObject(new string[] { digits + "@c.us" }) + ")"



                    + ".then(function(r){window.__adp='OK';}).catch(function(e){window.__adp='FAIL:'+(e&&e.message);});"



                    + "}catch(e){window.__adp='EX:'+e.message;}})();'DISPATCHED';";



                RawEval(driver, js);



                string res = Mailbox(driver, "__adp", 14);



                return res.StartsWith("OK") ? "OK" : res;



            }



            catch (Exception ex) { return "EX:" + ex.Message; }



        }







        public static void CreateGroup(object driver, string name, string defaultNumber)



        {



            try



            {



                if (!EnsureWpp(driver)) return;



                string digits = new string((defaultNumber ?? "").Where(char.IsDigit).ToArray());



                string parts = string.IsNullOrEmpty(digits) ? "[]" : "[" + JsonConvert.SerializeObject(digits + "@c.us") + "]";



                RawEval(driver, "(function(){try{WPP.group.create(" + JsonConvert.SerializeObject(name ?? "New Group") + "," + parts + ").catch(function(){});}catch(e){}})();'DISPATCHED';");



                Thread.Sleep(4000);



            }



            catch (Exception) { }



        }







        public static async Task<bool> JoinGroup(object driver, string code)



        {



            try



            {



                if (!EnsureWpp(driver)) return false;



                string c = (code ?? "").Trim();



                int idx = c.LastIndexOf('/');



                if (idx >= 0) c = c.Substring(idx + 1);



                c = c.Split('?')[0];



                string js = "window.__jg2='PENDING';"



                    + "(function(){try{WPP.group.join(" + JsonConvert.SerializeObject(c) + ").then(function(){window.__jg2='OK';}).catch(function(e){window.__jg2='FAIL';});}catch(e){window.__jg2='EX';}})();'DISPATCHED';";



                RawEval(driver, js);



                string res = Mailbox(driver, "__jg2", 16);



                if (!res.StartsWith("OK")) { GroupJoinFaildReson = res; return false; }



                return true;



            }



            catch (Exception ex) { GroupJoinFaildReson = ex.Message; return false; }



        }







        public static bool GetGroup__x_canSend(object driver, string groupId)



        {



            try



            {



                if (!EnsureWpp(driver)) return true;



                string r = Unwrap(Eval(driver, "(function(){try{if(WPP.group&&WPP.group.iAmRestrictedMember&&WPP.group.iAmRestrictedMember(" + JsonConvert.SerializeObject(groupId ?? "") + "))return 'RESTRICTED';return 'OK';}catch(e){return 'ERR';}})();"));



                return r == null || !r.Contains("RESTRICT");



            }



            catch (Exception) { return true; }



        }







        public static void markIsComposing(object driver, string groupId, int duration)



        {



            try



            {



                if (!EnsureWpp(driver)) return;



                RawEval(driver, "try{if(WPP.chat&&WPP.chat.markIsComposing){WPP.chat.markIsComposing(" + JsonConvert.SerializeObject(ChatId(groupId)) + ");}}catch(e){}");



            }



            catch (Exception) { }



        }







        public static void markIsRead(object driver, string chatId)



        {



            try



            {



                if (!EnsureWpp(driver)) return;



                RawEval(driver, "try{if(WPP.chat&&(WPP.chat.markIsRead||WPP.chat.markMessageAsRead)){(WPP.chat.markIsRead||WPP.chat.markMessageAsRead)(" + JsonConvert.SerializeObject(ChatId(chatId)) + ");}}catch(e){}");



            }



            catch (Exception) { }



        }







        public static void sendSeen(object driver, string number)



        {



            markIsRead(driver, number);



        }







        // ===== listing =====



        private static List<T> ParseList<T>(string raw, Func<JToken, T> map) where T : class



        {



            var list = new List<T>();



            try



            {



                if (string.IsNullOrEmpty(raw)) return list;



                string s = raw.Trim();



                if (s.StartsWith("\""))



                {



                    try { s = JsonConvert.DeserializeObject<string>(s); }



                    catch (Exception) { s = s.Trim('"'); }



                }



                if (!s.StartsWith("[")) return list;



                JArray ja = JArray.Parse(s.Length > 380000 ? s.Substring(0, 380000) : s);



                foreach (JToken t in ja)



                {



                    try



                    {



                        T v = map(t);



                        if (v != null) list.Add(v);



                    }



                    catch (Exception) { }



                }



            }



            catch (Exception) { }



            return list;



        }







        private static string Str(JToken t, params string[] names)



        {



            try



            {



                if (t == null) return "";



                foreach (string n in names)



                {



                    JToken v = t[n];



                    if (v != null && v.Type != JTokenType.Null)



                    {



                        string s = (v.Type == JTokenType.Object || v.Type == JTokenType.Array) ? v.ToString(Formatting.None) : v.ToString();



                        if (!string.IsNullOrEmpty(s)) return s;



                    }



                }



            }



            catch (Exception) { }



            return "";



        }







        private static string Digits(string s)



        {



            if (string.IsNullOrEmpty(s)) return "";



            return new string(s.Where(char.IsDigit).ToArray());



        }







        public static List<WASender.Models.WAPI_ContactModel> getMyContacts(object driver)



        {



            try



            {



                if (!EnsureWpp(driver)) return new List<WASender.Models.WAPI_ContactModel>();



                RawEval(driver, "window.__gc='PENDING';(function(){try{var cs=WPP.contact.list();var out=cs.slice(0,5000).map(function(c){try{return{id:(c.id&&(c.id._serialized||String(c.id)))||'',name:c.name||c.pushname||c.formattedName||'',num:(c.id&&(c.id.user||''))||'',isGroup:!!(c.isGroup)}catch(e){return null;}}).filter(function(x){return x&&!x.isGroup;});window.__gc='OK:'+JSON.stringify(out).substring(0,380000);}catch(e){window.__gc='FAIL';}})();'x';");



                Thread.Sleep(4000);



                string res = Unwrap(Eval(driver, "window.__gc"));



                if (res == null || !res.StartsWith("OK:")) return new List<WASender.Models.WAPI_ContactModel>();



                return ParseList(res.Substring(3), delegate(JToken c)



                {



                    string num = Digits(Str(c, "num"));



                    if (string.IsNullOrEmpty(num))



                    {



                        string id = Str(c, "id");



                        if (id.Contains("@")) num = Digits(id.Split('@')[0]);



                    }



                    if (string.IsNullOrEmpty(num)) return null;



                    return new WASender.Models.WAPI_ContactModel { number = num, Name = Str(c, "name"), Labels = "", countryShortCode = "", isGroup = false };



                });



            }



            catch (Exception) { return new List<WASender.Models.WAPI_ContactModel>(); }



        }







        public static List<WASender.Models.WAPI_ContactModel> getAlChats(object driver)



        {



            try



            {



                if (!EnsureWpp(driver)) return new List<WASender.Models.WAPI_ContactModel>();



                RawEval(driver, "window.__gac='PENDING';(function(){try{var cs=WPP.chat.list();var out=cs.slice(0,5000).map(function(c){try{var id=(c.id&&(c.id._serialized||String(c.id)))||'';return{id:id,name:c.name||c.formattedTitle||'',isGroup:!!(c.isGroup||c.groupMetadata)};}catch(e){return null;}}).filter(function(x){return !!x;});window.__gac='OK:'+JSON.stringify(out).substring(0,380000);}catch(e){window.__gac='FAIL';}})();'x';");



                Thread.Sleep(4000);



                string res = Unwrap(Eval(driver, "window.__gac"));



                if (res == null || !res.StartsWith("OK:")) return new List<WASender.Models.WAPI_ContactModel>();



                return ParseList(res.Substring(3), delegate(JToken c)



                {



                    string id = Str(c, "id");



                    string num = id.Contains("@") ? id.Split('@')[0] : id;



                    bool isg = false;



                    try { isg = c["isGroup"] != null && c["isGroup"].ToString().ToLower() == "true"; }



                    catch (Exception) { }



                    if (!isg) num = Digits(num);



                    return new WASender.Models.WAPI_ContactModel { number = num, Name = Str(c, "name"), Labels = "", countryShortCode = "", isGroup = isg };



                });



            }



            catch (Exception) { return new List<WASender.Models.WAPI_ContactModel>(); }



        }







        public static List<WASender.Models.WAPI_ContactModel> getAlChatsWithLabel(object driver, string label)



        {



            try { return getAlChats(driver); } catch (Exception) { return new List<WASender.Models.WAPI_ContactModel>(); }



        }







        public static List<global::Models.LableModel> getAllLabels(object driver)



        {



            var list = new List<global::Models.LableModel>();



            try



            {



                if (!EnsureWpp(driver)) return list;



                RawEval(driver, "window.__gl='PENDING';(function(){try{var ls=(WPP.labels&&WPP.labels.getAllLabels?WPP.labels.getAllLabels():[]);window.__gl='OK:'+JSON.stringify(ls.map(function(l){try{return{id:String(l.id||l.hexColor||l.name),name:l.name||'',color:l.hexColor||'',count:l.count||0};}catch(e){return null;}}).filter(function(x){return !!x;}));}catch(e){window.__gl='FAIL';}})();'x';");



                Thread.Sleep(3000);



                string res = Unwrap(Eval(driver, "window.__gl"));



                if (res == null || !res.StartsWith("OK:")) return list;



                return ParseList(res.Substring(3), delegate(JToken l)



                {



                    return new global::Models.LableModel { id = Str(l, "id"), name = Str(l, "name"), color = Str(l, "color"), hexColor = Str(l, "color"), count = 0 };



                });



            }



            catch (Exception) { return list; }



        }







        public static async Task<List<WASender.Models.WAPI_GroupModel>> getMyGroups(object driver)



        {



            return await Task.Run(() => getMyGroupsSync(driver));



        }







        private static List<WASender.Models.WAPI_GroupModel> getMyGroupsSync(object driver)



        {



            var list = new List<WASender.Models.WAPI_GroupModel>();



            try



            {



                if (!EnsureWpp(driver)) return list;



                RawEval(driver, "window.__gg='PENDING';(function(){try{var gs=(WPP.group&&WPP.group.getAllGroups?WPP.group.getAllGroups():[]);var out=gs.map(function(g){try{var id=(g.id&&(g.id._serialized||String(g.id)))||'';return{id:id,name:g.name||g.formattedTitle||''};}catch(e){return null;}}).filter(function(x){return x&&x.id;});window.__gg='OK:'+JSON.stringify(out).substring(0,380000);}catch(e){window.__gg='FAIL';}})();'x';");



                Thread.Sleep(4000);



                string res = Unwrap(Eval(driver, "window.__gg"));



                if (res == null || !res.StartsWith("OK:")) return list;



                return ParseList(res.Substring(3), delegate(JToken g)



                {



                    return new WASender.Models.WAPI_GroupModel { GroupId = Str(g, "id"), GroupName = Str(g, "name"), GroupLink = "", CanSend = true };



                });



            }



            catch (Exception) { return list; }



        }







        public static async Task<List<WASender.Models.WAPI_GroupModel>> getMyOwnedGroups(object driver)



        {



            return await getMyGroups(driver);



        }







        public static List<WASender.Models.WAPI_GroupModel> getMyCommunities(object driver)



        {



            try { return getMyGroupsSync(driver).FindAll(g => (g.GroupId ?? "").Contains("@g.us")); }



            catch (Exception) { return new List<WASender.Models.WAPI_GroupModel>(); }



        }







        public static List<WASender.Models.GroupMembersModel> GetGroupMembers(WASender.Models.WAPI_GroupModel group, object driver)
        {
            string gid = "";
            try { gid = group != null ? group.GroupId : ""; }
            catch (Exception) { }
            return GetGroupMembers(gid, driver);
        }

        public static List<WASender.Models.GroupMembersModel> GetGroupMembers(string group, object driver)



        {



            var list = new List<WASender.Models.GroupMembersModel>();



            try



            {



                if (!EnsureWpp(driver)) return list;



                RawEval(driver, "window.__gm='PENDING';(function(){try{WPP.group.getParticipants(" + JsonConvert.SerializeObject(group ?? "") + ").then(function(ps){try{var arr=(ps&&ps.map)?ps:(ps&&ps.participants?ps.participants:[]);var out=arr.slice(0,5000).map(function(p){try{var id=p.id||p;var sid=(typeof id==='string')?id:((id&&(id._serialized||String(id)))||'');return{id:sid,num:(sid.split('@')[0]||''),name:p.pushname||p.name||''};}catch(e){return null;}}).filter(function(x){return !!x;});window.__gm='OK:'+JSON.stringify(out).substring(0,380000);}catch(e){window.__gm='FAIL';}}).catch(function(e){window.__gm='FAIL';});}catch(e){window.__gm='EX';}})();'x';");



                Thread.Sleep(6000);



                string res = Unwrap(Eval(driver, "window.__gm"));



                if (res == null || !res.StartsWith("OK:")) return list;



                return ParseList(res.Substring(3), delegate(JToken p)



                {



                    string num = Digits(Str(p, "num"));



                    if (string.IsNullOrEmpty(num)) return null;



                    return new WASender.Models.GroupMembersModel { number = num, CountryShortCode = "" };



                });



            }



            catch (Exception) { return new List<WASender.Models.GroupMembersModel>(); }



        }







        public static async Task<List<WASender.Models.ActiveMemberModel>> getChatAndCount(object driver, string groupId)



        {



            var list = new List<WASender.Models.ActiveMemberModel>();



            try



            {



                var members = GetGroupMembers(groupId, driver);



                foreach (var m in members)



                {



                    try { list.Add(new WASender.Models.ActiveMemberModel { number = m.number, count = 0 }); }



                    catch (Exception) { }



                }



            }



            catch (Exception) { }



            return await Task.FromResult(list);



        }







        public static async Task<List<WASender.Models.GroupInfoModel>> getGroupInfoByLink(object driver, string link)



        {



            var list = new List<WASender.Models.GroupInfoModel>();



            try



            {



                if (!EnsureWpp(driver)) return list;



                string code = (link ?? "").Trim();



                int idx = code.LastIndexOf('/');



                if (idx >= 0) code = code.Substring(idx + 1);



                code = code.Split('?')[0];



                RawEval(driver, "window.__gib='PENDING';(function(){try{if(WPP.group&&WPP.group.getGroupInfoFromInviteCode){WPP.group.getGroupInfoFromInviteCode(" + JsonConvert.SerializeObject(code) + ").then(function(g){try{var o={subject:g.subject||g.name||'',desc:g.desc||g.description||'',size:g.size||(g.participants?g.participants.length:0),announce:!!g.announce,parts:[]};try{if(g.participants&&g.participants.map){o.parts=g.participants.slice(0,2000).map(function(p){var id=p.id||p;return (typeof id==='string')?id:((id&&(id._serialized||String(id)))||'');});}}catch(e){}window.__gib='OK:'+JSON.stringify(o);}catch(e){window.__gib='FAIL';}}).catch(function(e){window.__gib='FAIL';});}else{window.__gib='NOAPI';}})();'x';");



                Thread.Sleep(6000);



                string res = Unwrap(Eval(driver, "window.__gib"));



                if (res == null || !res.StartsWith("OK:")) return list;



                try



                {



                    JObject o = JObject.Parse(res.Substring(3));



                    var gi = new WASender.Models.GroupInfoModel();



                    gi.groupLink = link;



                    gi.status = "";



                    try { gi.announce = o["announce"] != null && o["announce"].ToString().ToLower() == "true"; }



                    catch (Exception) { }



                    gi.desc = o["desc"] != null ? o["desc"].ToString() : "";



                    try { gi.size = o["size"] != null ? Convert.ToInt32(o["size"].ToString()) : 0; }



                    catch (Exception) { gi.size = 0; }



                    gi.subject = o["subject"] != null ? o["subject"].ToString() : "";



                    gi.participants = new List<string>();



                    try



                    {



                        JToken ps = o["parts"];



                        if (ps != null && ps.Type == JTokenType.Array)



                        {



                            foreach (JToken p in ps)



                            {



                                string s = p.ToString();



                                if (!string.IsNullOrEmpty(s)) gi.participants.Add(s);



                            }



                        }



                    }



                    catch (Exception) { }



                    gi.isDone = true;



                    list.Add(gi);



                }



                catch (Exception) { }



            }



            catch (Exception) { }



            return await Task.FromResult(list);



        }







        public static List<global::Models.PollReportModel> GetPollResults(object driver)



        {



            return new List<global::Models.PollReportModel>();



        }







        public static global::Models.BusinessProfileModel getBusinessProfileDetails(object driver, string chatId)



        {



            var bp = new global::Models.BusinessProfileModel();



            try



            {



                if (!EnsureWpp(driver)) return bp;



                RawEval(driver, "window.__bp='PENDING';(function(){try{if(WPP.contact&&WPP.contact.getBusinessProfile){WPP.contact.getBusinessProfile(" + JsonConvert.SerializeObject(chatId ?? "") + ").then(function(p){try{window.__bp='OK:'+JSON.stringify({name:p.name||'',desc:p.description||p.desc||'',email:p.email||'',website:p.website||'',cat:(p.categories&&p.categories[0])||p.category||''});}catch(e){window.__bp='FAIL';}}).catch(function(e){window.__bp='FAIL';});}else{window.__bp='NOAPI';}})();'x';");



                Thread.Sleep(4000);



                string res = Unwrap(Eval(driver, "window.__bp"));



                if (res == null || !res.StartsWith("OK:")) return bp;



                try



                {



                    JObject o = JObject.Parse(res.Substring(3));



                    bp.name = o["name"] != null ? o["name"].ToString() : bp.name;



                    bp.desc = o["desc"] != null ? o["desc"].ToString() : bp.desc;



                    bp.email = o["email"] != null ? o["email"].ToString() : bp.email;



                    bp.website = o["website"] != null ? o["website"].ToString() : bp.website;



                    bp.catagory = o["cat"] != null ? o["cat"].ToString() : bp.catagory;



                }



                catch (Exception) { }



            }



            catch (Exception) { }



            return bp;



        }







        



        // ===== kirim (Selenium) =====



        private static bool WppSend(object driver, string chatId, string jsBody, string mbox, int waitMs = 15000)



        {



            try



            {



                if (!EnsureWpp(driver)) return false;



                RawEval(driver, "window." + mbox + "='PENDING';" + jsBody + "'DISPATCHED';");



                long end = DateTime.Now.Ticks + waitMs * 10000L;



                while (DateTime.Now.Ticks < end)



                {



                    Thread.Sleep(1500);



                    string r = Unwrap(Eval(driver, "window." + mbox));



                    if (string.IsNullOrEmpty(r) || r == "PENDING" || r == "null") continue;



                    return r.StartsWith("OK:{") || r.StartsWith("OK:true") || r == "OK";



                }



            }



            catch (Exception) { }



            return false;



        }







        public static void SendMessage(object driver, string to, string msg) { try { WppSendTextSync(driver, to, msg); } catch (Exception) { } }



        public static void SendMessage(object driver, string to, string msg, bool p1, bool p2) { try { WppSendTextSync(driver, to, msg); } catch (Exception) { } }



        public static void SendMessage(object driver, string to, string msg, bool p1) { try { WppSendTextSync(driver, to, msg); } catch (Exception) { } }



        public static void SendMessage(object driver, string to, string msg, bool p1, bool p2, WASender.Models.ButtonHolderModel holder) { try { WppSendTextSync(driver, to, msg); } catch (Exception) { } }







        private static bool WppSendTextSync(object driver, string to, string text)



        {



            try



            {



                string chatId = to ?? "";



                if (!chatId.Contains("@"))



                {



                    string d = new string(chatId.Where(char.IsDigit).ToArray());



                    chatId = d + "@c.us";



                }



                string js = "(function(){try{WPP.chat.sendTextMessage(" + JsonConvert.SerializeObject(chatId) + "," + JsonConvert.SerializeObject(text ?? "")



                    + ").then(function(r){window.__wst='OK:'+JSON.stringify(r).substring(0,200);}).catch(function(e){window.__wst='FAIL:'+(e&&e.message);});}catch(e){window.__wst='EX:'+e.message;}})();";



                RawEval(driver, js);



                long end = DateTime.Now.Ticks + 20000 * 10000L;



                while (DateTime.Now.Ticks < end)



                {



                    Thread.Sleep(1500);



                    string r = Unwrap(Eval(driver, "window.__wst"));



                    if (string.IsNullOrEmpty(r) || r == "PENDING" || r == "null") continue;



                    return r.StartsWith("OK:{");



                }



            }



            catch (Exception) { }



            return false;



        }







        public static void sendTextMessage(object driver, string number, string msg) { try { WppSendTextSync(driver, number, msg); } catch (Exception) { } }







        public static void sendTextMessageWithPreview(object driver, string to, string msg, bool p) { try { WppSendTextSync(driver, to, msg); } catch (Exception) { } }







        public static void sendButtonWithMessage(object driver, object model, string number, string msg)



        {



            try



            {



                string body = msg ?? "";



                try



                {



                    dynamic dmm = model;
                    if (dmm == null) { sendTextMessage(driver, number, msg); return; }
                    if (dmm.buttons != null)



                    {



                        var opts = new List<string>();



                        foreach (var b in dmm.buttons)



                        {



                            try



                            {



                                string t = null;



                                var bt = b.GetType();



                                foreach (string nn in new string[] { "ButtonText", "buttonText", "Text", "text", "Title", "title" })



                                {



                                    var pp = bt.GetProperty(nn);



                                    if (pp != null) { object vv = pp.GetValue(b, null); if (vv != null) { t = vv.ToString(); break; } }



                                }



                                if (!string.IsNullOrEmpty(t)) opts.Add(t);



                            }



                            catch (Exception) { }



                        }



                        if (opts.Count > 0) body = body + "\n" + string.Join(" / ", opts.ToArray());



                    }



                }



                catch (Exception) { }



                WppSendTextSync(driver, number, body);



            }



            catch (Exception) { }



        }







        public static void sendButtonWithMessageToGroup(object driver, object model, string groupId, string msg)



        {



            try { sendButtonWithMessage(driver, model, groupId, msg); }



            catch (Exception) { }



        }







        public static void sendCreatePollMessageToNumber(object driver, string to, object poll) { try { var _ = sendCreatePollMessageToNumberAsync(driver, to, poll); } catch (Exception) { } }







        public static async Task<bool> sendCreatePollMessageToNumberAsync(object driver, string to, object poll)



        {



            try



            {



                if (!EnsureWpp(driver)) return false;



                string name = "Poll";



                var choices = new List<string>();



                try



                {



                    if (poll != null)



                    {



                        var t = poll.GetType();



                        foreach (string n in new string[] { "PollName", "pollName", "Name", "name" })



                        {



                            var pp = t.GetProperty(n);



                            if (pp != null) { object vv = pp.GetValue(poll, null); if (vv != null) { name = vv.ToString(); break; } }



                        }



                        foreach (string n in new string[] { "PollOptions", "pollOptions", "Options", "options", "Choices", "choices" })



                        {



                            var pp = t.GetProperty(n);



                            if (pp == null) continue;



                            var v = pp.GetValue(poll, null) as System.Collections.IEnumerable;



                            if (v == null) continue;



                            foreach (var o in v)



                            {



                                try



                                {



                                    if (o is string) { choices.Add((string)o); continue; }



                                    var ot = o.GetType();



                                    foreach (string nn in new string[] { "OptionText", "Option", "Text", "text", "Name", "name" })



                                    {



                                        var p2 = ot.GetProperty(nn);



                                        if (p2 != null) { object vv = p2.GetValue(o, null); if (vv != null) { choices.Add(vv.ToString()); break; } }



                                    }



                                }



                                catch (Exception) { }



                            }



                            if (choices.Count > 0) break;



                        }



                    }



                }



                catch (Exception) { }



                if (choices.Count == 0) return false;



                string chatId = to ?? "";



                if (!chatId.Contains("@")) { string d = new string(chatId.Where(char.IsDigit).ToArray()); chatId = d + "@c.us"; }



                RawEval(driver, "window.__spn='PENDING';(function(){try{WPP.chat.sendCreatePollMessage(" + JsonConvert.SerializeObject(chatId) + "," + JsonConvert.SerializeObject(name) + "," + JsonConvert.SerializeObject(choices) + ").then(function(r){window.__spn='OK:'+JSON.stringify(r).substring(0,200);}).catch(function(e){window.__spn='FAIL';});}catch(e){window.__spn='EX';}})();'x';");



                for (int i = 0; i < 10; i++)



                {



                    Thread.Sleep(1500);



                    string r = Unwrap(Eval(driver, "window.__spn"));



                    if (!string.IsNullOrEmpty(r) && r != "PENDING" && r != "null") return r.StartsWith("OK:{");



                }



                return false;



            }



            catch (Exception) { return false; }



        }







        public static void sendCreatePollMessageToGroup(object driver, string groupId, object poll) { try { var _ = sendCreatePollMessageToNumberAsync(driver, groupId, poll); } catch (Exception) { } }







        private static string FullB64(object fileOrPath, out string contentType, out string fileName)



        {



            contentType = "application/octet-stream";



            fileName = "file";



            try



            {



                string path = fileOrPath as string;



                if (string.IsNullOrEmpty(path) || !System.IO.File.Exists(path)) return null;



                fileName = System.IO.Path.GetFileName(path);



                string ext = (System.IO.Path.GetExtension(path) ?? "").ToLower();



                try { contentType = System.Web.MimeMapping.GetMimeMapping(path); }



                catch (Exception) { }



                if (ext == ".mp4") contentType = "video/mp4";



                return "data:" + contentType + ";base64," + Convert.ToBase64String(System.IO.File.ReadAllBytes(path));



            }



            catch (Exception) { return null; }



        }







        private static bool WppSendMediaSync(object driver, string to, string base64, string fileName, string caption, string box)



        {



            try



            {



                if (string.IsNullOrEmpty(base64) || !EnsureWpp(driver)) return false;



                string chatId = to ?? "";



                if (!chatId.Contains("@"))



                {



                    string d = new string(chatId.Where(char.IsDigit).ToArray());



                    chatId = d + "@c.us";



                }



                string js = "window." + box + "='PENDING';"



                    + "(function(){try{WPP.chat.sendFileMessage(" + JsonConvert.SerializeObject(chatId) + "," + JsonConvert.SerializeObject(base64)



                    + ",{filename:" + JsonConvert.SerializeObject(fileName ?? "file") + (string.IsNullOrEmpty(caption) ? "" : ",caption:" + JsonConvert.SerializeObject(caption))



                    + "}).then(function(r){window." + box + "='OK:'+JSON.stringify(r).substring(0,200);}).catch(function(e){window." + box + "='FAIL:'+(e&&e.message);});"



                    + "}catch(e){window." + box + "='EX:'+e.message;}})();'x';";



                RawEval(driver, js);



                long end = DateTime.Now.Ticks + 60000 * 10000L;



                while (DateTime.Now.Ticks < end)



                {



                    Thread.Sleep(2000);



                    string r = Unwrap(Eval(driver, "window." + box));



                    if (string.IsNullOrEmpty(r) || r == "PENDING" || r == "null") continue;



                    return r.StartsWith("OK:{");



                }



            }



            catch (Exception) { }



            return false;



        }







        public static void SendVideo(object driver, string to, string base64, string caption, string fileName, WASender.Models.ButtonHolderModel holder) { try { WppSendMediaSync(driver, to, base64, fileName, caption, "__svw1"); } catch (Exception) { } }



        public static void SendVideo(object driver, string to, string base64, string caption, string fileName) { try { WppSendMediaSync(driver, to, base64, fileName, caption, "__svw2"); } catch (Exception) { } }



        public static void SendVideo(object driver, string to, string base64, string msg, string fileName, out string outParam, bool tagAll) { outParam = ""; try { WppSendMediaSync(driver, to, base64, fileName, msg, "__svw3"); } catch (Exception) { } }



        public static void SendVideo(object driver, string to, string base64, string caption, string msg, string fileName, WASender.Models.ButtonHolderModel holder) { try { WppSendMediaSync(driver, to, base64, fileName, caption, "__svw4"); } catch (Exception) { } }



        public static void SendAudio(object driver, string to, string base64, string caption) { try { WppSendMediaSync(driver, to, base64, "a", caption, "__saw1"); } catch (Exception) { } }



        public static void SendAudio(object driver, string to, string base64, string caption, WASender.Models.ButtonHolderModel holder) { try { WppSendMediaSync(driver, to, base64, "a", caption, "__saw2"); } catch (Exception) { } }



        public static bool sendAudioFile(object driver, string to, string base64) { try { return WppSendMediaSync(driver, to, base64, "a", "", "__safw"); } catch (Exception) { return false; } }



        public static void SendAttachment(object driver, string to, string base64, string fileName, string caption) { try { WppSendMediaSync(driver, to, base64, fileName, caption, "__satw"); } catch (Exception) { } }



        public static void SendAttachment(object driver, string to, string base64, string fileName, string caption, out string outParam) { outParam = ""; try { WppSendMediaSync(driver, to, base64, fileName, caption, "__satw2"); } catch (Exception) { } }



        public static void SendVideoToGroup(object driver, string groupId, string base64, string msg, string fileName, out string outParam, bool tagAll) { outParam = ""; try { WppSendMediaSync(driver, groupId, base64, fileName, msg, "__svgw"); } catch (Exception) { } }



        public static void SendAttachmentToGroup(object driver, string groupId, string base64, string fileName, string caption, out string outParam) { outParam = ""; try { WppSendMediaSync(driver, groupId, base64, fileName, caption, "__sagw"); } catch (Exception) { } }



        public static bool sendVideoNote(object driver, string to, string base64) { try { return WppSendMediaSync(driver, to, base64, "v", "", "__svn"); } catch (Exception) { return false; } }



        public static bool sendStickerSync(object driver, string to, string base64) { try { return WppSendMediaSync(driver, to, base64, "s", "", "__sstw"); } catch (Exception) { return false; } }



        public static bool SendLocationMessage(object driver, string to, string lat, string lng, string name, string addr, string url)



        {



            try



            {



                if (!EnsureWpp(driver)) return false;



                string chatId = ChatId(to);



                RawEval(driver, "window.__slmw='PENDING';(function(){try{WPP.chat.sendLocationMessage(" + JsonConvert.SerializeObject(chatId) + ",{lat:" + JsonConvert.SerializeObject(lat ?? "") + ",lng:" + JsonConvert.SerializeObject(lng ?? "") + ",name:" + JsonConvert.SerializeObject(name ?? "") + ",address:" + JsonConvert.SerializeObject(addr ?? "") + ",url:" + JsonConvert.SerializeObject(url ?? "") + "}).then(function(r){window.__slmw='OK:'+JSON.stringify(r).substring(0,200);}).catch(function(e){window.__slmw='FAIL';});}catch(e){window.__slmw='EX';}})();'x';");



                long end = DateTime.Now.Ticks + 30000 * 10000L;



                while (DateTime.Now.Ticks < end)



                {



                    Thread.Sleep(1500);



                    string r = Unwrap(Eval(driver, "window.__slmw"));



                    if (string.IsNullOrEmpty(r) || r == "PENDING" || r == "null") continue;



                    return r.StartsWith("OK:{");



                }



            }



            catch (Exception) { }



            return false;



        }



        public static bool SendsendVCardContactMessage(object driver, string to, string number, string name)



        {



            try



            {



                if (!EnsureWpp(driver)) return false;



                string num = new string((number ?? "").Where(char.IsDigit).ToArray());



                string vcard = "BEGIN:VCARD\nVERSION:3.0\nN:" + (name ?? num) + "\nTEL;type=CELL;type=VOICE;waid=" + num + ":+" + num + "\nEND:VCARD";



                string chatId = ChatId(to);



                RawEval(driver, "window.__svcw='PENDING';(function(){try{WPP.chat.sendVCardContactMessage(" + JsonConvert.SerializeObject(chatId) + "," + JsonConvert.SerializeObject(vcard) + ").then(function(r){window.__svcw='OK:'+JSON.stringify(r).substring(0,200);}).catch(function(e){window.__svcw='FAIL';});}catch(e){window.__svcw='EX';}})();'x';");



                long end = DateTime.Now.Ticks + 30000 * 10000L;



                while (DateTime.Now.Ticks < end)



                {



                    Thread.Sleep(1500);



                    string r = Unwrap(Eval(driver, "window.__svcw"));



                    if (string.IsNullOrEmpty(r) || r == "PENDING" || r == "null") continue;



                    return r.StartsWith("OK:{");



                }



            }



            catch (Exception) { }



            return false;



        }







        



        



        public static List<WASender.Models.UnReadMessagesModel> GetAllUnreadMessages2(object driver)



        {



            return new List<WASender.Models.UnReadMessagesModel>();



        }







        public static List<WASender.Models.UnReadMessagesModel> _newMessagesBuffer(object driver)



        {



            return new List<WASender.Models.UnReadMessagesModel>();



        }







        



        public static bool OpenAllChat(object driver, System.Collections.IList chunk)



        {



            try



            {



                if (!EnsureWpp(driver)) return false;



                if (chunk == null) return true;



                int n = 0;



                foreach (var c in chunk)



                {



                    try



                    {



                        RawEval(driver, "(function(){try{WPP.chat.openChatBottom(" + JsonConvert.SerializeObject(c != null ? c.ToString() : "") + ").catch(function(){});}catch(e){}})();'x';");



                        Thread.Sleep(800);



                        if (++n > 50) break;



                    }



                    catch (Exception) { }



                }



                return true;



            }



            catch (Exception) { return false; }



        }







        public static async Task<List<WASender.Models.ContactModel>> getMyContactsAsync(object driver)



        {



            return await Task.FromResult(new List<WASender.Models.ContactModel>());



        }



    }



}




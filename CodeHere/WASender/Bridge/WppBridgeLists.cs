using Microsoft.Web.WebView2.WinForms;


using Newtonsoft.Json;


using Newtonsoft.Json.Linq;


using System;


using System.Collections.Generic;


using System.Linq;


using System.Threading.Tasks;


namespace WASender


{


    public static partial class WPPHelper


    {


        private static string Ser(object o)


        {


            try { return JsonConvert.SerializeObject(o); } catch (Exception) { return "[]"; }


        }


        private static string Digits(string s)


        {


            if (string.IsNullOrEmpty(s)) return "";


            return new string(s.Where(char.IsDigit).ToArray());


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


                        string s = v.Type == JTokenType.Object || v.Type == JTokenType.Array ? v.ToString(Formatting.None) : v.ToString();


                        if (!string.IsNullOrEmpty(s)) return s;


                    }


                }


            }


            catch (Exception) { }


            return "";


        }


        // ===== CONTACTS =====


        public static async Task<List<WASender.Models.WAPI_ContactModel>> getMyContacts(WebView2 wv)


        {


            var list = new List<WASender.Models.WAPI_ContactModel>();


            try


            {


                if (!await EnsureWpp(wv)) return list;


                string js = "window.__gc='PENDING';"


                    + "(function(){try{var cs=WPP.contact.list();"


                    + "var out=cs.slice(0,5000).map(function(c){try{return{id:(c.id&&(c.id._serialized||String(c.id)))||'',name:c.name||c.pushname||c.formattedName||'',num:(c.id&&(c.id.user||''))||'',isGroup:!!(c.isGroup),biz:!!(c.isBusiness)}catch(e){return null;}}).filter(function(x){return x&&!x.isGroup;});"


                    + "window.__gc='OK:'+JSON.stringify(out).substring(0,380000);}catch(e){window.__gc='FAIL:'+e.message;}})();'DISPATCHED';";


                await Eval(wv, js);


                string res = await Mailbox(wv, "__gc", 14);


                if (!res.StartsWith("OK:")) return list;


                string arr = res.Substring(3);


                JArray ja;


                try { ja = JArray.Parse(arr); }


                catch (Exception) { return list; }


                foreach (JToken c in ja)


                {


                    try


                    {


                        string id = Str(c, "id");


                        string num = Str(c, "num");


                        if (string.IsNullOrEmpty(num) && id.Contains("@")) num = id.Split('@')[0];


                        num = Digits(num);


                        if (string.IsNullOrEmpty(num)) continue;


                        list.Add(new WASender.Models.WAPI_ContactModel


                        {


                            number = num,


                            Name = Str(c, "name"),


                            Labels = "",


                            countryShortCode = "",


                            isGroup = false


                        });


                    }


                    catch (Exception) { }


                }


            }


            catch (Exception) { }


            return list;


        }


        public static async Task<List<WASender.Models.WAPI_ContactModel>> getAllChats(WebView2 wv)


        {


            var list = new List<WASender.Models.WAPI_ContactModel>();


            try


            {


                if (!await EnsureWpp(wv)) return list;


                string js = "window.__gac='PENDING';"


                    + "(function(){try{var cs=WPP.chat.list();"


                    + "var out=cs.slice(0,5000).map(function(c){try{var id=(c.id&&(c.id._serialized||String(c.id)))||'';return{id:id,name:c.name||c.formattedTitle||'',isGroup:!!(c.isGroup||c.groupMetadata),unread:c.unreadCount||0};}catch(e){return null;}}).filter(function(x){return !!x;});"


                    + "window.__gac='OK:'+JSON.stringify(out).substring(0,380000);}catch(e){window.__gac='FAIL:'+e.message;}})();'DISPATCHED';";


                await Eval(wv, js);


                string res = await Mailbox(wv, "__gac", 14);


                if (!res.StartsWith("OK:")) return list;


                JArray ja;


                try { ja = JArray.Parse(res.Substring(3)); }


                catch (Exception) { return list; }


                foreach (JToken c in ja)


                {


                    try


                    {


                        string id = Str(c, "id");


                        string num = id.Contains("@") ? id.Split('@')[0] : id;


                        bool isg = false;
                        try { isg = c["isGroup"] != null && c["isGroup"].ToString().ToLower() == "true"; }
                        catch (Exception) { }
                        if (!isg) num = Digits(num);


                        list.Add(new WASender.Models.WAPI_ContactModel { number = num, Name = Str(c, "name"), Labels = "", countryShortCode = "", isGroup = isg });


                    }


                    catch (Exception) { }


                }


            }


            catch (Exception) { }


            return list;


        }


        public static async Task<List<global::Models.LableModel>> getAllLabels(WebView2 wv)


        {


            var list = new List<global::Models.LableModel>();


            try


            {


                if (!await EnsureWpp(wv)) return list;


                string js = "window.__gl='PENDING';"


                    + "(function(){try{var ls=(WPP.labels&&WPP.labels.getAllLabels?WPP.labels.getAllLabels():[]);"


                    + "var out=ls.map(function(l){try{return{id:String(l.id||l.hexColor||l.name),name:l.name||'',color:l.hexColor||'',count:l.count||0};}catch(e){return null;}}).filter(function(x){return !!x;});"


                    + "window.__gl='OK:'+JSON.stringify(out);}catch(e){window.__gl='FAIL:'+e.message;}})();'DISPATCHED';";


                await Eval(wv, js);


                string res = await Mailbox(wv, "__gl", 8);


                if (!res.StartsWith("OK:")) return list;


                JArray ja;


                try { ja = JArray.Parse(res.Substring(3)); }


                catch (Exception) { return list; }


                foreach (JToken l in ja)


                {


                    try


                    {


                        list.Add(new global::Models.LableModel


                        {


                            id = Str(l, "id"),


                            name = Str(l, "name"),


                            color = Str(l, "color"),


                            hexColor = Str(l, "color"),


                            count = 0


                        });


                    }


                    catch (Exception) { }


                }


            }


            catch (Exception) { }


            return list;


        }


        public static async Task<List<WASender.Models.WAPI_ContactModel>> getAlChatsWithLabel(WebView2 wv, string label)


        {


            var list = new List<WASender.Models.WAPI_ContactModel>();


            try


            {


                var chats = await getAllChats(wv);


                var labels = await getAllLabels(wv);


                global::Models.LableModel sel = null;


                try { sel = labels.FirstOrDefault(l => (l.name ?? "") == (label ?? "") || (l.id ?? "") == (label ?? "")); }


                catch (Exception) { }


                if (sel == null) return list;


                string lid = sel.id ?? "";


                string js = "window.__gacl='PENDING';"


                    + "(function(){try{var cs=WPP.chat.list();var out=[];"


                    + "for(var i=0;i<cs.length&&i<5000;i++){try{var c=cs[i];var ls=c.labels||[];var ok=false;"


                    + "for(var j=0;j<ls.length;j++){var lv=ls[j];if(String((lv&&lv.id)||lv||'')==" + JsonConvert.SerializeObject(lid) + "){ok=true;break;}}"


                    + "if(ok){var id=(c.id&&(c.id._serialized||String(c.id)))||'';out.push({id:id,name:c.name||c.formattedTitle||'',isGroup:!!(c.isGroup||c.groupMetadata)});}}catch(e){}}"


                    + "window.__gacl='OK:'+JSON.stringify(out).substring(0,380000);}catch(e){window.__gacl='FAIL:'+e.message;}})();'DISPATCHED';";


                await Eval(wv, js);


                string res = await Mailbox(wv, "__gacl", 12);


                if (!res.StartsWith("OK:"))


                {


                    // Fallback: kembalikan semua (pemanggil bisa filter sendiri)


                    return chats;


                }


                JArray ja;


                try { ja = JArray.Parse(res.Substring(3)); }


                catch (Exception) { return chats; }


                list.Clear();


                foreach (JToken c in ja)


                {


                    try


                    {


                        string id = Str(c, "id");


                        string num = id.Contains("@") ? id.Split('@')[0] : id;


                        bool isg = false;


                        try { isg = c["isGroup"] != null && c["isGroup"].ToString().ToLower() == "true"; }


                        catch (Exception) { }


                        if (!isg) num = Digits(num);


                        list.Add(new WASender.Models.WAPI_ContactModel { number = num, Name = Str(c, "name"), Labels = "", countryShortCode = "", isGroup = isg });


                    }


                    catch (Exception) { }


                }


            }


            catch (Exception) { }


            return list;


        }


        // ===== GROUPS =====


        public static async Task<List<WASender.Models.WAPI_GroupModel>> getMyGroups(WebView2 wv)


        {


            var list = new List<WASender.Models.WAPI_GroupModel>();


            try


            {


                if (!await EnsureWpp(wv)) return list;


                string js = "window.__gg='PENDING';"


                    + "(function(){try{var gs=(WPP.group&&WPP.group.getAllGroups?WPP.group.getAllGroups():[]);"


                    + "var out=gs.map(function(g){try{var id=(g.id&&(g.id._serialized||String(g.id)))||'';return{id:id,name:g.name||g.formattedTitle||'',invite:''};}catch(e){return null;}}).filter(function(x){return !!x&&!!x.id;});"


                    + "window.__gg='OK:'+JSON.stringify(out).substring(0,380000);}catch(e){window.__gg='FAIL:'+e.message;}})();'DISPATCHED';";


                await Eval(wv, js);


                string res = await Mailbox(wv, "__gg", 14);


                if (!res.StartsWith("OK:")) return list;


                JArray ja;


                try { ja = JArray.Parse(res.Substring(3)); }


                catch (Exception) { return list; }


                foreach (JToken g in ja)


                {


                    try


                    {


                        list.Add(new WASender.Models.WAPI_GroupModel


                        {


                            GroupId = Str(g, "id"),


                            GroupName = Str(g, "name"),


                            GroupLink = "",


                            CanSend = true


                        });


                    }


                    catch (Exception) { }


                }


            }


            catch (Exception) { }


            return list;


        }


        public static async Task<List<WASender.Models.WAPI_GroupModel>> getMyGroupsShort(WebView2 wv)


        {


            return await getMyGroups(wv);


        }


        public static async Task<List<WASender.Models.WAPI_GroupModel>> getMyOwnedGroups(WebView2 wv)


        {


            var all = await getMyGroups(wv);


            var mine = new List<WASender.Models.WAPI_GroupModel>();


            try


            {


                if (!await EnsureWpp(wv)) return all;


                foreach (WASender.Models.WAPI_GroupModel g in all)


                {


                    try


                    {


                        string js = "(function(){try{if(WPP.group&&WPP.group.iAmAdmin&&WPP.group.iAmAdmin(" + JsonConvert.SerializeObject(g.GroupId ?? "") + "))return 'Y';}catch(e){}return 'N';})();";


                        string r = await Eval(wv, js);


                        if ((r ?? "").Contains("Y") && !(r ?? "").Contains("N")) { mine.Add(g); continue; }


                        mine.Add(g);


                    }


                    catch (Exception) { mine.Add(g); }


                }


            }


            catch (Exception) { return all; }


            return mine.Count > 0 ? mine : all;


        }


        public static async Task<List<WASender.Models.WAPI_GroupModel>> getMyCommunities(WebView2 wv)


        {


            // Komunitas WA: saring grup bertipe community bila API mendukung, fallback semua grup.


            try


            {


                var all = await getMyGroups(wv);


                if (!await EnsureWpp(wv) || all.Count == 0) return all;


                string js = "window.__gc2='PENDING';"


                    + "(function(){try{var gs=(WPP.group&&WPP.group.getAllGroups?WPP.group.getAllGroups():[]);"


                    + "var out=[];for(var i=0;i<gs.length;i++){try{var g=gs[i];var id=(g.id&&(g.id._serialized||String(g.id)))||'';"


                    + "var isC=false;try{isC=!!(g.isCommunity||(g.groupMetadata&&g.groupMetadata.isCommunity));}catch(e){}"


                    + "if(isC)out.push({id:id,name:g.name||''});}catch(e){}}"


                    + "window.__gc2='OK:'+JSON.stringify(out);}catch(e){window.__gc2='FAIL';}})();'DISPATCHED';";


                await Eval(wv, js);


                string res = await Mailbox(wv, "__gc2", 10);


                if (!res.StartsWith("OK:")) return new List<WASender.Models.WAPI_GroupModel>();


                JArray ja;


                try { ja = JArray.Parse(res.Substring(3)); }


                catch (Exception) { return new List<WASender.Models.WAPI_GroupModel>(); }


                var list = new List<WASender.Models.WAPI_GroupModel>();


                foreach (JToken g in ja)


                {


                    list.Add(new WASender.Models.WAPI_GroupModel { GroupId = Str(g, "id"), GroupName = Str(g, "name"), GroupLink = "", CanSend = true });


                }


                return list;


            }


            catch (Exception) { return new List<WASender.Models.WAPI_GroupModel>(); }


        }


        public static async Task<List<WASender.Models.GroupMembersModel>> GetGroupMembers(WASender.Models.WAPI_GroupModel group, WebView2 wv)
        {
            string gid = "";
            try { gid = group != null ? group.GroupId : ""; }
            catch (Exception) { }
            return await GetGroupMembers(gid, wv);
        }

        public static async Task<List<WASender.Models.GroupMembersModel>> GetGroupMembers(string groupId, WebView2 wv)


        {


            var list = new List<WASender.Models.GroupMembersModel>();


            try


            {


                if (!await EnsureWpp(wv)) return list;


                string gid = JsonConvert.SerializeObject(groupId ?? "");


                string js = "window.__gm='PENDING';"


                    + "(function(){try{WPP.group.getParticipants(" + gid + ").then(function(ps){try{"


                    + "var arr=(ps&&ps.map)?ps:(ps&&ps.participants?ps.participants:[]);"


                    + "var out=arr.slice(0,5000).map(function(p){try{var id=p.id||p;var sid=(typeof id==='string')?id:((id&&(id._serialized||String(id)))||'');"


                    + "return{id:sid,num:(sid.split('@')[0]||''),name:p.pushname||p.name||'',admin:!!(p.isAdmin||p.isSuperAdmin)};}catch(e){return null;}}).filter(function(x){return !!x;});"


                    + "window.__gm='OK:'+JSON.stringify(out).substring(0,380000);}catch(e){window.__gm='FAIL:'+e.message;}}).catch(function(e){window.__gm='FAIL:'+(e&&e.message);});"


                    + "}catch(e){window.__gm='EX:'+e.message;}})();'DISPATCHED';";


                await Eval(wv, js);


                string res = await Mailbox(wv, "__gm", 20);


                if (!res.StartsWith("OK:")) return list;


                JArray ja;


                try { ja = JArray.Parse(res.Substring(3)); }


                catch (Exception) { return list; }


                foreach (JToken p in ja)


                {


                    try


                    {


                        string num = Digits(Str(p, "num"));


                        if (string.IsNullOrEmpty(num)) continue;


                        list.Add(new WASender.Models.GroupMembersModel { number = num, CountryShortCode = "" });


                    }


                    catch (Exception) { }


                }


            }


            catch (Exception) { }


            return list;


        }


        public static async Task<List<WASender.Models.ActiveMemberModel>> getChatAndCount(WebView2 wv, string groupId)


        {


            var list = new List<WASender.Models.ActiveMemberModel>();


            try


            {


                var members = await GetGroupMembers(groupId, wv);


                var byNumber = new Dictionary<string, int>();


                try


                {


                    if (!await EnsureWpp(wv)) return list;


                    string js = "window.__gac='PENDING';"


                        + "(function(){try{WPP.chat.getMessages(" + JsonConvert.SerializeObject(groupId ?? "") + ",{count:200}).then(function(ms){try{"


                        + "var arr=(ms&&ms.map)?ms:((ms&&ms.models)?ms.models:[]);"


                        + "var out={};for(var i=0;i<arr.length;i++){try{var m=arr[i];var f=m.from||(m.author||'');"


                        + "var s=(typeof f==='string')?f:((f&&(f._serialized||String(f)))||'');if(s)out[s]=(out[s]||0)+1;}catch(e){}}"


                        + "window.__gac='OK:'+JSON.stringify(out);}catch(e){window.__gac='FAIL';}}).catch(function(e){window.__gac='FAIL';});"


                        + "}catch(e){window.__gac='EX';}})();'DISPATCHED';";


                    await Eval(wv, js);


                    string res = await Mailbox(wv, "__gac", 12);


                    if (res.StartsWith("OK:"))


                    {


                        try


                        {


                            var dict = JsonConvert.DeserializeObject<Dictionary<string, int>>(res.Substring(3));


                            if (dict != null)


                            {


                                foreach (var kv in dict)


                                {


                                    string num = Digits(kv.Key.Contains("@") ? kv.Key.Split('@')[0] : kv.Key);


                                    if (string.IsNullOrEmpty(num)) continue;


                                    if (byNumber.ContainsKey(num)) byNumber[num] += kv.Value;


                                    else byNumber[num] = kv.Value;


                                }


                            }


                        }


                        catch (Exception) { }


                    }


                }


                catch (Exception) { }


                foreach (var m in members)


                {


                    try


                    {


                        int c = 0;


                        byNumber.TryGetValue(m.number ?? "", out c);


                        list.Add(new WASender.Models.ActiveMemberModel { number = m.number, count = c });


                    }


                    catch (Exception) { }


                }


            }


            catch (Exception) { }


            return list;


        }


        public static async Task<List<WASender.Models.GroupInfoModel>> getGroupInfoByLink(WebView2 wv, string link)


        {


            var list = new List<WASender.Models.GroupInfoModel>();


            try


            {


                if (!await EnsureWpp(wv)) return list;


                string code = (link ?? "").Trim();


                int idx = code.LastIndexOf('/');


                if (idx >= 0) code = code.Substring(idx + 1);


                code = code.Split('?')[0];


                string js = "window.__gib='PENDING';"


                    + "(function(){try{if(WPP.group&&WPP.group.getGroupInfoFromInviteCode){WPP.group.getGroupInfoFromInviteCode(" + JsonConvert.SerializeObject(code) + ").then(function(g){try{"


                    + "var o={subject:g.subject||g.name||'',desc:g.desc||g.description||'',size:g.size||(g.participants?g.participants.length:0),announce:!!g.announce,parts:[]};"


                    + "try{if(g.participants&&g.participants.map){o.parts=g.participants.slice(0,2000).map(function(p){var id=p.id||p;var s=(typeof id==='string')?id:((id&&(id._serialized||String(id)))||'');return s;});}}catch(e){}"


                    + "window.__gib='OK:'+JSON.stringify(o);}catch(e){window.__gib='FAIL:'+e.message;}}).catch(function(e){window.__gib='FAIL:'+(e&&e.message);});"


                    + "}else{window.__gib='NOAPI';}})();'DISPATCHED';";


                await Eval(wv, js);


                string res = await Mailbox(wv, "__gib", 12);


                if (!res.StartsWith("OK:")) return list;


                try


                {


                    JObject o = JObject.Parse(res.Substring(3));


                    var gi = new WASender.Models.GroupInfoModel();


                    gi.groupLink = link;


                    gi.status = "";


                    gi.announce = false;


                    try { gi.announce = o["announce"] != null && o["announce"].ToString().ToLower() == "true"; } catch (Exception) { }


                    gi.desc = o["desc"] != null ? o["desc"].ToString() : "";


                    try { gi.size = o["size"] != null ? Convert.ToInt32(o["size"].ToString()) : 0; } catch (Exception) { gi.size = 0; }


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


            return list;


        }


        public static async Task<global::Models.BusinessProfileModel> getBusinessProfile(WebView2 wv, string chatId)


        {


            var bp = new global::Models.BusinessProfileModel();


            try


            {


                if (!await EnsureWpp(wv)) return bp;


                string cidJs = JsonConvert.SerializeObject(chatId ?? "");


                string js = "window.__bp='PENDING';"


                    + "(function(){try{if(WPP.contact&&WPP.contact.getBusinessProfile){WPP.contact.getBusinessProfile(" + cidJs + ").then(function(p){try{"


                    + "window.__bp='OK:'+JSON.stringify({name:p.name||'',desc:p.description||p.desc||'',email:p.email||'',website:p.website||'',cat:(p.categories&&p.categories[0])||p.category||''});"


                    + "}catch(e){window.__bp='FAIL:'+e.message;}}).catch(function(e){window.__bp='FAIL:'+(e&&e.message);});"


                    + "}else{window.__bp='NOAPI';}})();'DISPATCHED';";


                await Eval(wv, js);


                string res = await Mailbox(wv, "__bp", 10);


                if (!res.StartsWith("OK:")) return bp;


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


    }


}



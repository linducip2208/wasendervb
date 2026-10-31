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








        // ===== grup: buat / gabung / tambah anggota =====








        public static async Task<string> CreateGroup(WebView2 wv, string name, string defaultNumber)








        {








            try








            {








                if (!await EnsureWpp(wv)) return "";








                string digits = new string((defaultNumber ?? "").Where(char.IsDigit).ToArray());








                string js = "window.__cg='PENDING';"








                    + "(function(){try{"








                    + "var parts=" + (string.IsNullOrEmpty(digits) ? "[]" : "[" + JsonConvert.SerializeObject(digits + "@c.us") + "]") + ";"








                    + "WPP.group.create(" + JsonConvert.SerializeObject(name ?? "New Group") + ",parts)"








                    + ".then(function(r){try{var g=r&&r.gid?r.gid:(r&&r.id?r.id:r);window.__cg='OK:'+JSON.stringify(g).substring(0,120);}catch(e){window.__cg='OK';}})"








                    + ".catch(function(e){window.__cg='FAIL:'+((e&&e.message)||e);});"








                    + "}catch(e){window.__cg='EX:'+e.message;}})();'DISPATCHED';";








                await Eval(wv, js);








                string res = await Mailbox(wv, "__cg", 20);








                return res.StartsWith("OK") ? res : "";








            }








            catch (Exception) { return ""; }








        }

















        public static async Task<bool> JoinGroupAsync(WebView2 wv, string code)








        {








            try








            {








                if (!await EnsureWpp(wv)) return false;








                string c = (code ?? "").Trim();








                int idx = c.LastIndexOf('/');








                if (idx >= 0) c = c.Substring(idx + 1);








                c = c.Split('?')[0];








                string js = "window.__jg='PENDING';"








                    + "(function(){try{WPP.group.join(" + JsonConvert.SerializeObject(c) + ")"








                    + ".then(function(){window.__jg='OK';}).catch(function(e){window.__jg='FAIL:'+(e&&e.message);});"








                    + "}catch(e){window.__jg='EX:'+e.message;}})();'DISPATCHED';";








                await Eval(wv, js);








                string res = await Mailbox(wv, "__jg", 20);








                if (!res.StartsWith("OK")) { GroupJoinFaildReson = res; return false; }








                return true;








            }








            catch (Exception ex) { GroupJoinFaildReson = ex.Message; return false; }








        }

















        public static async Task<string> addParticipants(WebView2 wv, string groupId, string number, bool isAdmin, string msg)








        {








            try








            {








                if (!await EnsureWpp(wv)) return "wpp-missing";








                string digits = new string((number ?? "").Where(char.IsDigit).ToArray());








                string js = "window.__ap='PENDING';"








                    + "(function(){try{WPP.group.addParticipants(" + JsonConvert.SerializeObject(groupId ?? "") + "," + JsonConvert.SerializeObject(new string[] { digits + "@c.us" }) + ")"








                    + ".then(function(r){window.__ap='OK:'+JSON.stringify(r).substring(0,200);}).catch(function(e){window.__ap='FAIL:'+(e&&e.message);});"








                    + "}catch(e){window.__ap='EX:'+e.message;}})();'DISPATCHED';";








                await Eval(wv, js);








                string res = await Mailbox(wv, "__ap", 20);








                return res.StartsWith("OK") ? "OK" : res;








            }








            catch (Exception ex) { return "EX:" + ex.Message; }








        }

















        private static string NormChat(string c)


        {


            try


            {


                string s = (c ?? "").Trim();


                if (s == "") return "";


                if (s.Contains("@")) return s;


                string d = new string(s.Where(char.IsDigit).ToArray());


                if (d == "") return s;


                return d + "@c.us";


            }


            catch (Exception) { return c ?? ""; }


        }





        public static async Task<bool> openAllchat(WebView2 wv, List<string> chunk)








        {








            try








            {








                if (!await EnsureWpp(wv)) return false;








                foreach (string c in (chunk ?? new List<string>()).Take(50))








                {








                    try








                    {








                        await Eval(wv, "try{WPP.chat.openChatBottom(" + JsonConvert.SerializeObject(NormChat(c)) + ").catch(function(){});}catch(e){}");








                        await Task.Delay(800);








                    }








                    catch (Exception) { }








                }








                return true;








            }








            catch (Exception) { return false; }








        }

















        public static async Task<string> GetGroupInviteLink(WebView2 wv, string groupId)
        {
            try
            {
                if (!await EnsureWpp(wv)) return "";
                string gid = (groupId ?? "").Contains("@") ? groupId : groupId + "@g.us";
                string js = "window.__gil='PENDING';(function(){try{WPP.group.getInviteCode(" + JsonConvert.SerializeObject(gid) + ").then(function(code){try{var s=(typeof code==='string')?code:((code&&(code.code||code.inviteCode))||'');s=String(s||'');window.__gil=s.indexOf('http')===0?('OK:'+s):(s?('OK:https://chat.whatsapp.com/'+s):'FAIL');}catch(e){window.__gil='FAIL';}}).catch(function(e){window.__gil='FAIL';});}catch(e){window.__gil='EX';}})();'DISPATCHED';";
                await Eval(wv, js);
                string res = await Mailbox(wv, "__gil", 12);
                if (res != null && res.StartsWith("OK:http")) return res.Substring(3);
                return "";
            }
            catch (Exception) { return ""; }
        }

        // ===== polling hasil =====








                public static async Task<List<global::Models.PollReportModel>> GetPollResults(WebView2 wv)


        {


            var outList = new List<global::Models.PollReportModel>();


            try


            {


                if (!await EnsureWpp(wv)) return outList;


            string js = "window.__pr2='PENDING';"


                + "(function(){"


                + "function sid(x){try{if(!x)return '';if(typeof x==='string')return x;return (x._serialized||x.id||String(x));}catch(e){return '';}}"


                + "WPP.chat.list({onlyUsers:false}).then(function(cs){"


                + "var jobs=cs.slice(0,30).map(function(c){var id=sid(c.id);"


                + "return WPP.chat.getMessages(id,{count:30}).then(function(ms){"


                + "var arr=(ms&&ms.map)?ms:((ms&&ms.models)?ms.models:[]);var polls=[];"


                + "for(var i=0;i<arr.length;i++){try{var m=arr[i];"


                + "if(m&&m.type==='poll_creation'&&m.id){var mid=sid(m.id);var nm=m.pollName||m.body||'';var opts=[];try{var po=m.pollOptions||[];for(var k=0;k<po.length;k++){opts.push({name:po[k].name||'',localId:po[k].localId});}}catch(e){}"


                + "polls.push({chat:id,msg:mid,name:nm,opts:opts});}}catch(e){}}"


                + "return polls;}).catch(function(){return [];});});"


                + "Promise.all(jobs).then(function(all){var flat=[];for(var i=0;i<all.length;i++){flat=flat.concat(all[i]);}flat=flat.slice(0,20);"


                + "var vjobs=flat.map(function(p){"


                + "return WPP.chat.getVotes(p.msg).then(function(v){var vv=[];try{var vs=v&&v.votes?v.votes:[];for(var i=0;i<vs.length;i++){var s=vs[i];var on=[];try{var so=s.selectedOptions||[];for(var k=0;k<so.length;k++){on.push(so[k].name||'');}}catch(e){}vv.push({sender:sid(s.sender),opts:on});}}catch(e){}p.votes=vv;return p;}).catch(function(){p.votes=[];return p;});});"


                + "Promise.all(vjobs).then(function(done){"


                + "var pjobs=done.map(function(p){try{"


                + "if(p.chat&&p.chat.indexOf('@g.us')>-1&&WPP.group&&WPP.group.getParticipants){"


                + "return WPP.group.getParticipants(p.chat).then(function(ps){var out=[];var arr=(ps&&ps.map)?ps:(ps&&ps.participants?ps.participants:[]);for(var i=0;i<arr.length;i++){try{var pp=arr[i];var pid=pp.id||pp;out.push(sid(pid));}catch(e){}}p.parts=out;return p;}).catch(function(){p.parts=[];return p;});}"


                + "else{p.parts=[p.chat];return Promise.resolve(p);}}catch(e){p.parts=[];try{return Promise.resolve(p);}catch(ee){return p;}}});"


                + "Promise.all(pjobs).then(function(fin){window.__pr2='OK:'+JSON.stringify(fin).substring(0,500000);}).catch(function(e){window.__pr2='FAIL';});"


                + "}).catch(function(e){window.__pr2='FAIL';});"


                + "}).catch(function(e){window.__pr2='FAIL';});"


                + "}).catch(function(e){window.__pr2='FAIL';});"


                + "})();'DISPATCHED';";


                await Eval(wv, js);


                string res = await Mailbox(wv, "__pr2", 60);


                if (!res.StartsWith("OK:")) return outList;


                JArray ja;


                try { ja = JArray.Parse(res.Substring(3)); }


                catch (Exception) { return outList; }


            foreach (JToken p in ja)


            {


                try


                {


                    var model = new global::Models.PollReportModel


                    {


                        PollName = Str(p, "name"),


                        options = new List<WaSender.Models.PollOptionsReturnModel>(),


                        unreactedList = new List<global::Models.SelectedVotesReturnModel>()


                    };


                    var voters = new System.Collections.Generic.HashSet<string>();


                    try


                    {


                        var votes = p["votes"] as JArray;


                        var opts = p["opts"] as JArray;


                        if (opts != null)


                        {


                            foreach (JToken o in opts)


                            {


                                string oname = Str(o, "name");


                                var sv = new System.Collections.Generic.List<global::Models.SelectedVotesReturlModel>();


                                if (votes != null)


                                {


                                    foreach (JToken v in votes)


                                    {


                                        try


                                        {


                                            var oo = v["opts"] as JArray;


                                            bool hit = false;


                                            if (oo != null) { foreach (JToken x in oo) { if ((x != null ? x.ToString() : "") == oname) { hit = true; break; } } }


                                            if (hit)


                                            {


                                                string snd = Str(v, "sender");


                                                if (!string.IsNullOrEmpty(snd)) voters.Add(snd);


                                                sv.Add(new global::Models.SelectedVotesReturlModel { sender = snd });


                                            }


                                        }


                                        catch (Exception) { }


                                    }


                                }


                                model.options.Add(new WaSender.Models.PollOptionsReturnModel { name = oname, selectedVotesList = sv });


                            }


                        }


                    }


                    catch (Exception) { }


                    try


                    {


                        var parts = p["parts"] as JArray;


                        if (parts != null)


                        {


                            foreach (JToken x in parts)


                            {


                                string pid = x != null ? x.ToString() : "";


                                if (!string.IsNullOrEmpty(pid) && !voters.Contains(pid))


                                    model.unreactedList.Add(new global::Models.SelectedVotesReturnModel { chatId = pid });


                            }


                        }


                    }


                    catch (Exception) { }


                    outList.Add(model);


                }


                catch (Exception) { }


            }


            }


            catch (Exception) { }


            return outList;


        }




















        // ===== autoreply: buffer pesan masuk (polling) + live push =====








        public static void Registerpoll_response(WebView2 wv) { try { var _ = EnsureLivePush(wv); } catch (Exception) { } }

















        public static void onNewMessage(WebView2 wv) { try { var _ = EnsureLivePush(wv); } catch (Exception) { } }

















        private static bool _livePushInstalled = false;








        private static async Task EnsureLivePush(WebView2 wv)








        {








            try








            {








                if (_livePushInstalled) return;








                if (!await EnsureWpp(wv)) return;








                string js = "(function(){try{"








                    + "if(window.__liveOn) return 'ON'; window.__liveOn=true;"








                    + "var push=function(type,obj){try{if(window.chrome&&window.chrome.webview){window.chrome.webview.postMessage(JSON.stringify({type:type,_data:JSON.stringify(obj)}));}}catch(e){}};"








                    + "if(window.WPP&&WPP.on){"








                    + "WPP.on('chat.new_message',function(m){try{if(m&&m.id&&!m.id.fromMe&&!(m.id.remote&&String(m.id.remote).indexOf('@g.us')>-1)){push('newmsg',{id:{fromMe:!!m.id.fromMe,_serialized:String(m.id._serialized||'')},body:m.body||m.caption||'',from:String((m.from||m.chatId||'')),timestamp:m.t||0,isGroup:false});}}catch(e){}});"








                    + "}"








                    + "return 'ON';}catch(e){return 'ERR';}})();";








                await Eval(wv, js);








                _livePushInstalled = true;








            }








            catch (Exception) { }








        }

















        private static DateTime _unreadSince = DateTime.Now.AddMinutes(-30);

















        public static List<WASender.Models.UnReadMessagesModel> GetAllUnreadMessages2(WebView2 wv)








        {








            var list = new List<WASender.Models.UnReadMessagesModel>();








            try








            {








                var t = GetAllUnreadMessages2Async(wv);








                t.Wait(TimeSpan.FromSeconds(45));








                list = t.Result ?? list;








                _unreadSince = DateTime.Now;








            }








            catch (Exception) { }








            return list;








        }

















        public static List<WASender.Models.UnReadMessagesModel> GetMessagesBuffer(WebView2 wv)








        {








            return new List<WASender.Models.UnReadMessagesModel>();








        }

















        private static async Task<List<WASender.Models.UnReadMessagesModel>> GetAllUnreadMessages2Async(WebView2 wv)








        {








            var list = new List<WASender.Models.UnReadMessagesModel>();








            try








            {








                if (!await EnsureWpp(wv)) return list;








                string js = "window.__um='PENDING';"








                    + "(function(){try{WPP.chat.getUnreadChats().then(function(cs){try{"








                    + "var jobs=cs.slice(0,25).map(function(c){var id=(c.id&&(c.id._serialized||String(c.id)))||'';"








                    + "return WPP.chat.getMessages(id,{count:15}).then(function(ms){"








                    + "var arr=(ms&&ms.map)?ms:((ms&&ms.models)?ms.models:[]);var out=[];"








                    + "for(var i=0;i<arr.length;i++){try{var m=arr[i];if(m&&m.id&&!m.id.fromMe&&m.t){out.push({id:String(m.id._serialized||''),body:m.body||m.caption||'',chatId:id,isPoll:(m.type==='poll'),t:m.t});}}catch(e){}}"








                    + "return out;}).catch(function(){return [];});});"








                    + "Promise.all(jobs).then(function(all){var flat=[];for(var i=0;i<all.length;i++){flat=flat.concat(all[i]);}window.__um='OK:'+JSON.stringify(flat.slice(0,200));}).catch(function(e){window.__um='FAIL';});"








                    + "}catch(e){window.__um='ERR:'+e.message;}}).catch(function(e){window.__um='FAIL';});"








                    + "}catch(e){window.__um='EX:'+e.message;}})();'DISPATCHED';";








                await Eval(wv, js);








                string res = await Mailbox(wv, "__um", 25);








                if (!res.StartsWith("OK:")) return list;








                JArray ja;








                try { ja = JArray.Parse(res.Substring(3)); }








                catch (Exception) { return list; }








                long since = 0;








                try { since = (long)(_unreadSince.ToUniversalTime() - new DateTime(1970, 1, 1)).TotalSeconds; } catch (Exception) { }








                foreach (JToken m in ja)








                {








                    try








                    {








                        long t = 0;








                        try { t = Convert.ToInt64(m["t"] != null ? m["t"].ToString() : "0"); }








                        catch (Exception) { }








                        if (t < since) continue;








                        bool isp = false;








                        try { isp = m["isPoll"] != null && m["isPoll"].ToString().ToLower() == "true"; }








                        catch (Exception) { }








                        list.Add(new WASender.Models.UnReadMessagesModel








                        {








                            id = m["id"] != null ? m["id"].ToString() : "",








                            body = m["body"] != null ? m["body"].ToString() : "",








                            chatId = m["chatId"] != null ? m["chatId"].ToString() : "",








                            isPoll = isp








                        });








                    }








                    catch (Exception) { }








                }








            }








            catch (Exception) { }








            return list;








        }

















        // ===== tombol (button message) -> kirim sebagai teks + daftar opsi =====








        private static string BtnProp(object o, string n)
        {
            try
            {
                var p = o.GetType().GetProperty(n);
                if (p == null) return null;
                var v = p.GetValue(o, null);
                return v != null ? v.ToString() : null;
            }
            catch (Exception) { return null; }
        }

        private static void BtnAdd(System.Collections.Generic.List<System.Collections.Generic.Dictionary<string, string>> out_, object b)
        {
            try
            {
                string id = BtnProp(b, "id") ?? BtnProp(b, "Id") ?? "";
                string tx = BtnProp(b, "text") ?? BtnProp(b, "Text") ?? BtnProp(b, "ButtonText") ?? BtnProp(b, "buttonText") ?? BtnProp(b, "Title") ?? BtnProp(b, "title") ?? "";
                string url = BtnProp(b, "url") ?? BtnProp(b, "Url") ?? "";
                string ph = BtnProp(b, "phoneNumber") ?? BtnProp(b, "PhoneNumber") ?? "";
                if (string.IsNullOrWhiteSpace(tx) && string.IsNullOrWhiteSpace(url) && string.IsNullOrWhiteSpace(ph)) return;
                var d = new System.Collections.Generic.Dictionary<string, string>();
                if (!string.IsNullOrWhiteSpace(url)) { d["url"] = url; d["text"] = tx; }
                else if (!string.IsNullOrWhiteSpace(ph)) { d["phoneNumber"] = ph; d["text"] = tx; }
                else { d["id"] = string.IsNullOrEmpty(id) ? Guid.NewGuid().ToString("N").Substring(0, 8) : id; d["text"] = tx; }
                out_.Add(d);
            }
            catch (Exception) { }
        }

        private static System.Collections.Generic.List<System.Collections.Generic.Dictionary<string, string>> ExtractButtons(object model)
        {
            var out_ = new System.Collections.Generic.List<System.Collections.Generic.Dictionary<string, string>>();
            try
            {
                if (model == null) return out_;
                var bp = model.GetType().GetProperty("buttons");
                if (bp == null) return out_;
                var items = bp.GetValue(model, null) as System.Collections.IEnumerable;
                if (items == null) return out_;
                foreach (var it in items)
                {
                    if (it == null || out_.Count >= 3) break;
                    System.Collections.IEnumerable inner = null;
                    try
                    {
                        var ip = it.GetType().GetProperty("buttons");
                        if (ip != null) inner = ip.GetValue(it, null) as System.Collections.IEnumerable;
                    }
                    catch (Exception) { }
                    if (inner != null)
                    {
                        foreach (var x in inner)
                        {
                            if (out_.Count >= 3 || x == null) break;
                            BtnAdd(out_, x);
                        }
                        continue;
                    }
                    BtnAdd(out_, it);
                }
            }
            catch (Exception) { }
            return out_;
        }

        public static void sendButtonWithMessage(object driverOrWv, object model, string to, string text)








        {








            try








            {








                WebView2 wv = driverOrWv as WebView2;








                if (wv == null) return;








                var btns = ExtractButtons(model);
                if (btns.Count > 0)
                {
                    var _ = System.Threading.Tasks.Task.Run(async () =>
                    {
                        try
                        {
                            if (!await EnsureWpp(wv)) return;
                            string mbox = "__sbtn" + Guid.NewGuid().ToString("N").Substring(0, 6);
                            string js = "window." + mbox + "='PENDING';"
                                + "WPP.chat.sendTextMessage(" + JsonConvert.SerializeObject(ChatId(to)) + "," + JsonConvert.SerializeObject(text ?? "")
                                + ",{buttons:" + JsonConvert.SerializeObject(btns) + ",waitForAck:true})"
                                + ".then(function(r){window." + mbox + "='OK:'+JSON.stringify(r).substring(0,300);}).catch(function(e){window." + mbox + "='FAIL:'+((e&&e.message)||e);});'DISPATCHED';";
                            await Eval(wv, js);
                            string res = await Mailbox(wv, mbox, 20);
                            if (res != null && res.StartsWith("OK:{")) return;
                        }
                        catch (Exception) { }
                        try { await WppSendTextNoWait(wv, to, text ?? "", "__sbf" + Guid.NewGuid().ToString("N").Substring(0, 6)); } catch (Exception) { }
                    });
                    return;
                }
                var _2 = WppSendTextNoWait(wv, to, text ?? "", "__sbtn" + Guid.NewGuid().ToString("N").Substring(0, 6));








            }








            catch (Exception) { }








        }

















        public static void sendButtonWithMessageToGroup(WebView2 wv, object model, string groupId, string text)








        {








            try








            {








                List<string> opts = new List<string>();








                try








                {








                    dynamic dm0 = model;


                    if (model != null && dm0.buttons != null)








                    {








                        foreach (var b in dm0.buttons)








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








                    }








                }








                catch (Exception) { }








                string body = text ?? "";








                if (opts.Count > 0) body = body + "\n" + string.Join(" / ", opts.ToArray());








                var gbtns = ExtractButtons(model);
                if (gbtns.Count > 0)
                {
                    string _gid = (groupId ?? "").Contains("@") ? groupId : groupId + "@g.us";
                    var _g = System.Threading.Tasks.Task.Run(async () =>
                    {
                        try
                        {
                            if (!await EnsureWpp(wv)) return;
                            string mbox = "__sbg" + Guid.NewGuid().ToString("N").Substring(0, 6);
                            string js = "window." + mbox + "='PENDING';"
                                + "WPP.chat.sendTextMessage(" + JsonConvert.SerializeObject(_gid) + "," + JsonConvert.SerializeObject(text ?? "")
                                + ",{buttons:" + JsonConvert.SerializeObject(gbtns) + ",waitForAck:true})"
                                + ".then(function(r){window." + mbox + "='OK:'+JSON.stringify(r).substring(0,300);}).catch(function(e){window." + mbox + "='FAIL:'+((e&&e.message)||e);});'DISPATCHED';";
                            await Eval(wv, js);
                            string res = await Mailbox(wv, mbox, 20);
                            if (res != null && res.StartsWith("OK:{")) return;
                        }
                        catch (Exception) { }
                        try { await WppSendTextNoWait(wv, groupId, body, "__sbfg" + Guid.NewGuid().ToString("N").Substring(0, 6)); } catch (Exception) { }
                    });
                    return;
                }
                var _ = WppSendTextNoWait(wv, groupId, body, "__sbtn" + Guid.NewGuid().ToString("N").Substring(0, 6));








            }








            catch (Exception) { }








        }

















        private static async Task WppSendTextNoWait(WebView2 wv, string chatIdOrNumber, string text, string mailbox)








        {








            try








            {








                if (!await EnsureWpp(wv)) return;








                string chatId = chatIdOrNumber ?? "";








                if (!chatId.Contains("@"))








                {








                    string d = new string(chatId.Where(char.IsDigit).ToArray());








                    chatId = d + "@c.us";








                }








                string js = "window." + mailbox + "='PENDING';"








                    + "WPP.chat.sendTextMessage(" + JsonConvert.SerializeObject(chatId) + "," + JsonConvert.SerializeObject(text ?? "")








                    + ").then(function(r){window." + mailbox + "='OK';}).catch(function(e){window." + mailbox + "='FAIL';});'DISPATCHED';";








                await Eval(wv, js);








                await Mailbox(wv, mailbox, 12);








            }








            catch (Exception) { }








        }








    }








}









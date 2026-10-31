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





        public static async Task<bool> openAllchat(WebView2 wv, List<string> chunk)


        {


            try


            {


                if (!await EnsureWpp(wv)) return false;


                foreach (string c in (chunk ?? new List<string>()).Take(50))


                {


                    try


                    {


                        await Eval(wv, "try{WPP.chat.openChatBottom(" + JsonConvert.SerializeObject(c ?? "") + ").catch(function(){});}catch(e){}");


                        await Task.Delay(800);


                    }


                    catch (Exception) { }


                }


                return true;


            }


            catch (Exception) { return false; }


        }





        // ===== polling hasil =====


        public static async Task<List<global::Models.PollReportModel>> GetPollResults(WebView2 wv)


        {


            var outList = new List<global::Models.PollReportModel>();


            try


            {


                if (!await EnsureWpp(wv)) return outList;


                string js = "window.__pr='PENDING';"


                    + "(function(){try{WPP.chat.list({onlyUsers:false}).then(function(cs){try{"


                    + "var jobs=cs.slice(0,30).map(function(c){var id=(c.id&&(c.id._serialized||String(c.id)))||'';"


                    + "return WPP.chat.getMessages(id,{count:30}).then(function(ms){"


                    + "var arr=(ms&&ms.map)?ms:((ms&&ms.models)?ms.models:[]);"


                    + "var polls=[];"


                    + "for(var i=0;i<arr.length;i++){try{var m=arr[i];if(m&&(m.type==='poll'||m.type==='poll_vote')&&m.id&&!m.id.fromMe){"


                    + "polls.push({chat:id,msg:(m.id&&(m.id._serialized||String(m.id)))||'',name:m.pollName||m.body||'',opts:[]});}}catch(e){}}"


                    + "return polls;}).catch(function(){return [];});});"


                    + "Promise.all(jobs).then(function(all){var flat=[];for(var i=0;i<all.length;i++){flat=flat.concat(all[i]);}window.__pr='OK:'+JSON.stringify(flat.slice(0,50));}).catch(function(e){window.__pr='FAIL';});"


                    + "}catch(e){window.__pr='ERR:'+e.message;}}).catch(function(e){window.__pr='FAIL';});"


                    + "}catch(e){window.__pr='EX:'+e.message;}})();'DISPATCHED';";


                await Eval(wv, js);


                string res = await Mailbox(wv, "__pr", 30);


                if (!res.StartsWith("OK:")) return outList;


                JArray ja;


                try { ja = JArray.Parse(res.Substring(3)); }


                catch (Exception) { return outList; }


                var byPoll = new Dictionary<string, global::Models.PollReportModel>();


                foreach (JToken p in ja)


                {


                    try


                    {


                        string msgId = Str(p, "msg");


                        string chat = Str(p, "chat");


                        string key = chat + "|" + msgId;


                        if (!byPoll.ContainsKey(key))


                        {


                            byPoll[key] = new global::Models.PollReportModel


                            {


                                PollName = Str(p, "name"),


                                options = new List<WaSender.Models.PollOptionsReturnModel>(),


                                unreactedList = new List<global::Models.SelectedVotesReturnModel>()


                            };


                        }


                    }


                    catch (Exception) { }


                }


                outList.AddRange(byPoll.Values);


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


        public static void sendButtonWithMessage(object driverOrWv, object model, string to, string text)


        {


            try


            {


                WebView2 wv = driverOrWv as WebView2;


                if (wv == null) return;


                var _ = WppSendTextNoWait(wv, to, text ?? "", "__sbtn" + Guid.NewGuid().ToString("N").Substring(0, 6));


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



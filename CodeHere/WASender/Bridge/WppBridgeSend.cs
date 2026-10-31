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
        private static string ChatId(object to)
        {
            string s = (to ?? "").ToString();
            if (string.IsNullOrEmpty(s)) return "";
            if (s.Contains("@")) return s;
            return new string(s.Where(char.IsDigit).ToArray()) + "@c.us";
        }

        private static async Task<Tuple<bool, string>> WppSendText(WebView2 wv, string chatId, string text, string mailbox)
        {
            try
            {
                if (!await EnsureWpp(wv)) return Tuple.Create(false, "nowpp");
                string js = "window." + mailbox + "='PENDING';"
                    + "WPP.chat.sendTextMessage(" + JsonConvert.SerializeObject(chatId) + "," + JsonConvert.SerializeObject(text ?? "")
                    + ").then(function(r){window." + mailbox + "='OK:'+JSON.stringify(r).substring(0,200);})"
                    + ".catch(function(e){window." + mailbox + "='FAIL:'+((e&&e.message)||e);});'DISPATCHED';";
                await Eval(wv, js);
                string res = await Mailbox(wv, mailbox, 14);
                return Tuple.Create(res.StartsWith("OK:{"), res);
            }
            catch (Exception ex) { return Tuple.Create(false, "ex:" + ex.Message); }
        }

        private static async Task<Tuple<bool, string>> WppSendMedia(WebView2 wv, string chatId, string base64, string fileName, string caption, string mailbox)
        {
            try
            {
                if (!await EnsureWpp(wv)) return Tuple.Create(false, "nowpp");
                string js = "window." + mailbox + "='PENDING';"
                    + "WPP.chat.sendFileMessage(" + JsonConvert.SerializeObject(chatId) + "," + JsonConvert.SerializeObject(base64)
                    + ",{filename:" + JsonConvert.SerializeObject(fileName ?? "file")
                    + (string.IsNullOrEmpty(caption) ? "" : ",caption:" + JsonConvert.SerializeObject(caption))
                    + "}).then(function(r){window." + mailbox + "='OK:'+JSON.stringify(r).substring(0,200);})"
                    + ".catch(function(e){window." + mailbox + "='FAIL:'+((e&&e.message)||e);});'DISPATCHED';";
                await Eval(wv, js);
                string res = await Mailbox(wv, mailbox, 20);
                return Tuple.Create(res.StartsWith("OK:{"), res);
            }
            catch (Exception ex) { return Tuple.Create(false, "ex:" + ex.Message); }
        }

        private static string B64(string filePath, out string contentType)
        {
            contentType = "application/octet-stream";
            try
            {
                if (!System.IO.File.Exists(filePath)) return null;
                string ext = (System.IO.Path.GetExtension(filePath) ?? "").ToLower();
                try { contentType = System.Web.MimeMapping.GetMimeMapping(filePath); }
                catch (Exception) { }
                if (ext == ".mp4") contentType = "video/mp4";
                byte[] bytes = System.IO.File.ReadAllBytes(filePath);
                return "data:" + contentType + ";base64," + Convert.ToBase64String(bytes);
            }
            catch (Exception) { return null; }
        }

        // ===== teks =====
        public static void SendMessage(WebView2 wv, string to, string msg) { try { var _ = WppSendText(wv, ChatId(to), msg, "__sm" + Guid.NewGuid().ToString("N").Substring(0, 6)); } catch (Exception) { } }
        public static void SendMessage(WebView2 wv, string to, string msg, bool p1, bool tagAll) { try { var _ = WppSendText(wv, ChatId(to), msg, "__sm" + Guid.NewGuid().ToString("N").Substring(0, 6)); } catch (Exception) { } }

        public static async Task<bool> SendMessageSync(WebView2 wv, string to, string msg, bool p1, bool p2, WASender.Models.ButtonHolderModel holder)
        {
            try
            {
                var r = await WppSendText(wv, ChatId(to), msg, "__sms" + Guid.NewGuid().ToString("N").Substring(0, 6));
                return r.Item1;
            }
            catch (Exception) { return false; }
        }

        public static void sendTextMessage(WebView2 wv, string number, string msg)
        {
            try { var _ = WppSendText(wv, ChatId(number), msg, "__stm" + Guid.NewGuid().ToString("N").Substring(0, 6)); } catch (Exception) { }
        }

        public static void sendTextMessageWithPreview(WebView2 wv, string to, string msg)
        {
            try { var _ = WppSendWithPreview(wv, ChatId(to), msg); } catch (Exception) { }
        }

        public static void sendTextMessageWithPreview(WebView2 wv, string to, string msg, bool p, bool tagAll)
        {
            try { var _ = WppSendWithPreview(wv, ChatId(to), msg); } catch (Exception) { }
        }

        public static async Task<bool> sendTextMessageWithPreviewSync(WebView2 wv, string to, string msg)
        {
            try
            {
                var r = await WppSendWithPreview(wv, ChatId(to), msg);
                return r;
            }
            catch (Exception) { return false; }
        }

        private static async Task<bool> WppSendWithPreview(WebView2 wv, string chatId, string text)
        {
            try
            {
                if (!await EnsureWpp(wv)) return false;
                string js = "window.__pv='PENDING';"
                    + "(function(){try{"
                    + "var link=null;try{var m=(" + JsonConvert.SerializeObject(text ?? "") + ").match(/https?:\\/\\/[^\\s]+/);if(m)link=m[0];}catch(e){}"
                    + "var p={linkPreview:true};"
                    + "WPP.chat.sendTextMessage(" + JsonConvert.SerializeObject(chatId) + "," + JsonConvert.SerializeObject(text ?? "") + ",p)"
                    + ".then(function(r){window.__pv='OK:'+JSON.stringify(r).substring(0,200);}).catch(function(e){window.__pv='FAIL:'+e;});"
                    + "}catch(e){window.__pv='EX:'+e.message;}})();'DISPATCHED';";
                await Eval(wv, js);
                string res = await Mailbox(wv, "__pv", 14);
                return res.StartsWith("OK:{");
            }
            catch (Exception) { return false; }
        }

        public static async void SendDirectMessageAsync(WebView2 wv, string number, string msg, WASender.Models.ButtonHolderModel holder)
        {
            try { await WppSendText(wv, ChatId(number), msg, "__sdm" + Guid.NewGuid().ToString("N").Substring(0, 6)); } catch (Exception) { }
        }

        public static async Task SendMessageFull(WebView2 wv, string to, int rInt, string msg)
        {
            try
            {
                if (rInt > 0) await Task.Delay(rInt * 1000);
                await WppSendText(wv, ChatId(to), msg, "__smf" + Guid.NewGuid().ToString("N").Substring(0, 6));
            }
            catch (Exception) { }
        }

        public static async Task SendMessageFullWithReply(WebView2 wv, string to, int rInt, string msg)
        {
            await SendMessageFull(wv, to, rInt, msg);
        }

        public static async Task<bool> SendMessageFullGroupRT(WebView2 wv, string groupId, int rInt, string msg, string extra)
        {
            try
            {
                if (rInt > 0) await Task.Delay(rInt * 1000);
                var r = await WppSendText(wv, groupId, msg, "__smg" + Guid.NewGuid().ToString("N").Substring(0, 6));
                return r.Item1;
            }
            catch (Exception) { return false; }
        }

        // ===== media generik =====
        public static async void SendVideoAsync(WebView2 wv, string to, string base64, string captionOrMsg)
        { try { await WppSendMedia(wv, ChatId(to), base64, "video.mp4", captionOrMsg, "__sva4" + Guid.NewGuid().ToString("N").Substring(0, 6)); } catch (Exception) { } }
        public static async void SendVideoAsync(WebView2 wv, string to, string base64, string captionOrMsg, string fileName) { try { await WppSendMedia(wv, ChatId(to), base64, fileName, captionOrMsg, "__sva" + Guid.NewGuid().ToString("N").Substring(0, 6)); } catch (Exception) { } }
        public static async void SendVideoAsync(WebView2 wv, string to, string base64, string captionOrMsg, string fileName, WASender.Models.ButtonHolderModel holder) { try { await WppSendMedia(wv, ChatId(to), base64, fileName, captionOrMsg, "__svb" + Guid.NewGuid().ToString("N").Substring(0, 6)); } catch (Exception) { } }
        public static async Task<bool> SendVideosync(WebView2 wv, string to, string base64, string captionOrMsg) { try { var r = await WppSendMedia(wv, ChatId(to), base64, "f", captionOrMsg, "__svs" + Guid.NewGuid().ToString("N").Substring(0, 6)); return r.Item1; } catch (Exception) { return false; } }
        public static async Task<bool> SendVideosync(WebView2 wv, string to, string base64, string captionOrMsg, string fileName) { try { var r = await WppSendMedia(wv, ChatId(to), base64, fileName, captionOrMsg, "__sv2" + Guid.NewGuid().ToString("N").Substring(0, 6)); return r.Item1; } catch (Exception) { return false; } }
        public static async Task<bool> SendVideosync(WebView2 wv, string to, string base64, string captionOrMsg, string fileName, WASender.Models.ButtonHolderModel holder) { try { var r = await WppSendMedia(wv, ChatId(to), base64, fileName, captionOrMsg, "__sv3" + Guid.NewGuid().ToString("N").Substring(0, 6)); return r.Item1; } catch (Exception) { return false; } }

        public static async void SendAudioAsync(WebView2 wv, string to, string base64, string captionOrMsg) { try { await WppSendMedia(wv, ChatId(to), base64, "a", captionOrMsg, "__saa" + Guid.NewGuid().ToString("N").Substring(0, 6)); } catch (Exception) { } }
        public static async void SendAudioAsync(WebView2 wv, string to, string base64, string captionOrMsg, WASender.Models.ButtonHolderModel holder) { try { await WppSendMedia(wv, ChatId(to), base64, "a", captionOrMsg, "__sab" + Guid.NewGuid().ToString("N").Substring(0, 6)); } catch (Exception) { } }

        public static async Task<bool> sendAudioFile(WebView2 wv, string to, string base64)
        {
            try { var r = await WppSendMedia(wv, ChatId(to), base64, "a", "", "__saf" + Guid.NewGuid().ToString("N").Substring(0, 6)); return r.Item1; }
            catch (Exception) { return false; }
        }

        public static bool sendAudioFileSync(WebView2 wv, string groupId, string base64)
        {
            try
            {
                var t = WppSendMedia(wv, ChatId(groupId), base64, "a", "", "__safs" + Guid.NewGuid().ToString("N").Substring(0, 6));
                t.Wait(60000);
                return t.Result.Item1;
            }
            catch (Exception) { return false; }
        }

        public static async Task<bool> SendMessageGroupSync(WebView2 wv, string groupId, string msg, bool tagAll)
        {
            try
            {
                if (!await EnsureWpp(wv)) return false;
                string gid = (groupId ?? "").Contains("@") ? groupId : groupId + "@g.us";
                string mbox = "__smg" + Guid.NewGuid().ToString("N").Substring(0, 6);
                string js;
                if (tagAll)
                {
                    js = "window." + mbox + "='PENDING';(function(){try{WPP.group.getParticipants(" + JsonConvert.SerializeObject(gid) + ").then(function(ps){try{var arr=(ps&&ps.map)?ps:(ps&&ps.participants?ps.participants:[]);var ids=arr.map(function(p){try{var id=p.id||p;return (typeof id==='string')?id:((id&&(id._serialized||String(id)))||'');}catch(e){return '';}}).filter(function(x){return !!x;});WPP.chat.sendTextMessage(" + JsonConvert.SerializeObject(gid) + "," + JsonConvert.SerializeObject(msg ?? "") + ",{mentionedList:ids,detectMentioned:true,waitForAck:true}).then(function(r){window." + mbox + "='OK:'+JSON.stringify(r).substring(0,300);}).catch(function(e){window." + mbox + "='FAIL:'+(e&&e.message);});}catch(e){window." + mbox + "='FAIL';}}).catch(function(e){window." + mbox + "='FAIL:'+(e&&e.message);});}catch(e){window." + mbox + "='EX';}})();'DISPATCHED';";
                }
                else
                {
                    js = "window." + mbox + "='PENDING';(function(){try{WPP.chat.sendTextMessage(" + JsonConvert.SerializeObject(gid) + "," + JsonConvert.SerializeObject(msg ?? "") + ",{waitForAck:true}).then(function(r){window." + mbox + "='OK:'+JSON.stringify(r).substring(0,300);}).catch(function(e){window." + mbox + "='FAIL:'+(e&&e.message);});}catch(e){window." + mbox + "='EX';}})();'DISPATCHED';";
                }
                await Eval(wv, js);
                string res = await Mailbox(wv, mbox, 20);
                return res != null && res.StartsWith("OK:{");
            }
            catch (Exception) { return false; }
        }

        public static async Task<bool> SendMediaGroupSync(WebView2 wv, string groupId, string base64, string fileName, string caption)
        {
            try
            {
                if (!await EnsureWpp(wv)) return false;
                string gid = (groupId ?? "").Contains("@") ? groupId : groupId + "@g.us";
                string mbox = "__gmd" + Guid.NewGuid().ToString("N").Substring(0, 6);
                string js = "window." + mbox + "='PENDING';(function(){try{"
                    + "WPP.chat.sendFileMessage(" + JsonConvert.SerializeObject(gid) + "," + JsonConvert.SerializeObject(base64 ?? "")
                    + ",{filename:" + JsonConvert.SerializeObject(fileName ?? "file") + ",caption:" + JsonConvert.SerializeObject(caption ?? "") + "})"
                    + ".then(function(r){var mid='';try{mid=(r&&r.id&&(r.id._serialized||r.id.id||String(r.id)))||'';}catch(e){}"
                    + "WPP.chat.getMessages(" + JsonConvert.SerializeObject(gid) + ",{count:10}).then(function(ms){try{var arr=(ms&&ms.map)?ms:((ms&&ms.models)?ms.models:[]);var ok=false;"
                    + "for(var i=arr.length-1;i>=0;i--){var m=arr[i]||{};var id='';try{id=(m.id&&(m.id._serialized||m.id.id||String(m.id)))||'';}catch(e){}var fm=false;try{fm=!!(m.fromMe||(m.id&&m.id.fromMe));}catch(e){}"
                    + "if(fm&&(id===mid||mid=='')){ok=true;break;}}window." + mbox + "=ok?'OK':'NOTFOUND';}catch(e){window." + mbox + "='ERR';}}).catch(function(e){window." + mbox + "='FAIL';});"
                    + "}).catch(function(e){window." + mbox + "='FAIL:'+(e&&e.message);});}catch(e){window." + mbox + "='EX';}})();'DISPATCHED';";
                await Eval(wv, js);
                string res = await Mailbox(wv, mbox, 25);
                return res == "OK";
            }
            catch (Exception) { return false; }
        }

        public static async void SendAttachmentAsync(WebView2 wv, string to, string base64, string fileName, string caption) { try { await WppSendMedia(wv, ChatId(to), base64, fileName, caption, "__saa2" + Guid.NewGuid().ToString("N").Substring(0, 6)); } catch (Exception) { } }

        public static async Task<bool> SendAttachmentsync(WebView2 wv, string to, string base64, string fileName, string caption)
        {
            try { var r = await WppSendMedia(wv, ChatId(to), base64, fileName, caption, "__sas" + Guid.NewGuid().ToString("N").Substring(0, 6)); return r.Item1; }
            catch (Exception) { return false; }
        }

        public static void SendVideoToGroup2(WebView2 wv, string groupId, string base64, string msgOrCaption, string fileName, bool tagAll)
        {
            try { var _ = WppSendMedia(wv, ChatId(groupId), base64, fileName, msgOrCaption, "__svg" + Guid.NewGuid().ToString("N").Substring(0, 6)); } catch (Exception) { }
        }

        public static void SendAttachmentToGroup(WebView2 wv, string groupId, string base64, string fileName, string caption)
        {
            try { var _ = WppSendMedia(wv, ChatId(groupId), base64, fileName, caption, "__sag" + Guid.NewGuid().ToString("N").Substring(0, 6)); } catch (Exception) { }
        }

        public static async Task<bool> sendVideoNote(WebView2 wv, string to, string base64)
        {
            try { var r = await WppSendMedia(wv, ChatId(to), base64, "v", "", "__svn" + Guid.NewGuid().ToString("N").Substring(0, 6)); return r.Item1; }
            catch (Exception) { return false; }
        }

        public static bool sendVideoNoteSync(WebView2 wv, string groupId, string base64)
        {
            try
            {
                var t = WppSendMedia(wv, ChatId(groupId), base64, "v", "", "__svns" + Guid.NewGuid().ToString("N").Substring(0, 6));
                t.Wait(60000);
                return t.Result.Item1;
            }
            catch (Exception) { return false; }
        }

        public static async Task<bool> sendSticker(WebView2 wv, string to, string base64)
        {
            try { var r = await WppSendMedia(wv, ChatId(to), base64, "s", "", "__sst" + Guid.NewGuid().ToString("N").Substring(0, 6)); return r.Item1; }
            catch (Exception) { return false; }
        }

        public static bool sendStickerSync(WebView2 wv, string groupId, string base64)
        {
            try
            {
                var t = WppSendMedia(wv, ChatId(groupId), base64, "s", "", "__ssts" + Guid.NewGuid().ToString("N").Substring(0, 6));
                t.Wait(60000);
                return t.Result.Item1;
            }
            catch (Exception) { return false; }
        }

        public static async Task<bool> SendLocationMessage(WebView2 wv, string to, string lat, string lng, string name, string addr, string url)
        {
            try
            {
                if (!await EnsureWpp(wv)) return false;
                string js = "window.__slm='PENDING';"
                    + "(function(){try{"
                    + "WPP.chat.sendLocationMessage(" + JsonConvert.SerializeObject(ChatId(to)) + ",{lat:" + JsonConvert.SerializeObject(lat ?? "") + ",lng:" + JsonConvert.SerializeObject(lng ?? "") + ",name:" + JsonConvert.SerializeObject(name ?? "") + ",address:" + JsonConvert.SerializeObject(addr ?? "") + ",url:" + JsonConvert.SerializeObject(url ?? "") + "})"
                    + ".then(function(r){window.__slm='OK:'+JSON.stringify(r).substring(0,200);}).catch(function(e){window.__slm='FAIL:'+(e&&e.message);});"
                    + "}catch(e){window.__slm='EX:'+e.message;}})();'DISPATCHED';";
                await Eval(wv, js);
                string res = await Mailbox(wv, "__slm", 12);
                return res.StartsWith("OK:{");
            }
            catch (Exception) { return false; }
        }

        public static bool SendLocationMessageSync(WebView2 wv, string groupId, string lat, string lng, string name, string addr, string url)
        {
            try
            {
                var t = SendLocationMessage(wv, groupId, lat, lng, name, addr, url);
                t.Wait(60000);
                return t.Result;
            }
            catch (Exception) { return false; }
        }

        public static async Task<bool> SendsendVCardContactMessage(WebView2 wv, string to, string number, string name)
        {
            try
            {
                if (!await EnsureWpp(wv)) return false;
                string num = new string((number ?? "").Where(char.IsDigit).ToArray());
                string vcard = "BEGIN:VCARD\nVERSION:3.0\nN:" + (name ?? num) + "\nTEL;type=CELL;type=VOICE;waid=" + num + ":+" + num + "\nEND:VCARD";
                string js = "window.__svc='PENDING';"
                    + "WPP.chat.sendVCardContactMessage(" + JsonConvert.SerializeObject(ChatId(to)) + "," + JsonConvert.SerializeObject(vcard) + ")"
                    + ".then(function(r){window.__svc='OK:'+JSON.stringify(r).substring(0,200);}).catch(function(e){window.__svc='FAIL:'+(e&&e.message);});'DISPATCHED';";
                await Eval(wv, js);
                string res = await Mailbox(wv, "__svc", 12);
                return res.StartsWith("OK:{");
            }
            catch (Exception) { return false; }
        }

        public static bool SendsendVCardContactMessageSync(WebView2 wv, string groupId, string number, string name)
        {
            try
            {
                var t = SendsendVCardContactMessage(wv, groupId, number, name);
                t.Wait(60000);
                return t.Result;
            }
            catch (Exception) { return false; }
        }

        // ===== polling =====
        private static string PollName(object poll)
        {
            try
            {
                if (poll == null) return "";
                var t = poll.GetType();
                foreach (string n in new string[] { "PollName", "pollName", "Name", "name", "Title", "title" })
                {
                    var p = t.GetProperty(n);
                    if (p != null) { object v = p.GetValue(poll, null); if (v != null) return v.ToString(); }
                }
            }
            catch (Exception) { }
            return "Poll";
        }

        private static List<string> PollChoices(object poll)
        {
            var outList = new List<string>();
            try
            {
                if (poll == null) return outList;
                var t = poll.GetType();
                foreach (string n in new string[] { "PollOptions", "pollOptions", "Options", "options", "Choices", "choices", "List" })
                {
                    var p = t.GetProperty(n);
                    if (p == null) continue;
                    var v = p.GetValue(poll, null) as System.Collections.IEnumerable;
                    if (v == null) continue;
                    foreach (var o in v)
                    {
                        try
                        {
                            if (o == null) continue;
                            if (o is string) { outList.Add((string)o); continue; }
                            var ot = o.GetType();
                            foreach (string nn in new string[] { "OptionText", "Option", "Text", "text", "Name", "name", "Title", "title" })
                            {
                                var pp = ot.GetProperty(nn);
                                if (pp != null) { object vv = pp.GetValue(o, null); if (vv != null) { outList.Add(vv.ToString()); break; } }
                            }
                        }
                        catch (Exception) { }
                    }
                    if (outList.Count > 0) break;
                }
            }
            catch (Exception) { }
            return outList;
        }

        public static void sendCreatePollMessageToNumber(WebView2 wv, string to, object poll)
        {
            try { var _ = sendCreatePollMessageToNumberSync(wv, to, poll); } catch (Exception) { }
        }

        public static async Task<bool> sendCreatePollMessageToNumberSync(WebView2 wv, string to, object poll)
        {
            try
            {
                if (!await EnsureWpp(wv)) return false;
                string name = PollName(poll);
                var choices = PollChoices(poll);
                if (choices.Count == 0) return false;
                string js = "window.__spoll='PENDING';"
                    + "WPP.chat.sendCreatePollMessage(" + JsonConvert.SerializeObject(ChatId(to)) + "," + JsonConvert.SerializeObject(name) + "," + JsonConvert.SerializeObject(choices) + ",{selectableCount:0})"
                    + ".then(function(r){window.__spoll='OK:'+JSON.stringify(r).substring(0,200);}).catch(function(e){window.__spoll='FAIL:'+(e&&e.message);});'DISPATCHED';";
                await Eval(wv, js);
                string res = await Mailbox(wv, "__spoll", 12);
                return res.StartsWith("OK:{");
            }
            catch (Exception) { return false; }
        }

        public static void sendCreatePollMessageToGroup(WebView2 wv, string groupId, object poll)
        {
            try { var _ = sendCreatePollMessageToNumberSync(wv, groupId, poll); } catch (Exception) { }
        }
    }
}

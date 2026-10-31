/* WAPI v4 — DOM-based send for reliability.
 * Instead of hooking into WhatsApp's internal Store/module API (which changes constantly),
 * this uses real DOM interaction: find input → type message → click send button.
 * Falls back to Store API if available.
 */
var WAPI = window.WAPI || {};
WAPI.version = "4.0.0";
WAPI.__ready = true;

WAPI.isReady = function () { return true; };

WAPI.getMyJid = function () {
  try {
    var store = window.Store || {};
    if (store.Conn && store.Conn.wid) return store.Conn.wid._serialized || String(store.Conn.wid);
    if (store.Conn && store.Conn.me) return store.Conn.me._serialized || String(store.Conn.me);
    return null;
  } catch (e) { return null; }
};

WAPI.normalizePhone = function (raw) {
  return String(raw).replace(/[^0-9]/g, "");
};

WAPI.phoneToJid = function (phone) {
  return WAPI.normalizePhone(phone) + "@c.us";
};

// ============================================================
// DOM-based send — reliable, mimics real user
// ============================================================

function sleep(ms) {
  return new Promise(function (resolve) { setTimeout(resolve, ms); });
}

function findElement(selectors, timeout) {
  timeout = timeout || 10000;
  var start = Date.now();
  return new Promise(function (resolve) {
    function tryFind() {
      for (var i = 0; i < selectors.length; i++) {
        try {
          var el = document.querySelector(selectors[i]);
          if (el) return resolve(el);
        } catch (e) {}
      }
      if (Date.now() - start > timeout) return resolve(null);
      setTimeout(tryFind, 300);
    }
    tryFind();
  });
}

function findMessageInput() {
  return findElement([
    'div[contenteditable="true"][role="textbox"][aria-label="Type a message"]',
    'div[contenteditable="true"][data-tab="10"]',
    'div[contenteditable="true"][data-tab="1"]',
    'footer div[contenteditable="true"]',
    '#main footer div[contenteditable="true"]',
    '[data-lexical-editor] p',
    '[contenteditable="true"][spellcheck]',
  ]);
}

function findSendButton() {
  return findElement([
    'button[aria-label="Send"]',
    'span[data-icon="send"]',
    'button[data-tab="11"]',
    'button span[data-icon="send"]',
    '#main footer button[aria-label="Send"]',
    'footer button[aria-label="Send"]',
  ]);
}

function pasteText(el, text) {
  el.focus();
  if (typeof document.execCommand === "function") {
    el.innerText = "";
    document.execCommand("selectAll", false, null);
    document.execCommand("insertText", false, text);
  } else {
    el.textContent = text;
  }
  el.dispatchEvent(new InputEvent("input", { bubbles: true, inputType: "insertText", data: text }));
  el.dispatchEvent(new Event("change", { bubbles: true }));
}

async function sendViaDOM(text) {
  var input = await findMessageInput();
  if (!input) throw new Error("input_not_found: cannot find message input box");

  pasteText(input, text);
  await sleep(500);

  var btn = await findSendButton();
  if (btn) {
    btn.click();
    await sleep(300);
    return { ok: true, method: "dom" };
  }

  // Fallback: press Enter
  input.dispatchEvent(new KeyboardEvent("keydown", { key: "Enter", keyCode: 13, which: 13, bubbles: true }));
  await sleep(300);
  return { ok: true, method: "dom_enter" };
}

// ============================================================
// Store-based send — faster, used as primary when available
// ============================================================

async function sendViaStore(phone, text) {
  var Store = window.Store;
  if (!Store || !Store.Chat) throw new Error("store_unavailable");

  var jid = WAPI.phoneToJid(phone);
  var wid;
  try {
    if (Store.WidFactory && Store.WidFactory.createUserWid) {
      wid = Store.WidFactory.createUserWid(WAPI.normalizePhone(phone));
    } else if (Store.WidFactory) {
      wid = Store.WidFactory.createWid(jid);
    }
  } catch (e) {}
  if (!wid) throw new Error("wid_create_failed");

  var chat = null;
  try {
    if (Store.FindChat && Store.FindChat.findOrCreateLatestChat) {
      var result = await Store.FindChat.findOrCreateLatestChat(wid, "username_contactless_search", { forceUsync: true });
      chat = result.chat;
    }
  } catch (e) {}
  if (!chat && Store.Chat && typeof Store.Chat.get === "function") {
    chat = Store.Chat.get(wid);
  }
  if (!chat) throw new Error("chat_not_found");

  var fromWid;
  try {
    fromWid = await Store.UserPrefs.getMaybeMeUser();
  } catch (e) {
    fromWid = Store.Conn && Store.Conn.wid;
  }
  if (!fromWid) fromWid = Store.Conn && Store.Conn.me;

  if (Store.addAndSendMsgToChat) {
    var msgId;
    if (Store.MsgKey) {
      msgId = Store.MsgKey.fromString(chat.id._serialized + "_" + Date.now() + "_" + Math.random().toString(36).substr(2, 9));
    }
    var now = Math.floor(Date.now() / 1000);
    var rawMessage = {
      id: msgId,
      ack: 1,
      from: fromWid,
      to: chat.id,
      local: true,
      self: "out",
      t: now,
      isNewMsg: true,
      type: "chat",
      body: String(text),
      subtype: null,
    };
    try {
      if (Store.EphemeralFields && Store.EphemeralFields.getEphemeralFields) {
        var eph = Store.EphemeralFields.getEphemeralFields(chat);
        Object.assign(rawMessage, eph);
      }
    } catch (e) {}
    await Store.addAndSendMsgToChat(chat, rawMessage);
    return { ok: true, method: "store" };
  }

  if (Store.SendTextMsgToChat) {
    var fn = Store.SendTextMsgToChat.sendTextMsgToChat || Store.SendTextMsgToChat;
    if (typeof fn === "function") {
      await fn(chat, text);
      return { ok: true, method: "store_legacy" };
    }
  }

  if (typeof chat.sendMessage === "function") {
    await chat.sendMessage(text);
    return { ok: true, method: "chat_sendMessage" };
  }

  throw new Error("store_no_send_path");
}

// ============================================================
// Public send API — tries Store first, falls back to DOM
// ============================================================

WAPI.sendMessageByPhone = async function (phone, text) {
  if (!phone || !text) return { ok: false, error: "phone and text required" };
  if (typeof text !== "string") text = String(text);

  // Open chat via deep link if needed
  var jid = WAPI.phoneToJid(phone);
  var currentUrl = window.location.href;
  if (!currentUrl.includes("web.whatsapp.com")) {
    return { ok: false, error: "not_on_whatsapp" };
  }

  // Check if chat exists for this phone
  var rawPhone = WAPI.normalizePhone(phone);

  // Try Store API first (fast, reliable if modules available)
  if (window.Store && window.Store.Chat && window.Store.Msg) {
    try {
      return await sendViaStore(phone, text);
    } catch (storeErr) {
      console.log("[WAPI] Store send failed: " + (storeErr.message || storeErr) + ", falling back to DOM");
    }
  }

  // Navigate to chat
  try {
    window.location.href = "https://web.whatsapp.com/send?phone=" + rawPhone;
    await sleep(3000);
  } catch (e) {}

  // DOM send
  try {
    return await sendViaDOM(text);
  } catch (domErr) {
    return { ok: false, error: "dom_send_failed: " + (domErr.message || domErr) };
  }
};

WAPI.sendMessage = async function (jid, text) {
  var phone = (jid || "").replace("@c.us", "").replace("@s.whatsapp.net", "");
  return WAPI.sendMessageByPhone(phone, text);
};

// ============================================================
// Other API (read-only, Store-based with DOM fallback)
// ============================================================

WAPI.getAllChats = function () {
  try {
    if (window.Store && window.Store.Chat) {
      var arr = typeof window.Store.Chat.getModelsArray === "function"
        ? window.Store.Chat.getModelsArray()
        : (window.Store.Chat._models || []);
      return arr.map(function (c) { return {
        id: (c.id && c.id._serialized) || String(c.id || ""),
        name: c.name || c.formattedTitle || "",
        isGroup: !!(c.isGroup || c.groupMetadata),
        unread: c.unreadCount || 0,
        timestamp: c.t || 0,
      }; });
    }
  } catch (e) {}
  return [];
};

WAPI.getAllContacts = function () {
  try {
    if (window.Store && window.Store.Contact) {
      var arr = typeof window.Store.Contact.getModelsArray === "function"
        ? window.Store.Contact.getModelsArray()
        : (window.Store.Contact._models || []);
      return arr.map(function (c) { return {
        id: (c.id && c.id._serialized) || String(c.id || ""),
        name: c.name || c.pushname || "",
        phone: (c.id && c.id.user) || "",
      }; });
    }
  } catch (e) {}
  return [];
};

WAPI.checkNumberStatus = function (phone) {
  return Promise.resolve({ phone: phone, exists: null });
};

WAPI.getGroupParticipants = function (groupId) {
  return Promise.resolve([]);
};

WAPI.getMessages = function (chatId, count) {
  return [];
};

WAPI.getPollResults = function (chatId, msgId) {
  return null;
};

WAPI.onIncomingMessage = function (callback) {
  try {
    var MsgStore = (window.Store && window.Store.Msg);
    if (MsgStore && typeof MsgStore.on === "function") {
      MsgStore.on("add", function (msg) {
        try {
          if (msg && !msg.id.fromMe && msg.isNewMsg) {
            callback({
              id: msg.id._serialized,
              from: msg.from && msg.from._serialized,
              body: msg.body || "",
              type: msg.type,
              timestamp: msg.t,
              isGroup: msg.from && msg.from.endsWith("@g.us"),
            });
          }
        } catch (e) {}
      });
      return true;
    }
  } catch (e) {}
  return false;
};

WAPI._sendViaDOM = sendViaDOM;
WAPI.bridgeReady = function () {
  return !!(window.pywebview && window.pywebview.api);
};

console.log("[WAPI] v4.0.0 ready — DOM-based send with Store fallback");

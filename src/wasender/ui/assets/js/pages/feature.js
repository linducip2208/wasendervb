/* Generic feature runner — renders form per feature key, calls api.feature_run, shows result. */

router.register('feature', async (root, params = {}) => {
  const key = params.key;
  if (!key) { root.innerHTML = '<div class="msg error">No feature key</div>'; return; }

  const features = await api.feature_list();
  const meta = features.find(f => f.key === key);
  if (!meta) { root.innerHTML = `<div class="msg error">Unknown feature: ${escapeHtml(key)}</div>`; return; }

  const accounts = await api.list_accounts();

  // form templates per feature key
  const formHtml = renderFeatureForm(key, accounts);

  root.innerHTML = `
    <div class="breadcrumb">Tools &nbsp;/&nbsp; <strong>${escapeHtml(meta.label)}</strong></div>
    <div class="page-header">
      <div>
        <div class="page-title">${escapeHtml(meta.label)}</div>
        <div class="page-subtitle">${meta.description ? escapeHtml(meta.description) : escapeHtml(meta.category)} ${meta.requires_account ? '· <span class="badge badge-warn">requires account</span>' : '· <span class="badge badge-muted">no account needed</span>'}</div>
      </div>
      <div><button class="btn btn-secondary btn-sm" id="back-btn">← Back</button></div>
    </div>
    <div class="ws-grid">
      <div class="ws-stack">
        <div class="card">
          <div class="card-title-row"><span class="step-num">1</span><span class="card-title" style="margin:0">Parameter</span></div>
          <form id="feat-form" onsubmit="return false">
            ${formHtml}
          </form>
        </div>
        <div class="card">
          <div class="card-title-row"><span class="step-num">2</span><span class="card-title" style="margin:0">Jalankan</span></div>
          <button class="btn btn-primary btn-block" id="feat-run">▶ Run</button>
          <div class="field-hint small muted" style="margin-top:8px">Proses berjalan di backend; jangan tutup halaman saat running.</div>
        </div>
      </div>
      <div class="ws-stack">
        <div class="card">
          <div class="card-title-row"><span class="step-num">3</span><span class="card-title" style="margin:0">Hasil</span></div>
          <div id="feat-result"><div class="empty-state"><div class="big">📋</div><div>Belum ada hasil. Isi parameter lalu klik Run.</div></div></div>
        </div>
      </div>
    </div>
  `;

  document.getElementById('back-btn').onclick = () => router.go('dashboard');
  document.getElementById('feat-run').onclick = async () => {
    const params = collectFeatureParams(key);
    if (params === null) return; // validation failed already toasted

    if (meta.requires_account && !params.account_id) {
      toast('Pilih account dulu', 'error'); return;
    }

    const resultEl = document.getElementById('feat-result');
    resultEl.innerHTML = '<div class="skeleton" style="height:18px"></div><div class="skeleton"></div><div class="skeleton" style="width:60%"></div><div class="muted small" style="margin-top:8px"><span class="spinner"></span> Running... please wait</div>';
    document.getElementById('feat-run').disabled = true;

    try {
      const r = await api.feature_run(key, params);
      renderResult(resultEl, r);
    } catch (e) {
      resultEl.innerHTML = `<div class="card"><div class="msg error">Error: ${escapeHtml(e.message || e)}</div></div>`;
    } finally {
      document.getElementById('feat-run').disabled = false;
    }

    // For auto_responder list mode — refresh rule list after add
    if (key === 'auto_responder') {
      await refreshResponderRules();
    }
  };

  // Special init: auto_responder shows list of existing rules
  if (key === 'auto_responder') {
    await refreshResponderRules();
  }
});

// ---------- Form templates per feature ----------

function accountSelect(accounts, id = 'f-account') {
  if (!accounts.length) {
    return `<div class="msg warning">Belum ada account. Add dulu di tab Accounts.</div>
            <input id="${id}" type="hidden" value="" />`;
  }
  return `
    <div class="form-group">
      <label>Account</label>
      <select id="${id}">
        <option value="">— select —</option>
        ${accounts.map(a => `<option value="${a.id}">${escapeHtml(a.label)}${a.phone ? ' — ' + escapeHtml(a.phone) : ''}</option>`).join('')}
      </select>
    </div>
  `;
}

function renderFeatureForm(key, accounts) {
  switch (key) {
    case 'bulk_send':
      return `
        ${accountSelect(accounts)}
        <div class="form-group">
          <label>Contacts CSV (header: phone,name,...)</label>
          <textarea id="f-contacts" rows="6" placeholder="phone,name\n628123456789,John\n628987654321,Jane"></textarea>
        </div>
        <div class="form-group">
          <label>Message template (atau caption kalau pakai media)</label>
          <textarea id="f-message" rows="4" placeholder="Halo {name}, ada promo spesial buat kamu hari ini!"></textarea>
          <div class="field-hint small muted">Gunakan <code>{name}</code>, <code>{phone}</code>, atau kolom CSV lainnya untuk personalisasi.</div>
        </div>
        <div class="form-group">
          <label>Media file path <span class="muted small">(optional — image, PDF, video)</span></label>
          <input id="f-media-path" placeholder="C:\\Users\\you\\Pictures\\promo.jpg" />
          <div class="field-hint small muted">Absolute path. Kosongkan untuk text-only.</div>
        </div>
        <div class="form-group">
          <label>Nama campaign <span class="muted small">(optional — untuk reply-rate di dashboard)</span></label>
          <input id="f-campaign" placeholder="Promo-November" />
        </div>
      `;

    case 'get_group_member':
      return `
        ${accountSelect(accounts)}
        <div class="form-group">
          <label>Group ID / Invite link</label>
          <input id="f-group" placeholder="120363012345678901@g.us atau https://chat.whatsapp.com/..." />
        </div>
      `;

    case 'contact_list':
      return `
        ${accountSelect(accounts)}
        <div class="form-group">
          <label style="display:flex;align-items:center;gap:8px">
            <input id="f-only-my" type="checkbox" style="width:auto" checked />
            Only my contacts (filter saved contacts)
          </label>
        </div>
        <div class="form-group">
          <label style="display:flex;align-items:center;gap:8px">
            <input id="f-save-db" type="checkbox" style="width:auto" checked />
            Save to database
          </label>
        </div>
      `;

    case 'chat_list':
      return `
        ${accountSelect(accounts)}
        <div class="form-group">
          <label style="display:flex;align-items:center;gap:8px">
            <input id="f-only-groups" type="checkbox" style="width:auto" />
            Only groups
          </label>
        </div>
        <div class="form-group">
          <label style="display:flex;align-items:center;gap:8px">
            <input id="f-only-unread" type="checkbox" style="width:auto" />
            Only unread
          </label>
        </div>
      `;

    case 'active_members':
      return `
        ${accountSelect(accounts)}
        <div class="form-group">
          <label>Group ID</label>
          <input id="f-group" placeholder="120363012345678901@g.us" />
        </div>
        <div class="form-group">
          <label>Days lookback</label>
          <input id="f-days" type="number" min="1" max="90" value="7" />
        </div>
      `;

    case 'poll_results':
      return `
        ${accountSelect(accounts)}
        <div class="form-group">
          <label>Chat ID</label>
          <input id="f-chat" placeholder="628xxx@c.us atau group@g.us" />
        </div>
        <div class="form-group">
          <label>Message ID (poll)</label>
          <input id="f-msg" placeholder="3EB0..." />
        </div>
      `;

    case 'grab_group_links':
      return `
        <div class="form-group">
          <label>URLs (1 per line)</label>
          <textarea id="f-urls" rows="8" placeholder="https://example.com/page1\nhttps://example.com/page2"></textarea>
          <div class="field-hint small muted">Tool akan crawl & ekstrak semua link <code>chat.whatsapp.com/...</code>.</div>
        </div>
      `;

    case 'auto_joiner':
      return `
        ${accountSelect(accounts)}
        <div class="form-group">
          <label>Invite links (1 per line)</label>
          <textarea id="f-links" rows="8" placeholder="https://chat.whatsapp.com/AbCdEf...\nhttps://chat.whatsapp.com/XyZ..."></textarea>
        </div>
      `;

    case 'bulk_add_members':
      return `
        ${accountSelect(accounts)}
        <div class="form-group">
          <label>Group ID</label>
          <input id="f-group" placeholder="120363012345678901@g.us" />
        </div>
        <div class="form-group">
          <label>Phone numbers (1 per line)</label>
          <textarea id="f-phones" rows="8" placeholder="628123456789\n628987654321"></textarea>
        </div>
      `;

    case 'group_finder':
      return `
        <div class="form-group">
          <label>Keyword</label>
          <input id="f-keyword" placeholder="komunitas marketing indonesia" />
        </div>
        <div class="form-group">
          <label>Limit results</label>
          <input id="f-limit" type="number" min="1" max="500" value="50" />
        </div>
      `;

    case 'bulk_group_gen':
      return `
        ${accountSelect(accounts)}
        <div class="form-group">
          <label>Group name template</label>
          <input id="f-name" placeholder="Reseller {{n}}" />
          <div class="field-hint small muted"><code>{{n}}</code> akan diganti angka 1..count</div>
        </div>
        <div class="form-group">
          <label>Count</label>
          <input id="f-count" type="number" min="1" max="100" value="5" />
        </div>
        <div class="form-group">
          <label>Seed phones (1 per line — minimal 1 member untuk bikin group)</label>
          <textarea id="f-seeds" rows="4" placeholder="628123456789"></textarea>
        </div>
      `;

    case 'auto_responder':
      return `
        ${accountSelect(accounts)}
        <div class="card" style="margin-top:8px;background:#f9fafb">
          <div class="card-title small">Add new rule</div>
          <div class="form-group">
            <label>Pattern (text or regex)</label>
            <input id="f-pattern" placeholder="halo|hi|hello" />
          </div>
          <div class="form-group">
            <label style="display:flex;align-items:center;gap:8px">
              <input id="f-regex" type="checkbox" style="width:auto" />
              Pattern is regex
            </label>
          </div>
          <div class="form-group">
            <label>Auto-response</label>
            <textarea id="f-response" rows="3" placeholder="Halo! Pesan kamu kami terima, admin akan balas segera."></textarea>
          </div>
        </div>
        <div id="responder-rules" style="margin-top:12px"></div>
      `;

    case 'number_filter':
      return `
        ${accountSelect(accounts)}
        <div class="form-group">
          <label>Phone numbers (1 per line)</label>
          <textarea id="f-phones" rows="10" placeholder="628123456789\n628987654321"></textarea>
          <div class="field-hint small muted">Cek mana nomor yang aktif punya WhatsApp.</div>
        </div>
      `;

    case 'wa_warmer':
      return `
        ${accountSelect(accounts)}
        <div class="form-group">
          <label>Whitelist phones (1 per line — pesan dikirim ke nomor ini sebagai warmup)</label>
          <textarea id="f-whitelist" rows="6" placeholder="628111111111\n628222222222"></textarea>
        </div>
        <div class="form-group">
          <label>Messages count per round</label>
          <input id="f-count" type="number" min="1" max="100" value="10" />
        </div>
      `;

    case 'group_warmer':
      return `
        ${accountSelect(accounts)}
        <div class="form-group">
          <label>Group IDs (1 per line)</label>
          <textarea id="f-groups" rows="5" placeholder="120363012345678901@g.us"></textarea>
        </div>
        <div class="form-group">
          <label>Message templates (1 per line — random pick)</label>
          <textarea id="f-templates" rows="5" placeholder="Pagi semua\nApa kabar gengs\nGimana hari ini"></textarea>
        </div>
        <div class="form-group">
          <label>Rounds</label>
          <input id="f-rounds" type="number" min="1" max="50" value="3" />
        </div>
      `;

    case 'gmaps_extractor':
      return `
        <div class="form-group">
          <label>Search query</label>
          <input id="f-query" placeholder="restoran" />
        </div>
        <div class="form-group">
          <label>Location</label>
          <input id="f-location" placeholder="Jakarta Selatan" />
        </div>
        <div class="form-group">
          <label>Limit</label>
          <input id="f-limit" type="number" min="1" max="500" value="50" />
        </div>
      `;

    case 'website_extractor':
      return `
        <div class="form-group">
          <label>URLs (1 per line)</label>
          <textarea id="f-urls" rows="8" placeholder="https://example.com\nhttps://other.com"></textarea>
        </div>
        <div class="form-group">
          <label>Max pages per site</label>
          <input id="f-maxpages" type="number" min="1" max="200" value="10" />
        </div>
      `;

    case 'social_extractor':
      return `
        <div class="form-group">
          <label>Keyword</label>
          <input id="f-keyword" placeholder="real estate jakarta" />
        </div>
        <div class="form-group">
          <label>Platform</label>
          <select id="f-platform">
            <option value="linkedin">LinkedIn</option>
            <option value="twitter">Twitter / X</option>
            <option value="instagram">Instagram</option>
            <option value="facebook">Facebook</option>
            <option value="tiktok">TikTok</option>
          </select>
        </div>
        <div class="form-group">
          <label>Limit</label>
          <input id="f-limit" type="number" min="1" max="1000" value="100" />
        </div>
      `;

    case 'gcontacts_csv':
      return `
        <div class="form-group">
          <label>CSV input (Google Contacts export)</label>
          <textarea id="f-csv" rows="10" placeholder="Name,Phone 1 - Value\nJohn Doe,628123456789"></textarea>
        </div>
        <div class="form-group">
          <label>Label / tag</label>
          <input id="f-label" placeholder="leads-mei-2026" />
        </div>
      `;

    default:
      // generic JSON fallback
      return `
        <div class="form-group">
          <label>Parameters (JSON)</label>
          <textarea id="f-json" rows="8" placeholder='{}'>{}</textarea>
          <div class="field-hint small muted">No specific form for this feature — pass raw JSON params.</div>
        </div>
      `;
  }
}

// ---------- Param collectors ----------

function val(id) {
  const el = document.getElementById(id);
  if (!el) return '';
  if (el.type === 'checkbox') return el.checked;
  return el.value;
}
function intVal(id, def = 0) { const v = parseInt(val(id)); return isNaN(v) ? def : v; }
function lines(id) { return String(val(id) || '').split(/\r?\n/).map(s => s.trim()).filter(Boolean); }

function parseCsv(text) {
  const rows = String(text || '').split(/\r?\n/).map(l => l.trim()).filter(Boolean);
  if (!rows.length) return [];
  const header = rows.shift().split(',').map(h => h.trim().toLowerCase());
  return rows.map(line => {
    const cols = line.split(',');
    const r = {};
    header.forEach((h, i) => r[h] = (cols[i] || '').trim());
    return r;
  });
}

function collectFeatureParams(key) {
  const accId = (() => { const v = val('f-account'); return v ? parseInt(v) : null; })();

  switch (key) {
    case 'bulk_send': {
      const contacts = parseCsv(val('f-contacts'));
      const message = val('f-message');
      const mediaPath = val('f-media-path');
      if (!contacts.length) { toast('Contacts CSV kosong', 'error'); return null; }
      if (!message && !mediaPath) { toast('Message template atau media wajib diisi', 'error'); return null; }
      return { account_id: accId, contacts, message, media_path: mediaPath || null, campaign: val('f-campaign') || null };
    }
    case 'get_group_member':
      return { account_id: accId, group_id: val('f-group') };
    case 'contact_list':
      return { account_id: accId, only_my_contacts: val('f-only-my'), save_to_db: val('f-save-db') };
    case 'chat_list':
      return { account_id: accId, only_groups: val('f-only-groups'), only_unread: val('f-only-unread') };
    case 'active_members':
      return { account_id: accId, group_id: val('f-group'), days: intVal('f-days', 7) };
    case 'poll_results':
      return { account_id: accId, chat_id: val('f-chat'), msg_id: val('f-msg') };
    case 'grab_group_links':
      return { urls: lines('f-urls') };
    case 'auto_joiner':
      return { account_id: accId, links: lines('f-links') };
    case 'bulk_add_members':
      return { account_id: accId, group_id: val('f-group'), phones: lines('f-phones') };
    case 'group_finder':
      return { keyword: val('f-keyword'), limit: intVal('f-limit', 50) };
    case 'bulk_group_gen':
      return { account_id: accId, name_template: val('f-name'), count: intVal('f-count', 5), seed_phones: lines('f-seeds') };
    case 'auto_responder': {
      const pattern = val('f-pattern');
      const response = val('f-response');
      if (!pattern || !response) { toast('Pattern & response wajib diisi', 'error'); return null; }
      return {
        action: 'add',
        account_id: accId,
        pattern,
        is_regex: val('f-regex'),
        response,
      };
    }
    case 'number_filter':
      return { account_id: accId, phones: lines('f-phones') };
    case 'wa_warmer':
      return { account_id: accId, whitelist: lines('f-whitelist'), messages_count: intVal('f-count', 10) };
    case 'group_warmer':
      return { account_id: accId, group_ids: lines('f-groups'), templates: lines('f-templates'), rounds: intVal('f-rounds', 3) };
    case 'gmaps_extractor':
      return { query: val('f-query'), location: val('f-location'), limit: intVal('f-limit', 50) };
    case 'website_extractor':
      return { urls: lines('f-urls'), max_pages_per_site: intVal('f-maxpages', 10) };
    case 'social_extractor':
      return { keyword: val('f-keyword'), platform: val('f-platform'), limit: intVal('f-limit', 100) };
    case 'gcontacts_csv':
      return { csv: val('f-csv'), label: val('f-label') };
    default: {
      const raw = val('f-json') || '{}';
      try { return JSON.parse(raw); }
      catch (e) { toast('JSON invalid: ' + e.message, 'error'); return null; }
    }
  }
}

// ---------- Result rendering ----------

function renderResult(el, r) {
  if (!r) {
    el.innerHTML = '<div class="card"><div class="muted">No result.</div></div>';
    return;
  }
  if (r.ok === false) {
    el.innerHTML = `<div class="card"><div class="msg error">Error: ${escapeHtml(r.error || 'unknown')}</div></div>`;
    return;
  }

  const data = r.data !== undefined ? r.data : r;
  let summary = '';
  if (data && typeof data === 'object') {
    if (Array.isArray(data)) {
      summary += `<div><strong>${data.length}</strong> items returned</div>`;
    } else {
      if (data.count !== undefined) summary += `<div>Count: <strong>${escapeHtml(data.count)}</strong></div>`;
      if (data.sent !== undefined) summary += `<div>Sent: <strong style="color:var(--success)">${escapeHtml(data.sent)}</strong></div>`;
      if (data.failed !== undefined) summary += `<div>Failed: <strong style="color:var(--danger)">${escapeHtml(data.failed)}</strong></div>`;
      if (data.export_path) summary += `<div>Export: <code>${escapeHtml(data.export_path)}</code></div>`;
      if (data.path) summary += `<div>Path: <code>${escapeHtml(data.path)}</code></div>`;
      if (data.message) summary += `<div>${escapeHtml(data.message)}</div>`;
    }
  } else {
    summary = `<div>${escapeHtml(String(data))}</div>`;
  }

  let pretty;
  try { pretty = JSON.stringify(data, null, 2); }
  catch (e) { pretty = String(data); }

  const dataTable = renderDataTable(Array.isArray(data) ? data : (data && Array.isArray(data.items) ? data.items : null));

  el.innerHTML = `
    <div class="card-title">Result</div>
    ${summary ? `<div style="margin-bottom:10px">${summary}</div>` : ''}
    ${dataTable}
    <details>
      <summary class="muted small" style="cursor:pointer">Raw output</summary>
      <pre style="background:#f3f4f6;padding:10px;border-radius:4px;max-height:400px;overflow:auto;font-size:12px;margin-top:8px">${escapeHtml(pretty)}</pre>
    </details>
  `;
}

// Tabel data generik dari array-of-objects aktual (tanpa data dummy).
// Dipakai semua fitur yang mengembalikan list (gmaps, ekstraktor, member, ...).
function renderDataTable(data) {
  if (!Array.isArray(data) || !data.length) return '';
  const first = data.find(r => r && typeof r === 'object' && !Array.isArray(r));
  if (!first) return `<div class="muted">${escapeHtml(data.length)} items returned (scalar list — lihat Raw output).</div>`;
  const cols = Object.keys(first).slice(0, 8);
  const rows = data.slice(0, 200).map(r => {
    if (!r || typeof r !== 'object' || Array.isArray(r)) return '';
    return `<tr>${cols.map(c => `<td>${escapeHtml(r[c] ?? '—')}</td>`).join('')}</tr>`;
  }).join('');
  return `
    <div class="table-wrap" style="margin-bottom:10px">
      <table class="table">
        <thead><tr>${cols.map(c => `<th>${escapeHtml(c)}</th>`).join('')}</tr></thead>
        <tbody>${rows}</tbody>
      </table>
    </div>
    <div class="pager"><span>${escapeHtml(data.length)} baris${data.length > 200 ? ' (200 pertama ditampilkan)' : ''}</span></div>
  `;
}

// ---------- Auto-responder rule list ----------

async function refreshResponderRules() {
  const accEl = document.getElementById('f-account');
  const accId = accEl ? (accEl.value ? parseInt(accEl.value) : null) : null;
  const target = document.getElementById('responder-rules');
  if (!target) return;

  try {
    const r = await api.feature_run('auto_responder', { action: 'list', account_id: accId });
    const rules = (r && (r.data || r)) || [];
    if (!Array.isArray(rules) || !rules.length) {
      target.innerHTML = '<div class="card"><div class="muted">No rules yet.</div></div>';
      return;
    }
    let html = `
      <div class="card" style="padding:0">
        <table class="table">
          <thead><tr><th>Pattern</th><th>Regex?</th><th>Response</th><th>Account</th></tr></thead>
          <tbody>
    `;
    for (const rule of rules) {
      html += `
        <tr>
          <td><code>${escapeHtml(rule.pattern || '')}</code></td>
          <td>${rule.is_regex ? '<span class="badge badge-warn">regex</span>' : '<span class="badge badge-muted">text</span>'}</td>
          <td>${escapeHtml(rule.response || '')}</td>
          <td>${escapeHtml(rule.account_id ?? '—')}</td>
        </tr>
      `;
    }
    html += '</tbody></table></div>';
    target.innerHTML = html;
  } catch (e) {
    target.innerHTML = `<div class="card"><div class="msg error">Failed to load rules: ${escapeHtml(e.message || e)}</div></div>`;
  }
}

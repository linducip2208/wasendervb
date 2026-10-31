/* Dashboard — overview + feature catalog. */

router.register('dashboard', async (root) => {
  const [
    features, stats, accounts, chart,
    leads, fails, warmer, schedules, recent, lic,
    heatmap, health, alerts, campaigns,
  ] = await Promise.all([
    api.feature_list().catch(() => []),
    api.stats().catch(() => ({})),
    api.accounts_overview().catch(() => []),
    api.send_chart(14).catch(() => []),
    api.leads_by_source().catch(() => []),
    api.fail_reasons(5).catch(() => []),
    api.warmer_progress().catch(() => []),
    api.list_schedules().catch(() => []),
    api.message_log(null, 10).catch(() => []),
    api.license_status().catch(() => null),
    api.send_heatmap(30).catch(() => []),
    api.account_health().catch(() => []),
    api.alerts().catch(() => []),
    api.campaigns().catch(() => []),
  ]);

  // Group features by category, preserving order
  const order = ['Data Extractors', 'Group Tools', 'Automations', 'Lead Generation', 'Messaging'];
  const groups = {};
  for (const f of features) {
    (groups[f.category] = groups[f.category] || []).push(f);
  }

  let html = `
    <div class="page-header">
      <div>
        <div class="page-title">Tools</div>
        <div class="page-subtitle">${features.length} features available</div>
      </div>
      ${licenseChip(lic)}
    </div>

    ${alerts.length ? `<div class="alert-banner">${alerts.map((a) => `
      <div class="msg ${a.level === 'danger' ? 'error' : a.level === 'warn' ? 'warning' : 'info'}">⚠ ${escapeHtml(a.text)}</div>
    `).join('')}</div>` : ''}

    <div class="stats-grid">
      <div class="stat-card"><div class="stat-value">${stats.messages_total ?? 0}</div><div class="stat-label">Total messages</div></div>
      <div class="stat-card"><div class="stat-value" style="color:var(--success)">${stats.sent ?? 0}</div><div class="stat-label">Sent</div></div>
      <div class="stat-card"><div class="stat-value" style="color:var(--danger)">${stats.failed ?? 0}</div><div class="stat-label">Failed</div></div>
      <div class="stat-card"><div class="stat-value">${stats.contacts ?? 0}</div><div class="stat-label">Contacts</div></div>
      <div class="stat-card"><div class="stat-value">${stats.leads ?? 0}</div><div class="stat-label">Leads</div></div>
    </div>

    <div class="card">
      <div class="card-title">Accounts quota &amp; status</div>
      ${accounts.length ? accounts.map(quotaCard).join('') : '<div class="muted small">Belum ada akun. Tambah di tab Accounts.</div>'}
    </div>

    <div class="dash-grid">
      <div class="card">
        <div class="card-title">Send activity <span class="muted small">(14 hari)</span></div>
        ${sendBars(chart)}
      </div>
      <div class="card">
        <div class="card-title">Warmer progress</div>
        ${warmer.length ? warmer.map(warmerCard).join('') : '<div class="muted small">Belum ada aktivitas warmup.</div>'}
      </div>
    </div>

    <div class="dash-grid">
      <div class="card">
        <div class="card-title">Jam kirim tersibuk <span class="muted small">(30 hari)</span></div>
        ${heatmapGrid(heatmap)}
      </div>
      <div class="card">
        <div class="card-title">Health score akun <span class="muted small">(risiko ban)</span></div>
        ${health.length ? health.map(healthCard).join('') : '<div class="muted small">Belum ada akun.</div>'}
      </div>
    </div>

    <div class="card">
      <div class="card-title">Campaigns &amp; reply-rate
        <button class="btn btn-secondary btn-sm" id="export-report" style="float:right">⬇ Export CSV</button>
      </div>
      <div id="export-msg"></div>
      ${campaignTable(campaigns)}
    </div>

    <div class="dash-grid">
      <div class="card">
        <div class="card-title">Leads by source</div>
        ${leadsDonut(leads)}
      </div>
      <div class="card">
        <div class="card-title">Top fail reasons</div>
        ${fails.length ? fails.map((r) => `
          <div class="feed-item"><span class="badge badge-error">${r.count}×</span><span>${escapeHtml(r.error)}</span></div>
        `).join('') : '<div class="muted small">Tidak ada pesan gagal. 🎉</div>'}
      </div>
    </div>

    <div class="dash-grid">
      <div class="card">
        <div class="card-title">Schedules aktif</div>
        ${upcomingSchedules(schedules)}
      </div>
      <div class="card">
        <div class="card-title">Recent activity</div>
        <div class="feed">
          ${recent.length ? recent.map(feedItem).join('') : '<div class="muted small">Belum ada aktivitas.</div>'}
        </div>
      </div>
    </div>
  `;

  for (const cat of order) {
    if (!groups[cat]) continue;
    html += `<div class="section-label">${escapeHtml(cat)}</div>`;
    html += '<div class="feature-grid">';
    for (const f of groups[cat]) {
      html += renderFeatureCard(f);
    }
    html += '</div>';
  }

  root.innerHTML = html;
  root.querySelectorAll('.feature-card').forEach(card => {
    card.querySelector('.feature-action').onclick = () => router.go('feature', { key: card.dataset.key });
  });
  const expBtn = root.querySelector('#export-report');
  if (expBtn) expBtn.onclick = async () => {
    const msg = root.querySelector('#export-msg');
    msg.innerHTML = '<div class="msg info">Membuat laporan…</div>';
    try {
      const res = await api.export_daily_report(30);
      if (res.ok && res.data) msg.innerHTML = `<div class="msg success">Tersimpan: ${escapeHtml(res.data)}</div>`;
      else msg.innerHTML = '<div class="msg warning">Belum ada data untuk diekspor.</div>';
    } catch (e) {
      msg.innerHTML = `<div class="msg error">${escapeHtml(String(e))}</div>`;
    }
  };
});

function licenseChip(lic) {
  if (!lic) return '<span class="lic-chip none">NO LICENSE</span>';
  const days = lic.days_remaining;
  const warn = days !== null && days !== undefined && days < 14;
  const sub = days === null || days === undefined ? 'lifetime' : `${days} hari tersisa`;
  return `<span class="lic-chip${warn ? ' warn' : ''}">${escapeHtml(lic.tier || '')} · ${escapeHtml(String(sub))}</span>`;
}

function quotaCard(a) {
  const cls = a.quota_pct >= 100 ? 'full' : a.quota_pct >= 80 ? 'hot' : '';
  const badge = a.open
    ? (a.wapi_ready ? '<span class="badge badge-success">WAPI ready</span>' : '<span class="badge badge-warn">open</span>')
    : '<span class="badge badge-muted">closed</span>';
  return `
    <div class="quota-card">
      <div class="quota-top">
        <div><strong>${escapeHtml(a.label)}</strong> <span class="muted small">${a.sent_today}/${a.daily_quota}</span></div>
        ${badge}
      </div>
      <div class="progress"><div class="${cls}" style="width:${a.quota_pct}%"></div></div>
    </div>
  `;
}

function sendBars(chart) {
  if (!chart.length) return '<div class="muted small">Belum ada data.</div>';
  const max = Math.max(1, ...chart.map((d) => d.sent + d.failed));
  return `<div class="bars">` + chart.map((d) => {
    const sH = Math.round((d.sent / max) * 130);
    const fH = Math.round((d.failed / max) * 130);
    const label = String(d.day).slice(5);
    return `
      <div class="bar-col" title="${escapeHtml(d.day)}: ${d.sent} sent, ${d.failed} failed">
        <div class="bar-stack" style="height:130px;justify-content:flex-end">
          <div class="b-sent" style="height:${sH}px"></div>
          <div class="b-fail" style="height:${fH}px"></div>
        </div>
        <div class="bar-day">${escapeHtml(label)}</div>
      </div>`;
  }).join('') + `</div>`;
}

const DONUT_COLORS = ['#128C7E', '#25D366', '#f59e0b', '#3b82f6', '#8b5cf6', '#ec4899', '#6b7280'];

function leadsDonut(leads) {
  if (!leads.length) return '<div class="muted small">Belum ada leads.</div>';
  const total = leads.reduce((n, r) => n + r.count, 0) || 1;
  let acc = 0;
  const segs = leads.map((r, i) => {
    const from = (acc / total) * 360;
    acc += r.count;
    const to = (acc / total) * 360;
    return `${DONUT_COLORS[i % DONUT_COLORS.length]} ${from}deg ${to}deg`;
  });
  const legend = leads.map((r, i) => `
    <div><span class="dot" style="background:${DONUT_COLORS[i % DONUT_COLORS.length]}"></span>${escapeHtml(r.source)} — <strong>${r.count}</strong></div>
  `).join('');
  return `
    <div class="donut-wrap">
      <div class="donut" style="background:conic-gradient(${segs.join(',')})"></div>
      <div class="donut-legend">${legend}<div class="muted small">Total: ${total}</div></div>
    </div>`;
}

function warmerCard(w) {
  return `
    <div class="quota-card">
      <div class="quota-top">
        <div><strong>${escapeHtml(w.label)}</strong>
          <span class="muted small">${w.days_active}/${w.warmup_days} hari · ${w.sent_total}/${w.target_total} pesan</span>
        </div>
        ${w.pct >= 100 ? '<span class="badge badge-success">done</span>' : '<span class="badge badge-warn">warming</span>'}
      </div>
      <div class="progress"><div style="width:${w.pct}%"></div></div>
    </div>
  `;
}

function upcomingSchedules(schedules) {
  const active = schedules.filter((s) => s.enabled).slice(0, 5);
  if (!active.length) return '<div class="muted small">Tidak ada jadwal aktif.</div>';
  return active.map((s) => `
    <div class="feed-item">
      <span class="badge badge-success">ON</span>
      <span><strong>${escapeHtml(s.name)}</strong> <span class="muted small">· ${escapeHtml(s.feature)} · ${escapeHtml(s.cron || s.run_at || '')}</span></span>
    </div>
  `).join('');
}

function feedItem(m) {
  const ok = m.status === 'sent';
  return `
    <div class="feed-item">
      <span class="badge ${ok ? 'badge-success' : 'badge-error'}">${ok ? 'sent' : 'fail'}</span>
      <span class="phone">${escapeHtml(String(m.phone || ''))}</span>
      <span class="time">${escapeHtml(String(m.sent_at || '').slice(0, 16).replace('T', ' '))}</span>
    </div>`;
}

function healthCard(h) {
  const cls = h.status === 'good' ? 'health-good' : h.status === 'warn' ? 'health-warn' : 'health-bad';
  const badge = h.status === 'good' ? 'badge-success' : h.status === 'warn' ? 'badge-warn' : 'badge-error';
  return `
    <div class="quota-card">
      <div class="quota-top">
        <div><strong>${escapeHtml(h.label)}</strong>
          <span class="muted small">gagal ${Math.round(h.fail_rate_7d * 100)}% (7h) · kuota ${Math.round(h.quota_usage * 100)}%${h.warming_penalty ? ' · warmup belum selesai' : ''}</span>
        </div>
        <div style="display:flex;gap:8px;align-items:center">
          <span class="health-score ${cls}">${h.score}</span>
          <span class="badge ${badge}">${h.status}</span>
        </div>
      </div>
    </div>
  `;
}

const DOW_ID = ['Min', 'Sen', 'Sel', 'Rab', 'Kam', 'Jum', 'Sab'];

function heatmapGrid(cells) {
  if (!cells.length) return '<div class="muted small">Belum ada data.</div>';
  const map = {};
  let max = 1;
  for (const c of cells) {
    map[`${c.dow}:${c.hour}`] = c;
    max = Math.max(max, c.sent + c.failed);
  }
  let html = '<div class="heatmap"><div></div>';
  for (let h = 0; h < 24; h++) html += `<div class="hh">${h % 3 === 0 ? h : ''}</div>`;
  for (let d = 0; d < 7; d++) {
    html += `<div class="hd">${DOW_ID[d]}</div>`;
    for (let h = 0; h < 24; h++) {
      const c = map[`${d}:${h}`];
      const v = c ? c.sent + c.failed : 0;
      const alpha = c ? (0.15 + 0.85 * (v / max)) : 0;
      const bg = c ? `background:rgba(18,140,126,${alpha.toFixed(2)})` : '';
      const tip = c ? `${DOW_ID[d]} jam ${h}: ${c.sent} sent, ${c.failed} failed` : `${DOW_ID[d]} jam ${h}: -`;
      html += `<div class="cell" style="${bg}" title="${escapeHtml(tip)}"></div>`;
    }
  }
  return html + '</div>';
}

function campaignTable(campaigns) {
  if (!campaigns.length) return '<div class="muted small">Belum ada campaign. Isi field campaign saat bulk send.</div>';
  return `
    <table class="table">
      <thead><tr><th>Campaign</th><th>Terkirim</th><th>Gagal</th><th>Dibalas</th><th>Reply-rate</th></tr></thead>
      <tbody>
        ${campaigns.map((c) => `
          <tr>
            <td><strong>${escapeHtml(c.campaign)}</strong> <span class="muted small">(${c.phones} kontak)</span></td>
            <td>${c.sent}</td>
            <td>${c.failed}</td>
            <td>${c.replies}</td>
            <td>
              <div class="progress" style="min-width:90px"><div style="width:${Math.round(c.reply_rate * 100)}%"></div></div>
              <span class="small muted">${Math.round(c.reply_rate * 100)}%</span>
            </td>
          </tr>
        `).join('')}
      </tbody>
    </table>`;
}

function renderFeatureCard(f) {
  return `
    <div class="feature-card" data-key="${escapeHtml(f.key)}">
      <div class="feature-icon">${iconFor(f.key)}</div>
      <div class="feature-name">${escapeHtml(f.label)}</div>
      <button class="feature-action">▶ START NOW</button>
    </div>
  `;
}

function iconFor(key) {
  const map = {
    get_group_member: '👥', contact_list: '📒', active_members: '🗣️',
    chat_list: '💬', poll_results: '📊', grab_group_links: '🔗',
    auto_joiner: '➕', bulk_add_members: '👥', group_finder: '🔍',
    bulk_group_gen: '🆕', auto_responder: '🤖', number_filter: '☑️',
    wa_warmer: '🔥', group_warmer: '🔥', gmaps_extractor: '🗺️',
    website_extractor: '🌐', social_extractor: '📱', gcontacts_csv: '📄',
    bulk_send: '✉️',
  };
  return `<div style="font-size:36px">${map[key] || '⚙️'}</div>`;
}

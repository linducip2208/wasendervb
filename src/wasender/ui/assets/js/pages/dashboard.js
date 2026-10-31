/* Dashboard — overview + feature catalog. */

router.register('dashboard', async (root) => {
  const [
    features, stats, accounts, chart,
    leads, fails, warmer, schedules, recent, lic,
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

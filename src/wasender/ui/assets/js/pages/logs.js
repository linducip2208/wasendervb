/* Logs page — message log viewer with filter & stats. */

router.register('logs', async (root, params = {}) => {
  const accounts = await api.list_accounts();
  const stats = await api.stats();
  const filterAccId = params.accountId ? parseInt(params.accountId) : null;
  const filterDir = params.direction || '';
  const logs = await api.message_log(filterAccId, 200);
  const shown = filterDir ? logs.filter((l) => (l.direction || 'out') === filterDir) : logs;

  const truncate = (s, n) => {
    s = String(s ?? '');
    return s.length > n ? s.slice(0, n - 1) + '…' : s;
  };
  const statusBadge = (st) => {
    const s = String(st || '').toLowerCase();
    if (s === 'sent') return '<span class="badge badge-success">sent</span>';
    if (s === 'failed') return '<span class="badge badge-error">failed</span>';
    if (s === 'received') return '<span class="badge badge-muted">received</span>';
    return `<span class="badge badge-warn">${escapeHtml(st || '—')}</span>`;
  };

  let html = `
    <div class="page-header">
      <div class="page-title">Message Logs</div>
    </div>

    <div class="stats-grid">
      <div class="stat-card"><div class="stat-value">${stats?.messages_total ?? 0}</div><div class="stat-label">Total</div></div>
      <div class="stat-card"><div class="stat-value" style="color:var(--success)">${stats?.sent ?? 0}</div><div class="stat-label">Sent</div></div>
      <div class="stat-card"><div class="stat-value" style="color:var(--danger)">${stats?.failed ?? 0}</div><div class="stat-label">Failed</div></div>
      <div class="stat-card"><div class="stat-value">${stats?.contacts ?? 0}</div><div class="stat-label">Contacts</div></div>
      <div class="stat-card"><div class="stat-value">${stats?.leads ?? 0}</div><div class="stat-label">Leads</div></div>
    </div>

    <div class="card">
      <div class="form-row" style="grid-template-columns:1fr 1fr auto;align-items:end">
        <div class="form-group" style="margin:0">
          <label>Filter by account</label>
          <select id="log-account">
            <option value="">All accounts</option>
            ${accounts.map(a => `<option value="${a.id}" ${filterAccId === a.id ? 'selected' : ''}>${escapeHtml(a.label)}${a.phone ? ' — ' + escapeHtml(a.phone) : ''}</option>`).join('')}
          </select>
        </div>
        <div class="form-group" style="margin:0">
          <label>Direction</label>
          <select id="log-direction">
            <option value="">Out + In</option>
            <option value="out" ${filterDir === 'out' ? 'selected' : ''}>Outgoing only</option>
            <option value="in" ${filterDir === 'in' ? 'selected' : ''}>Incoming only</option>
          </select>
        </div>
        <button class="btn btn-secondary" id="log-refresh">Refresh</button>
      </div>
    </div>

    <div class="card" style="padding:0">
      <table class="table">
        <thead>
          <tr>
            <th>Sent at</th>
            <th>Dir</th>
            <th>Phone</th>
            <th>Account</th>
            <th>Status</th>
            <th>Campaign</th>
            <th>Message</th>
            <th>Error</th>
          </tr>
        </thead>
        <tbody>
  `;

  if (!shown.length) {
    html += '<tr><td colspan="8" class="muted text-center" style="padding:20px">No messages yet.</td></tr>';
  } else {
    for (const l of shown) {
      const when = l.sent_at || l.created_at || '';
      const dir = l.direction || 'out';
      html += `
        <tr>
          <td class="small muted">${escapeHtml(when)}</td>
          <td>${dir === 'in' ? '⬅' : '➡'}</td>
          <td>${escapeHtml(l.phone || '—')}</td>
          <td>${escapeHtml(l.account_id ?? '—')}</td>
          <td>${statusBadge(l.status)}</td>
          <td class="small muted">${escapeHtml(l.campaign || '—')}</td>
          <td title="${escapeHtml(l.message || '')}">${escapeHtml(truncate(l.message, 60))}</td>
          <td class="small" style="color:var(--danger)">${escapeHtml(truncate(l.error, 60))}</td>
        </tr>
      `;
    }
  }
  html += '</tbody></table></div>';

  root.innerHTML = html;

  const go = () => {
    const acc = document.getElementById('log-account').value;
    const dir = document.getElementById('log-direction').value;
    router.go('logs', { accountId: acc ? parseInt(acc) : null, direction: dir || null });
  };
  document.getElementById('log-account').onchange = go;
  document.getElementById('log-direction').onchange = go;
  document.getElementById('log-refresh').onclick = () => router.go('logs', { accountId: filterAccId, direction: filterDir || null });
});

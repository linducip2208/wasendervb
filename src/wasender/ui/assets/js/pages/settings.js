/* Settings page — anti-ban config, license info, about. */

router.register('settings', async (root) => {
  const cfg = (await api.get_config()) || {};
  const lic = await api.license_status();
  const hwidInfo = await api.get_hwid();

  const v = (k, def = '') => (cfg[k] !== undefined && cfg[k] !== null ? cfg[k] : def);
  const checked = (k) => (cfg[k] ? 'checked' : '');

  let html = `
    <div class="page-header">
      <div class="page-title">Settings</div>
    </div>

    <div class="card">
      <div class="card-title">Anti-Ban &amp; Sending Behavior</div>
      <div class="form-row">
        <div class="form-group">
          <label>Delay min (seconds)</label>
          <input id="cfg-delay-min" type="number" min="1" max="300" value="${escapeHtml(v('delay_min_seconds', 3))}" />
        </div>
        <div class="form-group">
          <label>Delay max (seconds)</label>
          <input id="cfg-delay-max" type="number" min="1" max="600" value="${escapeHtml(v('delay_max_seconds', 15))}" />
        </div>
      </div>
      <div class="form-row">
        <div class="form-group">
          <label>Daily quota per account</label>
          <input id="cfg-quota" type="number" min="10" max="5000" value="${escapeHtml(v('daily_quota_per_account', 200))}" />
          <div class="field-hint small muted">Anti-ban: 200 untuk akun lama, 100 untuk akun baru.</div>
        </div>
        <div class="form-group">
          <label>Warmup days for new accounts</label>
          <input id="cfg-warmup-days" type="number" min="1" max="30" value="${escapeHtml(v('warmup_days', 7))}" />
        </div>
      </div>
      <div class="form-row">
        <div class="form-group">
          <label>Warmup messages per day</label>
          <input id="cfg-warmup-msgs" type="number" min="5" max="200" value="${escapeHtml(v('warmup_messages_per_day', 20))}" />
        </div>
        <div class="form-group">
          <label style="display:flex;align-items:center;gap:8px;margin-top:24px">
            <input id="cfg-typing" type="checkbox" style="width:auto" ${checked('typing_simulation')} />
            Typing simulation
          </label>
        </div>
      </div>
      <div class="form-group">
        <label style="display:flex;align-items:center;gap:8px">
          <input id="cfg-warmup-required" type="checkbox" style="width:auto" ${checked('warmup_required_for_new')} />
          Require warmup for new accounts
        </label>
      </div>
      <div class="row-end">
        <button class="btn btn-primary" id="cfg-save">Save Settings</button>
      </div>
    </div>

    <div class="card">
      <div class="card-title">License</div>
  `;

  if (lic) {
    const days = lic.days_remaining;
    const dayBadge = days == null ? '' :
      days < 0 ? '<span class="badge badge-error">expired</span>' :
      days < 14 ? `<span class="badge badge-warn">${days} days left</span>` :
      `<span class="badge badge-success">${days} days left</span>`;
    html += `
      <table class="table">
        <tbody>
          <tr><td style="width:180px"><strong>License ID</strong></td><td><code>${escapeHtml(lic.license_id || '—')}</code></td></tr>
          <tr><td><strong>Customer</strong></td><td>${escapeHtml(lic.customer || '—')}</td></tr>
          <tr><td><strong>Tier</strong></td><td><span class="badge badge-success">${escapeHtml(lic.tier || '—')}</span></td></tr>
          <tr><td><strong>Expires at</strong></td><td>${escapeHtml(lic.expires_at || '—')} ${dayBadge}</td></tr>
          <tr><td><strong>Features</strong></td><td class="small">${Array.isArray(lic.features) ? lic.features.map(f => `<span class="badge badge-muted" style="margin-right:4px">${escapeHtml(f)}</span>`).join('') : '—'}</td></tr>
        </tbody>
      </table>
      <div class="row-end" style="margin-top:12px">
        <button class="btn btn-danger" id="lic-uninstall">Uninstall License</button>
      </div>
    `;
  } else {
    html += '<div class="muted">No license installed. Install license dari layar startup atau hubungi vendor.</div>';
  }
  html += '</div>';

  // HWID
  const hwidShort = hwidInfo?.short || '';
  const hwidFull = hwidInfo?.hwid || '';
  html += `
    <div class="card">
      <div class="card-title">Hardware ID (HWID)</div>
      <div class="muted small" style="margin-bottom:8px">Berikan HWID ini ke vendor saat request license. Setiap PC unik.</div>
      <div class="form-row" style="grid-template-columns:1fr auto">
        <input id="hwid-display" readonly value="${escapeHtml(hwidFull)}" style="font-family:Consolas,monospace" />
        <button class="btn btn-secondary" id="hwid-copy">Copy</button>
      </div>
      <div class="muted small" style="margin-top:6px">Short: <code>${escapeHtml(hwidShort)}</code></div>
    </div>

    <div class="card">
      <div class="card-title">About</div>
      <div><strong>WASender</strong> v1.0.0</div>
      <div class="muted small" style="margin-top:6px">Desktop WhatsApp automation suite — Python + WebView2.</div>
      <div class="msg warning" style="margin-top:12px">
        <strong>Disclaimer:</strong> Penggunaan tool ini di luar tanggung jawab developer. WhatsApp dapat membatasi atau memblokir akun yang melanggar Terms of Service mereka. Gunakan dengan bijak: warmup akun baru, hormati delay anti-spam, jangan kirim ke kontak yang tidak opt-in.
      </div>
    </div>
  `;

  root.innerHTML = html;

  document.getElementById('cfg-save').onclick = async () => {
    const payload = {
      delay_min_seconds: parseInt(document.getElementById('cfg-delay-min').value) || 3,
      delay_max_seconds: parseInt(document.getElementById('cfg-delay-max').value) || 15,
      daily_quota_per_account: parseInt(document.getElementById('cfg-quota').value) || 200,
      warmup_days: parseInt(document.getElementById('cfg-warmup-days').value) || 7,
      warmup_messages_per_day: parseInt(document.getElementById('cfg-warmup-msgs').value) || 20,
      typing_simulation: document.getElementById('cfg-typing').checked,
      warmup_required_for_new: document.getElementById('cfg-warmup-required').checked,
    };
    if (payload.delay_min_seconds > payload.delay_max_seconds) {
      toast('Delay min tidak boleh > delay max', 'error'); return;
    }
    const r = await api.save_config(payload);
    if (r.ok) toast('Settings saved', 'success');
    else toast(r.error || 'Save failed', 'error');
  };

  const uninstallBtn = document.getElementById('lic-uninstall');
  if (uninstallBtn) {
    uninstallBtn.onclick = async () => {
      if (!confirm('Yakin uninstall license? Aplikasi akan minta license baru saat restart.')) return;
      const r = await api.uninstall_license();
      if (r.ok) { toast('License removed. Restart aplikasi.', 'info'); router.go('settings'); }
      else toast(r.error || 'Uninstall failed', 'error');
    };
  }

  document.getElementById('hwid-copy').onclick = async () => {
    const inp = document.getElementById('hwid-display');
    try {
      await navigator.clipboard.writeText(inp.value);
      toast('HWID copied to clipboard', 'success');
    } catch (e) {
      inp.select();
      document.execCommand('copy');
      toast('HWID copied', 'success');
    }
  };
});

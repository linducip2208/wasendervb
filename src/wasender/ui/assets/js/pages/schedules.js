/* Schedules page — manage cron / one-time scheduled jobs. */

router.register('schedules', async (root) => {
  const schedules = await api.list_schedules();
  const features = await api.feature_list();

  let html = `
    <div class="page-header">
      <div class="page-title">Schedules</div>
      <button class="btn btn-primary" id="add-schedule">+ Add Schedule</button>
    </div>

    <div class="card" style="padding:0">
  `;

  if (!schedules.length) {
    html += '<div class="muted" style="padding:20px">No schedules yet. Klik <strong>+ Add Schedule</strong> untuk bikin job pertama.</div>';
  } else {
    html += `
      <table class="table">
        <thead>
          <tr>
            <th>Name</th>
            <th>Feature</th>
            <th>Cron / Run At</th>
            <th>Last run</th>
            <th>Enabled</th>
            <th>Actions</th>
          </tr>
        </thead>
        <tbody>
    `;
    for (const s of schedules) {
      const when = s.cron ? `cron: <code>${escapeHtml(s.cron)}</code>` : (s.run_at ? `at: ${escapeHtml(s.run_at)}` : '—');
      const last = s.last_run ? escapeHtml(s.last_run) : '<span class="muted">never</span>';
      const enabled = !!s.enabled;
      html += `
        <tr data-id="${s.id}">
          <td><strong>${escapeHtml(s.name)}</strong></td>
          <td><span class="badge badge-muted">${escapeHtml(s.feature)}</span></td>
          <td class="small">${when}</td>
          <td class="small">${last}</td>
          <td>
            <button class="btn btn-sm ${enabled ? 'btn-primary' : 'btn-secondary'}" data-act="toggle" data-enabled="${enabled ? 1 : 0}">
              ${enabled ? 'Enabled' : 'Disabled'}
            </button>
          </td>
          <td>
            <button class="btn btn-sm btn-danger" data-act="remove">Delete</button>
          </td>
        </tr>
      `;
    }
    html += '</tbody></table>';
  }
  html += '</div>';

  root.innerHTML = html;

  document.getElementById('add-schedule').onclick = () => {
    const featureOpts = features.map(f => `<option value="${escapeHtml(f.key)}">${escapeHtml(f.label)} (${escapeHtml(f.category)})</option>`).join('');
    modal({
      title: 'Add Schedule',
      body: `
        <div class="form-group">
          <label>Name</label>
          <input id="sc-name" placeholder="Daily morning blast" />
        </div>
        <div class="form-group">
          <label>Feature</label>
          <select id="sc-feature">${featureOpts}</select>
        </div>
        <div class="form-group">
          <label>Schedule type</label>
          <select id="sc-type">
            <option value="cron">Cron (recurring)</option>
            <option value="once">One-time (run at)</option>
          </select>
        </div>
        <div class="form-group" id="sc-cron-row">
          <label>Cron expression</label>
          <input id="sc-cron" placeholder="0 9 * * *  (every day at 09:00)" />
          <div class="field-hint small muted">Format: <code>min hour day month dow</code></div>
        </div>
        <div class="form-group" id="sc-once-row" style="display:none">
          <label>Run at</label>
          <input id="sc-runat" type="datetime-local" />
        </div>
        <div class="form-group">
          <label>Feature config (JSON)</label>
          <textarea id="sc-config" rows="6" placeholder='{"account_id": 1, "message": "Hi"}'>{}</textarea>
        </div>
      `,
      onConfirm: async (body) => {
        const name = body.querySelector('#sc-name').value.trim();
        const feature = body.querySelector('#sc-feature').value;
        const type = body.querySelector('#sc-type').value;
        const cron = type === 'cron' ? body.querySelector('#sc-cron').value.trim() : null;
        const runAt = type === 'once' ? body.querySelector('#sc-runat').value.trim() : null;
        const cfgRaw = body.querySelector('#sc-config').value.trim() || '{}';

        if (!name) { toast('Name wajib diisi', 'error'); return false; }
        if (!feature) { toast('Pilih feature dulu', 'error'); return false; }
        if (type === 'cron' && !cron) { toast('Cron expression wajib', 'error'); return false; }
        if (type === 'once' && !runAt) { toast('Run-at wajib', 'error'); return false; }

        let cfg;
        try { cfg = JSON.parse(cfgRaw); }
        catch (e) { toast('Config bukan JSON valid: ' + e.message, 'error'); return false; }

        const r = await api.add_schedule(name, feature, cfg, cron, runAt);
        if (r.ok) { toast('Schedule added', 'success'); router.go('schedules'); }
        else { toast(r.error, 'error'); return false; }
      },
      confirmLabel: 'Add',
    });

    // toggle visibility cron vs once
    const sel = document.querySelector('#sc-type');
    if (sel) sel.onchange = () => {
      const isCron = sel.value === 'cron';
      document.querySelector('#sc-cron-row').style.display = isCron ? '' : 'none';
      document.querySelector('#sc-once-row').style.display = isCron ? 'none' : '';
    };
  };

  root.querySelectorAll('tr[data-id]').forEach(tr => {
    const id = parseInt(tr.dataset.id);
    tr.querySelector('[data-act="toggle"]').onclick = async (ev) => {
      const btn = ev.currentTarget;
      const wasEnabled = btn.dataset.enabled === '1';
      const r = await api.toggle_schedule(id, !wasEnabled);
      if (r.ok) { toast(wasEnabled ? 'Disabled' : 'Enabled', 'info'); router.go('schedules'); }
      else toast(r.error, 'error');
    };
    tr.querySelector('[data-act="remove"]').onclick = async () => {
      if (!confirm('Hapus schedule ini?')) return;
      const r = await api.remove_schedule(id);
      if (r.ok) { toast('Deleted', 'info'); router.go('schedules'); }
      else toast(r.error, 'error');
    };
  });
});

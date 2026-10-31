/* Contacts page — list contacts, import dari CSV. */

router.register('contacts', async (root) => {
  const accounts = await api.list_accounts();
  const contacts = await api.list_contacts();

  let html = `
    <div class="page-header">
      <div class="page-title">Contacts</div>
      <button class="btn btn-primary" id="import-csv">Import CSV</button>
    </div>

    <div class="card">
      <div class="muted">Total: ${contacts.length} contacts</div>
    </div>

    <div class="card" style="padding:0">
      <table class="table">
        <thead><tr><th>Phone</th><th>Name</th><th>Tags</th><th>Account</th><th>Created</th></tr></thead>
        <tbody>
          ${contacts.length ? contacts.map(c => `
            <tr><td>${escapeHtml(c.phone)}</td><td>${escapeHtml(c.name || '')}</td><td>${escapeHtml(c.tags || '')}</td><td>${c.account_id}</td><td class="muted small">${escapeHtml(c.created_at)}</td></tr>
          `).join('') : '<tr><td colspan="5" class="muted text-center" style="padding:20px">No contacts. Import CSV untuk mulai.</td></tr>'}
        </tbody>
      </table>
    </div>
  `;

  root.innerHTML = html;

  document.getElementById('import-csv').onclick = () => {
    if (accounts.length === 0) { toast('Add WhatsApp account dulu di tab Accounts', 'warning'); return; }
    modal({
      title: 'Import Contacts (CSV)',
      body: `
        <div class="form-group">
          <label>Account untuk associate kontak ini</label>
          <select id="ic-account">
            ${accounts.map(a => `<option value="${a.id}">${escapeHtml(a.label)}</option>`).join('')}
          </select>
        </div>
        <div class="form-group">
          <label>CSV content (header: phone,name,tags)</label>
          <textarea id="ic-csv" rows="10" placeholder="phone,name,tags\n628123456789,John,vip\n628987654321,Jane,follower"></textarea>
        </div>
      `,
      onConfirm: async (body) => {
        const accId = parseInt(body.querySelector('#ic-account').value);
        const text = body.querySelector('#ic-csv').value.trim();
        if (!text) { toast('CSV kosong', 'error'); return false; }
        const lines = text.split(/\r?\n/);
        const header = lines.shift().split(',').map(h => h.trim().toLowerCase());
        const rows = lines.filter(l => l.trim()).map(line => {
          const cols = line.split(',');
          const r = {};
          header.forEach((h, i) => r[h] = (cols[i] || '').trim());
          return r;
        });
        const r = await api.import_contacts(accId, rows);
        if (r.ok) { toast(`Imported ${r.data} contacts`, 'success'); router.go('contacts'); }
        else { toast(r.error, 'error'); return false; }
      },
      confirmLabel: 'Import',
    });
  };
});

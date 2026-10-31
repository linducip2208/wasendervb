/* Accounts page — manage WhatsApp connections (multiple accounts). */

router.register('accounts', async (root) => {
  const accounts = await api.list_accounts();

  let html = `
    <div class="page-header">
      <div class="page-title">WhatsApp Accounts</div>
      <button class="btn btn-primary" id="add-account">+ Add Account</button>
    </div>

    <div class="card">
  `;

  if (accounts.length === 0) {
    html += '<div class="muted">No accounts yet. Klik <strong>+ Add Account</strong> untuk hubungkan WhatsApp pertama.</div>';
  } else {
    html += '<table class="table"><thead><tr><th>Label</th><th>Phone</th><th>Quota</th><th>Sent today</th><th>Actions</th></tr></thead><tbody>';
    for (const a of accounts) {
      html += `
        <tr data-id="${a.id}">
          <td><strong>${escapeHtml(a.label)}</strong></td>
          <td>${escapeHtml(a.phone || '—')}</td>
          <td>${a.daily_quota}</td>
          <td>${a.sent_today || 0}</td>
          <td>
            <button class="btn btn-sm btn-primary" data-act="open">Open</button>
            <button class="btn btn-sm btn-secondary" data-act="close">Close</button>
            <button class="btn btn-sm btn-danger" data-act="remove">Remove</button>
          </td>
        </tr>
      `;
    }
    html += '</tbody></table>';
  }
  html += '</div>';

  html += `
    <div class="card">
      <div class="card-title">Tips Anti-Ban</div>
      <ul style="padding-left:20px;line-height:1.8">
        <li>Akun baru wajib warmup 7 hari (Tools → WhatsApp Warmer) sebelum bulk send</li>
        <li>Daily quota default 200 — naikkan bertahap setelah 1 minggu tanpa flag</li>
        <li>Jangan kirim pesan identik ke ratusan kontak — variasikan template</li>
        <li>Pakai delay min 5-15 detik antar pesan</li>
      </ul>
    </div>
  `;

  root.innerHTML = html;

  document.getElementById('add-account').onclick = () => {
    modal({
      title: 'Add WhatsApp Account',
      body: `
        <div class="form-group">
          <label>Label / Nama akun</label>
          <input id="acc-label" placeholder="Akun Marketing" />
        </div>
        <div class="form-group">
          <label>Daily quota (max pesan/hari)</label>
          <input id="acc-quota" type="number" value="200" min="10" max="2000" />
          <div class="field-hint">Anti-ban: mulai 100-200 untuk akun baru, naikkan bertahap.</div>
        </div>
      `,
      onConfirm: async (body) => {
        const label = body.querySelector('#acc-label').value.trim();
        const quota = parseInt(body.querySelector('#acc-quota').value) || 200;
        if (!label) { toast('Label wajib diisi', 'error'); return false; }
        const r = await api.add_account(label, quota);
        if (r.ok) { toast('Account added', 'success'); router.go('accounts'); }
        else { toast(r.error, 'error'); return false; }
      },
      confirmLabel: 'Add',
    });
  };

  root.querySelectorAll('tr[data-id]').forEach(tr => {
    const id = parseInt(tr.dataset.id);
    tr.querySelector('[data-act="open"]').onclick = async () => {
      const r = await api.open_account(id);
      if (r.ok) toast('WA window opened — scan QR jika belum login', 'success');
      else toast(r.error, 'error');
    };
    tr.querySelector('[data-act="close"]').onclick = async () => {
      await api.close_account(id);
      toast('Closed', 'info');
    };
    tr.querySelector('[data-act="remove"]').onclick = async () => {
      if (!confirm('Hapus akun ini? Session WA juga akan dihapus.')) return;
      const r = await api.remove_account(id);
      if (r.ok) router.go('accounts');
    };
  });
});

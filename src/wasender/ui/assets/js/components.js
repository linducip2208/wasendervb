/* Reusable UI components. */

function modal({ title, body, onConfirm, confirmLabel = 'Save', cancelLabel = 'Cancel' }) {
  const root = document.getElementById('modal-root');
  root.innerHTML = `
    <div class="modal-backdrop">
      <div class="modal">
        <div class="modal-header">
          <div class="modal-title">${escapeHtml(title)}</div>
          <button class="modal-close">×</button>
        </div>
        <div class="modal-body">${body}</div>
        <div class="modal-footer">
          <button class="btn btn-secondary" id="modal-cancel">${escapeHtml(cancelLabel)}</button>
          ${onConfirm ? `<button class="btn btn-primary" id="modal-confirm">${escapeHtml(confirmLabel)}</button>` : ''}
        </div>
      </div>
    </div>
  `;
  const close = () => { root.innerHTML = ''; };
  root.querySelector('.modal-close').onclick = close;
  root.querySelector('#modal-cancel').onclick = close;
  if (onConfirm) {
    root.querySelector('#modal-confirm').onclick = async () => {
      const result = await onConfirm(root.querySelector('.modal-body'));
      if (result !== false) close();
    };
  }
  return { close, root };
}

function toast(message, type = 'info', timeout = 4000) {
  let host = document.getElementById('toast-host');
  if (!host) {
    host = document.createElement('div');
    host.id = 'toast-host';
    host.style.cssText = 'position:fixed;bottom:20px;right:20px;z-index:9999;display:flex;flex-direction:column;gap:8px';
    document.body.appendChild(host);
  }
  const t = document.createElement('div');
  t.className = `msg ${type}`;
  t.style.cssText = 'min-width:240px;max-width:380px;box-shadow:0 4px 12px rgba(0,0,0,0.18)';
  t.textContent = message;
  host.appendChild(t);
  setTimeout(() => t.remove(), timeout);
}

function escapeHtml(s) {
  return String(s ?? '').replace(/[&<>"']/g, c => ({
    '&': '&amp;', '<': '&lt;', '>': '&gt;', '"': '&quot;', "'": '&#39;'
  })[c]);
}

window.modal = modal;
window.toast = toast;
window.escapeHtml = escapeHtml;

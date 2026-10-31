/* Simple hash-based router. */

const router = {
  routes: {},
  current: null,

  register(name, renderFn) {
    this.routes[name] = renderFn;
  },

  async go(name, params = {}) {
    const fn = this.routes[name];
    if (!fn) {
      console.error('Unknown route', name);
      return;
    }
    document.querySelectorAll('.nav-item').forEach(b => b.classList.toggle('active', b.dataset.page === name));
    const root = document.getElementById('page');
    root.innerHTML = '<div class="muted">Loading...</div>';
    try {
      await fn(root, params);
    } catch (e) {
      console.error(e);
      root.innerHTML = `<div class="msg error">Render error: ${e.message}</div>`;
    }
    this.current = name;
  },
};

window.router = router;

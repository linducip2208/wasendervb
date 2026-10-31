/* Main app bootstrap: license check, sidebar wiring, default route. */

(async () => {
  const lic = await api.license_status();
  const licInfoEl = document.getElementById('license-info');
  if (lic) {
    const exp = lic.expires_at ? `expires ${lic.expires_at}` : 'lifetime';
    licInfoEl.textContent = `${lic.tier.toUpperCase()} · ${exp}`;
    if (lic.days_remaining != null && lic.days_remaining < 14) {
      licInfoEl.style.background = 'rgba(255,200,0,0.3)';
    }
  } else {
    licInfoEl.textContent = 'NO LICENSE';
    licInfoEl.style.background = 'rgba(255,80,80,0.4)';
  }

  document.querySelectorAll('.nav-item').forEach(btn => {
    btn.addEventListener('click', () => router.go(btn.dataset.page));
  });

  document.getElementById('toggle-sidebar').addEventListener('click', () => {
    document.querySelector('.layout').classList.toggle('sidebar-collapsed');
  });

  await router.go('dashboard');
})();

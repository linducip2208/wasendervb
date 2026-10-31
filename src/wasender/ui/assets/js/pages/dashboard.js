/* Dashboard — feature catalog matching the reference screenshot layout. */

router.register('dashboard', async (root) => {
  const features = await api.feature_list();
  const stats = await api.stats();

  // Group by category, preserving order
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
    </div>

    <div class="stats-grid">
      <div class="stat-card"><div class="stat-value">${stats.messages_total ?? 0}</div><div class="stat-label">Total messages</div></div>
      <div class="stat-card"><div class="stat-value" style="color:var(--success)">${stats.sent ?? 0}</div><div class="stat-label">Sent</div></div>
      <div class="stat-card"><div class="stat-value" style="color:var(--danger)">${stats.failed ?? 0}</div><div class="stat-label">Failed</div></div>
      <div class="stat-card"><div class="stat-value">${stats.contacts ?? 0}</div><div class="stat-label">Contacts</div></div>
      <div class="stat-card"><div class="stat-value">${stats.leads ?? 0}</div><div class="stat-label">Leads</div></div>
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

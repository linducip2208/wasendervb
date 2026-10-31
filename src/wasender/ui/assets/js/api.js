/* Wrapper around pywebview.api with promise-based unwrapping. */

const _waitForBridge = () => new Promise((resolve) => {
  if (window.pywebview && window.pywebview.api) return resolve();
  window.addEventListener('pywebviewready', () => resolve(), { once: true });
});

const _call = (method) => async (...args) => {
  await _waitForBridge();
  const fn = window.pywebview.api[method];
  if (typeof fn !== 'function') {
    return { ok: false, error: `Method ${method} not exposed` };
  }
  return await fn(...args);
};

const api = {
  get_hwid: async () => (await _call('get_hwid')()).data,
  license_status: async () => (await _call('license_status')()).data,
  install_license: _call('install_license'),
  uninstall_license: _call('uninstall_license'),

  get_config: async () => (await _call('get_config')()).data,
  save_config: _call('save_config'),

  list_accounts: async () => (await _call('list_accounts')()).data || [],
  add_account: _call('add_account'),
  remove_account: _call('remove_account'),
  open_account: _call('open_account'),
  close_account: _call('close_account'),
  account_status: async (id) => (await _call('account_status')(id)).data,

  list_contacts: async (accId) => (await _call('list_contacts')(accId)).data || [],
  import_contacts: _call('import_contacts'),

  list_schedules: async () => (await _call('list_schedules')()).data || [],
  add_schedule: _call('add_schedule'),
  remove_schedule: _call('remove_schedule'),
  toggle_schedule: _call('toggle_schedule'),

  message_log: async (accId, limit) => (await _call('message_log')(accId, limit || 200)).data || [],
  stats: async () => (await _call('stats')()).data,

  feature_run: _call('feature_run'),
  feature_list: async () => (await _call('feature_list')()).data || [],
};

window.api = api;

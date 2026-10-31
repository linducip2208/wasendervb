using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.IO;
using System.Windows.Forms;

namespace WASender
{
    public partial class AiSettingsForm : Form
    {
        private Form _main;
        private CheckBox chkEnabled;
        private TextBox txtBaseUrl, txtApiKey, txtModel, txtPrompt, txtTemp, txtMaxTok, txtTimeout;
        private CheckBox chkCache, chkFuzzy;
        private TextBox txtTtl, txtMaxEntries;
        private Label lblTest;
        private DataGridView gridOverride, gridSegmen;
        private ListBox lstFaq;

        public AiSettingsForm(Form main)
        {
            _main = main;
            Text = "AI Bot Setup (BYOK)";
            Width = 720;
            Height = 560;
            StartPosition = FormStartPosition.CenterParent;
            FormClosed += delegate { try { if (_main != null) _main.Show(); } catch (Exception) { } };
            BuildUi();
            LoadAll();
        }

        private TabPage NewTab(TabControl tc, string title)
        {
            TabPage p = new TabPage(title);
            p.Padding = new Padding(8);
            tc.TabPages.Add(p);
            return p;
        }

        private Label MkLabel(TabPage p, string t, int x, int y, int w = 130)
        {
            Label l = new Label();
            l.Text = t; l.Left = x; l.Top = y + 3; l.Width = w;
            p.Controls.Add(l);
            return l;
        }

        private TextBox MkText(TabPage p, int x, int y, int w, bool multi = false)
        {
            TextBox tb = new TextBox();
            tb.Left = x; tb.Top = y; tb.Width = w;
            if (multi) { tb.Multiline = true; tb.Height = 90; tb.ScrollBars = ScrollBars.Vertical; }
            p.Controls.Add(tb);
            return tb;
        }

        private Button MkBtn(TabPage p, string t, int x, int y, EventHandler h)
        {
            Button b = new Button();
            b.Text = t; b.Left = x; b.Top = y; b.Width = 150;
            b.Click += h;
            p.Controls.Add(b);
            return b;
        }

        private void BuildUi()
        {
            TabControl tc = new TabControl();
            tc.Dock = DockStyle.Fill;
            Controls.Add(tc);

            TabPage tConn = NewTab(tc, "Koneksi");
            chkEnabled = new CheckBox();
            chkEnabled.Text = "Aktifkan AI (Enabled)";
            chkEnabled.Left = 150; chkEnabled.Top = 10; chkEnabled.Width = 200;
            tConn.Controls.Add(chkEnabled);
            MkLabel(tConn, "Base URL", 10, 40); txtBaseUrl = MkText(tConn, 150, 38, 420);
            MkLabel(tConn, "API Key", 10, 68); txtApiKey = MkText(tConn, 150, 66, 420);
            txtApiKey.UseSystemPasswordChar = true;
            MkLabel(tConn, "Model", 10, 96); txtModel = MkText(tConn, 150, 94, 420);
            MkLabel(tConn, "System Prompt", 10, 124); txtPrompt = MkText(tConn, 150, 122, 420, true);
            MkLabel(tConn, "Temperature", 10, 222); txtTemp = MkText(tConn, 150, 220, 100);
            MkLabel(tConn, "Max Tokens", 270, 222); txtMaxTok = MkText(tConn, 360, 220, 100);
            MkLabel(tConn, "Timeout (detik)", 10, 250); txtTimeout = MkText(tConn, 150, 248, 100);
            MkBtn(tConn, "Tes Koneksi", 150, 285, OnTest);
            MkBtn(tConn, "Simpan", 320, 285, delegate { SaveConnCache(); MessageBox.Show("Disimpan.", "AI Setup"); });
            lblTest = new Label();
            lblTest.Left = 150; lblTest.Top = 320; lblTest.Width = 450; lblTest.Height = 60;
            tConn.Controls.Add(lblTest);

            TabPage tCache = NewTab(tc, "Cache & FAQ");
            chkCache = new CheckBox(); chkCache.Text = "CacheEnabled";
            chkCache.Left = 150; chkCache.Top = 10; chkCache.Width = 200;
            tCache.Controls.Add(chkCache);
            chkFuzzy = new CheckBox(); chkFuzzy.Text = "CacheFuzzy (mirip)";
            chkFuzzy.Left = 150; chkFuzzy.Top = 36; chkFuzzy.Width = 200;
            tCache.Controls.Add(chkFuzzy);
            MkLabel(tCache, "CacheTtlDays", 10, 64); txtTtl = MkText(tCache, 150, 62, 100);
            MkLabel(tCache, "MaxCacheEntries", 270, 64); txtMaxEntries = MkText(tCache, 390, 62, 100);
            MkBtn(tCache, "Simpan", 150, 95, delegate { SaveConnCache(); MessageBox.Show("Disimpan.", "AI Setup"); });
            MkBtn(tCache, "Muat FAQ", 320, 95, delegate { LoadFaq(); });
            MkBtn(tCache, "Bersihkan Cache", 490, 95, delegate { AiResponder.ClearCache(); LoadFaq(); MessageBox.Show("Cache dikosongkan.", "AI Setup"); });
            lstFaq = new ListBox();
            lstFaq.Left = 10; lstFaq.Top = 130; lstFaq.Width = 650; lstFaq.Height = 300;
            tCache.Controls.Add(lstFaq);

            TabPage tOvr = NewTab(tc, "Override");
            gridOverride = NewGrid(tOvr, new string[] { "question", "answer", "enabled" });
            MkBtn(tOvr, "Tambah", 10, 335, delegate { gridOverride.Rows.Add("", "", true); });
            MkBtn(tOvr, "Hapus", 175, 335, delegate { foreach (DataGridViewRow r in gridOverride.SelectedRows) { if (!r.IsNewRow) gridOverride.Rows.Remove(r); } });
            MkBtn(tOvr, "Simpan", 340, 335, delegate { SaveOverride(); MessageBox.Show("Disimpan.", "AI Setup"); });

            TabPage tSeg = NewTab(tc, "Segmen Waktu");
            gridSegmen = NewGrid(tSeg, new string[] { "Name", "HourFrom", "HourTo", "Keyword", "Mode", "Text" });
            MkBtn(tSeg, "Tambah", 10, 335, delegate { gridSegmen.Rows.Add("", "0", "24", "", "append", ""); });
            MkBtn(tSeg, "Hapus", 175, 335, delegate { foreach (DataGridViewRow r in gridSegmen.SelectedRows) { if (!r.IsNewRow) gridSegmen.Rows.Remove(r); } });
            MkBtn(tSeg, "Simpan", 340, 335, delegate { SaveSegmen(); MessageBox.Show("Disimpan.", "AI Setup"); });
            gridSegmen.AllowUserToDeleteRows = true;
        }

        private DataGridView NewGrid(TabPage p, string[] cols)
        {
            DataGridView g = new DataGridView();
            g.Left = 10; g.Top = 10; g.Width = 650; g.Height = 315;
            g.AllowUserToAddRows = true;
            g.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            foreach (string c in cols)
            {
                if (c == "enabled")
                {
                    DataGridViewCheckBoxColumn cb = new DataGridViewCheckBoxColumn();
                    cb.Name = c; cb.HeaderText = c; cb.Width = 60;
                    g.Columns.Add(cb);
                }
                else
                {
                    g.Columns.Add(c, c);
                }
            }
            p.Controls.Add(g);
            return g;
        }

        private void LoadAll()
        {
            try
            {
                AiSettings cfg = AiResponder.LoadSettings();
                if (cfg == null) cfg = new AiSettings();
                chkEnabled.Checked = cfg.Enabled;
                txtBaseUrl.Text = cfg.BaseUrl ?? "";
                txtApiKey.Text = cfg.ApiKey ?? "";
                txtModel.Text = cfg.Model ?? "";
                txtPrompt.Text = cfg.SystemPrompt ?? "";
                txtTemp.Text = cfg.Temperature.ToString();
                txtMaxTok.Text = cfg.MaxTokens.ToString();
                txtTimeout.Text = cfg.TimeoutSeconds.ToString();
                chkCache.Checked = cfg.CacheEnabled;
                chkFuzzy.Checked = cfg.CacheFuzzy;
                txtTtl.Text = cfg.CacheTtlDays.ToString();
                txtMaxEntries.Text = cfg.MaxCacheEntries.ToString();
                if (cfg.Segments != null)
                {
                    foreach (var s in cfg.Segments)
                    {
                        if (s == null) continue;
                        gridSegmen.Rows.Add(s.Name ?? "", s.HourFrom.ToString(), s.HourTo.ToString(), s.Keyword ?? "", s.Mode ?? "append", s.Text ?? "");
                    }
                }
            }
            catch (Exception ex) { lblTest.Text = "Gagal muat: " + ex.Message; }
            try { LoadOverrideGrid(); } catch (Exception) { }
            try { LoadFaq(); } catch (Exception) { }
        }

        private void SaveConnCache()
        {
            try
            {
                AiSettings cfg = AiResponder.LoadSettings() ?? new AiSettings();
                cfg.Enabled = chkEnabled.Checked;
                cfg.BaseUrl = txtBaseUrl.Text.Trim();
                cfg.ApiKey = txtApiKey.Text.Trim();
                cfg.Model = txtModel.Text.Trim();
                cfg.SystemPrompt = txtPrompt.Text;
                double tp;
                if (double.TryParse(txtTemp.Text, out tp)) cfg.Temperature = tp;
                int iv;
                if (int.TryParse(txtMaxTok.Text, out iv)) cfg.MaxTokens = iv;
                if (int.TryParse(txtTimeout.Text, out iv)) cfg.TimeoutSeconds = iv;
                cfg.CacheEnabled = chkCache.Checked;
                cfg.CacheFuzzy = chkFuzzy.Checked;
                if (int.TryParse(txtTtl.Text, out iv)) cfg.CacheTtlDays = iv;
                if (int.TryParse(txtMaxEntries.Text, out iv)) cfg.MaxCacheEntries = iv;
                File.WriteAllText(AiResponder.GetSettingsPath(), JsonConvert.SerializeObject(cfg, Formatting.Indented));
            }
            catch (Exception ex) { MessageBox.Show("Gagal simpan: " + ex.Message, "AI Setup"); }
        }

        private async void OnTest(object sender, EventArgs e)
        {
            lblTest.Text = "Mengetes...";
            try
            {
                string baseUrl = txtBaseUrl.Text.Trim().TrimEnd('/');
                string key = txtApiKey.Text.Trim();
                if (string.IsNullOrEmpty(key)) { lblTest.Text = "API key kosong."; return; }
                System.Net.HttpWebRequest req = (System.Net.HttpWebRequest)System.Net.WebRequest.Create(baseUrl + "/models");
                req.Method = "GET";
                req.Headers["Authorization"] = "Bearer " + key;
                req.Timeout = 15000;
                string body;
                using (System.Net.WebResponse resp = await req.GetResponseAsync())
                using (System.IO.StreamReader sr = new System.IO.StreamReader(resp.GetResponseStream()))
                {
                    body = await sr.ReadToEndAsync();
                }
                int n = 0;
                try
                {
                    dynamic o = JsonConvert.DeserializeObject(body);
                    if (o != null && o.data != null) n = (int)o.data.Count;
                }
                catch (Exception) { }
                lblTest.Text = "OK: koneksi berhasil" + (n > 0 ? " (" + n + " model tersedia)." : ".");
            }
            catch (Exception ex)
            {
                string msg = ex.Message;
                try { if (ex is System.Net.WebException && ((System.Net.WebException)ex).Response != null) { using (var r = ((System.Net.WebException)ex).Response.GetResponseStream()) using (var sr = new System.IO.StreamReader(r)) { msg = sr.ReadToEnd(); } } } catch (Exception) { }
                if (msg.Length > 300) msg = msg.Substring(0, 300);
                lblTest.Text = "Gagal: " + msg;
            }
        }

        private class OverrideRow
        {
            public string question { get; set; }
            public string answer { get; set; }
            public bool enabled { get; set; }
        }

        private void LoadOverrideGrid()
        {
            gridOverride.Rows.Clear();
            try
            {
                string path = AiResponder.GetOverridesPath();
                if (!File.Exists(path)) return;
                var list = JsonConvert.DeserializeObject<List<OverrideRow>>(File.ReadAllText(path));
                if (list == null) return;
                foreach (var o in list)
                {
                    if (o == null) continue;
                    gridOverride.Rows.Add(o.question ?? "", o.answer ?? "", o.enabled);
                }
            }
            catch (Exception) { }
        }

        private void SaveOverride()
        {
            try
            {
                var list = new List<OverrideRow>();
                foreach (DataGridViewRow r in gridOverride.Rows)
                {
                    if (r.IsNewRow) continue;
                    string q = r.Cells[0].Value != null ? r.Cells[0].Value.ToString() : "";
                    string a = r.Cells[1].Value != null ? r.Cells[1].Value.ToString() : "";
                    if (string.IsNullOrWhiteSpace(q) || string.IsNullOrWhiteSpace(a)) continue;
                    bool en = true;
                    try { en = r.Cells[2].Value == null ? true : Convert.ToBoolean(r.Cells[2].Value); } catch (Exception) { }
                    list.Add(new OverrideRow { question = q, answer = a, enabled = en });
                }
                File.WriteAllText(AiResponder.GetOverridesPath(), JsonConvert.SerializeObject(list, Formatting.Indented));
            }
            catch (Exception ex) { MessageBox.Show("Gagal simpan: " + ex.Message, "AI Setup"); }
        }

        private void SaveSegmen()
        {
            try
            {
                AiSettings cfg = AiResponder.LoadSettings() ?? new AiSettings();
                var segs = new List<AiSegment>();
                foreach (DataGridViewRow r in gridSegmen.Rows)
                {
                    if (r.IsNewRow) continue;
                    string nm = r.Cells[0].Value != null ? r.Cells[0].Value.ToString() : "";
                    string tx = r.Cells[5].Value != null ? r.Cells[5].Value.ToString() : "";
                    if (string.IsNullOrWhiteSpace(tx)) continue;
                    int hf = 0, ht = 24;
                    try { hf = Convert.ToInt32(r.Cells[1].Value); } catch (Exception) { }
                    try { ht = Convert.ToInt32(r.Cells[2].Value); } catch (Exception) { }
                    segs.Add(new AiSegment
                    {
                        Name = nm,
                        HourFrom = hf,
                        HourTo = ht,
                        Keyword = r.Cells[3].Value != null ? r.Cells[3].Value.ToString() : "",
                        Mode = r.Cells[4].Value != null ? r.Cells[4].Value.ToString() : "append",
                        Text = tx
                    });
                }
                cfg.Segments = segs;
                File.WriteAllText(AiResponder.GetSettingsPath(), JsonConvert.SerializeObject(cfg, Formatting.Indented));
            }
            catch (Exception ex) { MessageBox.Show("Gagal simpan: " + ex.Message, "AI Setup"); }
        }

        private void LoadFaq()
        {
            lstFaq.Items.Clear();
            try
            {
                string db = AiResponder.GetCachePath();
                if (!File.Exists(db))
                {
                    string f = AiResponder.GetFaqPath();
                    if (File.Exists(f))
                    {
                        foreach (string ln in File.ReadAllLines(f)) lstFaq.Items.Add(ln);
                    }
                    else lstFaq.Items.Add("(belum ada data FAQ)");
                    return;
                }
                using (var c = new SQLiteConnection("Data Source=" + db + ";Version=3;"))
                {
                    c.Open();
                    using (var cmd = c.CreateCommand())
                    {
                        cmd.CommandText = "SELECT question,hits FROM ai_cache ORDER BY hits DESC LIMIT 100;";
                        using (var r = cmd.ExecuteReader())
                        {
                            while (r.Read()) lstFaq.Items.Add("[" + r.GetInt64(1) + "x] " + (r.IsDBNull(0) ? "" : r.GetString(0)));
                        }
                    }
                }
                if (lstFaq.Items.Count == 0) lstFaq.Items.Add("(belum ada data FAQ)");
            }
            catch (Exception ex) { lstFaq.Items.Add("Gagal muat: " + ex.Message); }
        }
    }
}

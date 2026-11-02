using MaterialSkin.Controls;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using WaAutoReplyBot;

namespace WASender
{
    /// <summary>
    /// Auto Responder Bot Telegram (Bot API, polling getUpdates — tanpa server).
    /// Aturan: pola cocok (contains, abaikan huruf) -> balas (dukung spintax {a|b}).
    /// Token + aturan tersimpan lokal di telegram_bot.json. Tanpa ubah logika WA.
    /// </summary>
    public partial class TelegramBotForm : MyMaterialPopOp
    {
        private WaSenderForm waSenderForm;
        private TextBox txtToken;
        private CheckBox chkShow;
        private Button btnTest;
        private Button btnStart;
        private Button btnStop;
        private Label lblStatus;
        private DataGridView gridRules;
        private TextBox txtPattern;
        private TextBox txtResponse;
        private Button btnAddRule;
        private Button btnDelRule;
        private Button btnSave;
        private DataGridView gridLog;
        private CancellationTokenSource cts;
        private Task pollTask;
        private long updateOffset = 0;
        private int sentCount = 0;

        private class TgRule
        {
            public string pattern = "";
            public string response = "";
        }
        private List<TgRule> rules = new List<TgRule>();

        public TelegramBotForm(WaSenderForm _waSenderForm)
        {
            waSenderForm = _waSenderForm;
            InitializeComponentManual();
            this.Icon = Strings.AppIcon;
            this.Text = "Telegram Bot Auto Responder";
            try { LoadState(); } catch (Exception) { }
            try { RefreshGrids(); } catch (Exception) { }
            try { SetStoppedUi("Berhenti."); } catch (Exception) { }
        }

        private string StatePath()
        {
            try { return Path.Combine(Config.GetAppDataFolder(), "telegram_bot.json"); }
            catch (Exception) { return "telegram_bot.json"; }
        }

        private void LoadState()
        {
            try
            {
                string p = StatePath();
                if (!File.Exists(p)) return;
                JObject o = JObject.Parse(File.ReadAllText(p));
                txtToken.Text = (o["token"] ?? "").ToString();
                rules.Clear();
                JArray arr = o["rules"] as JArray;
                if (arr != null)
                {
                    foreach (JToken r in arr)
                    {
                        try
                        {
                            rules.Add(new TgRule
                            {
                                pattern = ((r["pattern"] ?? "").ToString() ?? "").Trim(),
                                response = (r["response"] ?? "").ToString() ?? ""
                            });
                        }
                        catch (Exception) { }
                    }
                }
                rules.RemoveAll(r => string.IsNullOrEmpty(r.pattern));
            }
            catch (Exception) { }
        }

        private void SaveState()
        {
            try
            {
                JArray arr = new JArray();
                foreach (TgRule r in rules) arr.Add(new JObject(new JProperty("pattern", r.pattern ?? ""), new JProperty("response", r.response ?? "")));
                File.WriteAllText(StatePath(), new JObject(new JProperty("token", txtToken.Text.Trim()), new JProperty("rules", arr)).ToString());
            }
            catch (Exception ex) { try { MessageBox.Show("Gagal simpan: " + ex.Message, Strings.Error, MessageBoxButtons.OK, MessageBoxIcon.Warning); } catch (Exception) { } }
        }

        private void RefreshGrids()
        {
            try
            {
                gridRules.Rows.Clear();
                foreach (TgRule r in rules) gridRules.Rows.Add(r.pattern, r.response);
            }
            catch (Exception) { }
        }

        private void SetStoppedUi(string msg)
        {
            try
            {
                lblStatus.Text = msg;
                btnStart.Enabled = true;
                btnStop.Enabled = false;
            }
            catch (Exception) { }
        }

        private void SetRunningUi()
        {
            try
            {
                lblStatus.Text = "Berjalan — menunggu pesan masuk... (terkirim: " + sentCount + ")";
                btnStart.Enabled = false;
                btnStop.Enabled = true;
            }
            catch (Exception) { }
        }

        private void LogUi(string from, string text, string rule, bool ok)
        {
            try
            {
                if (gridLog.InvokeRequired) { try { gridLog.Invoke(new Action(() => LogUi(from, text, rule, ok))); } catch (Exception) { } return; }
                if (gridLog.Rows.Count >= 500) { try { gridLog.Rows.RemoveAt(0); } catch (Exception) { } }
                string t = text ?? "";
                if (t.Length > 80) t = t.Substring(0, 80) + "...";
                gridLog.Rows.Add(DateTime.Now.ToString("HH:mm:ss"), from ?? "-", t, rule ?? "-", ok ? "OK" : "GAGAL");
                try { gridLog.FirstDisplayedScrollingRowIndex = gridLog.RowCount - 1; } catch (Exception) { }
                try { lblStatus.Text = "Berjalan — menunggu pesan masuk... (terkirim: " + sentCount + ")"; } catch (Exception) { }
            }
            catch (Exception) { }
        }

        private string ApiGet(string token, string method, string query)
        {
            try
            {
                ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
                HttpWebRequest req = (HttpWebRequest)WebRequest.Create("https://api.telegram.org/bot" + token + "/" + method + (string.IsNullOrEmpty(query) ? "" : "?" + query));
                req.Method = "GET";
                req.Timeout = 45000;
                using (WebResponse resp = req.GetResponse())
                using (StreamReader sr = new StreamReader(resp.GetResponseStream(), Encoding.UTF8))
                    return sr.ReadToEnd();
            }
            catch (WebException wx)
            {
                try
                {
                    using (WebResponse r = wx.Response)
                    using (StreamReader sr = new StreamReader(r.GetResponseStream(), Encoding.UTF8))
                        return "HTTPERR:" + sr.ReadToEnd();
                }
                catch (Exception) { }
                throw;
            }
        }

        private string ApiPost(string token, string method, object payload)
        {
            try
            {
                ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
                string body = JsonConvert.SerializeObject(payload);
                byte[] bytes = Encoding.UTF8.GetBytes(body);
                HttpWebRequest req = (HttpWebRequest)WebRequest.Create("https://api.telegram.org/bot" + token + "/" + method);
                req.Method = "POST";
                req.ContentType = "application/json";
                req.Timeout = 30000;
                using (Stream s = req.GetRequestStream()) s.Write(bytes, 0, bytes.Length);
                using (WebResponse resp = req.GetResponse())
                using (StreamReader sr = new StreamReader(resp.GetResponseStream(), Encoding.UTF8))
                    return sr.ReadToEnd();
            }
            catch (WebException wx)
            {
                try
                {
                    using (WebResponse r = wx.Response)
                    using (StreamReader sr = new StreamReader(r.GetResponseStream(), Encoding.UTF8))
                        return "HTTPERR:" + sr.ReadToEnd();
                }
                catch (Exception) { }
                throw;
            }
        }

        private async void btnTest_Click(object sender, EventArgs e)
        {
            string token = txtToken.Text.Trim();
            if (token.Length < 20) { try { MessageBox.Show("Isi Token Bot dulu (dari @BotFather).", "Telegram", MessageBoxButtons.OK, MessageBoxIcon.Warning); } catch (Exception) { } return; }
            btnTest.Enabled = false;
            try
            {
                string res = await Task.Run(() => ApiGet(token, "getMe", ""));
                JObject o = JObject.Parse(res);
                if (o["ok"] != null && o["ok"].ToString().ToLower() == "true")
                {
                    string uname = "";
                    try { uname = "@" + (o["result"]["username"] ?? "").ToString(); } catch (Exception) { }
                    try { MessageBox.Show("Terhubung ke " + uname + ".\n\nPastikan Anda sudah /start ke bot ini dari akun Telegram Anda.", "Telegram", MessageBoxButtons.OK, MessageBoxIcon.Information); } catch (Exception) { }
                }
                else
                {
                    try { MessageBox.Show("Token ditolak Telegram:\n" + res, "Telegram", MessageBoxButtons.OK, MessageBoxIcon.Error); } catch (Exception) { }
                }
            }
            catch (Exception ex) { try { MessageBox.Show("Gagal koneksi: " + ex.Message, "Telegram", MessageBoxButtons.OK, MessageBoxIcon.Error); } catch (Exception) { } }
            btnTest.Enabled = true;
        }

        private async void btnStart_Click(object sender, EventArgs e)
        {
            string token = txtToken.Text.Trim();
            if (token.Length < 20) { try { MessageBox.Show("Isi Token Bot dulu (dari @BotFather).", "Telegram", MessageBoxButtons.OK, MessageBoxIcon.Warning); } catch (Exception) { } return; }
            if (rules.Count == 0) { try { MessageBox.Show("Tambahkan minimal 1 aturan (kata kunci -> balasan).", "Telegram", MessageBoxButtons.OK, MessageBoxIcon.Warning); } catch (Exception) { } return; }
            try { SaveState(); } catch (Exception) { }
            btnStart.Enabled = false;
            lblStatus.Text = "Menghubungkan...";
            try
            {
                string probe = await Task.Run(() => ApiGet(token, "getMe", ""));
                JObject po = JObject.Parse(probe);
                if (po["ok"] == null || po["ok"].ToString().ToLower() != "true")
                {
                    try { MessageBox.Show("Token ditolak Telegram. Periksa kembali tokennya.", "Telegram", MessageBoxButtons.OK, MessageBoxIcon.Error); } catch (Exception) { }
                    SetStoppedUi("Token ditolak.");
                    return;
                }
            }
            catch (Exception ex) { try { MessageBox.Show("Gagal koneksi: " + ex.Message, "Telegram", MessageBoxButtons.OK, MessageBoxIcon.Error); } catch (Exception) { } SetStoppedUi("Gagal koneksi."); return; }
            try
            {
                string drain = await Task.Run(() => ApiGet(token, "getUpdates", "timeout=0"));
                updateOffset = MaxUpdateId(drain) + 1;
            }
            catch (Exception) { updateOffset = 0; }
            sentCount = 0;
            cts = new CancellationTokenSource();
            SetRunningUi();
            CancellationToken ct = cts.Token;
            pollTask = Task.Run(() => PollLoop(token, ct));
        }

        private long MaxUpdateId(string json)
        {
            try
            {
                JObject o = JObject.Parse(json);
                JArray arr = o["result"] as JArray;
                long m = 0;
                if (arr != null) foreach (JToken u in arr) { try { long id = Convert.ToInt64(u["update_id"] ?? 0); if (id > m) m = id; } catch (Exception) { } }
                return m;
            }
            catch (Exception) { return 0; }
        }

        private void PollLoop(string token, CancellationToken ct)
        {
            while (!ct.IsCancellationRequested)
            {
                string res = "";
                try { res = ApiGet(token, "getUpdates", "timeout=30&offset=" + updateOffset); }
                catch (Exception ex)
                {
                    try
                    {
                        if (lblStatus.InvokeRequired) lblStatus.Invoke(new Action(() => { try { lblStatus.Text = "Gangguan koneksi, mencoba lagi... (" + ex.Message + ")"; } catch (Exception) { } }));
                        else lblStatus.Text = "Gangguan koneksi, mencoba lagi...";
                    }
                    catch (Exception) { }
                    try { Task.Delay(3000, ct).Wait(ct); } catch (Exception) { break; }
                    continue;
                }
                try
                {
                    JObject o = JObject.Parse(res);
                    JArray arr = o["result"] as JArray;
                    if (arr != null)
                    {
                        foreach (JToken u in arr)
                        {
                            if (ct.IsCancellationRequested) break;
                            try
                            {
                                long uid = Convert.ToInt64(u["update_id"] ?? 0);
                                if (uid >= updateOffset) updateOffset = uid + 1;
                                JToken msg = u["message"] ?? u["edited_message"];
                                if (msg == null) continue;
                                string text = (msg["text"] ?? "").ToString();
                                if (string.IsNullOrWhiteSpace(text)) continue;
                                JToken from = msg["from"];
                                bool isBot = false;
                                try { isBot = Convert.ToBoolean(from["is_bot"] ?? false); } catch (Exception) { }
                                if (isBot) continue;
                                string chatId = "";
                                try { chatId = (msg["chat"]["id"] ?? "").ToString(); } catch (Exception) { continue; }
                                string who = "";
                                try { who = ((from["first_name"] ?? "").ToString() + " " + (from["username"] ?? "").ToString()).Trim(); } catch (Exception) { }
                                TgRule hit = null;
                                foreach (TgRule r in new List<TgRule>(rules))
                                {
                                    try { if (!string.IsNullOrEmpty(r.pattern) && text.IndexOf(r.pattern, StringComparison.OrdinalIgnoreCase) >= 0) { hit = r; break; } }
                                    catch (Exception) { }
                                }
                                if (hit == null) continue;
                                string reply = hit.response ?? "";
                                try { reply = ProjectCommon.SpinText(reply); } catch (Exception) { }
                                bool ok = SendReply(token, chatId, reply);
                                if (ok) sentCount++;
                                LogUi(who, text, hit.pattern, ok);
                            }
                            catch (Exception) { }
                        }
                    }
                }
                catch (Exception) { }
            }
        }

        private bool SendReply(string token, string chatId, string text)
        {
            try
            {
                string res = ApiPost(token, "sendMessage", new { chat_id = chatId, text = text ?? "" });
                try { JObject o = JObject.Parse(res); return o["ok"] != null && o["ok"].ToString().ToLower() == "true"; }
                catch (Exception) { return false; }
            }
            catch (Exception) { return false; }
        }

        private void btnStop_Click(object sender, EventArgs e)
        {
            try { if (cts != null) cts.Cancel(); } catch (Exception) { }
            SetStoppedUi("Berhenti. Terkirim sesi ini: " + sentCount);
        }

        private void btnAddRule_Click(object sender, EventArgs e)
        {
            try
            {
                string p = (txtPattern.Text ?? "").Trim();
                string r = txtResponse.Text ?? "";
                if (p == "" || r.Trim() == "") { try { MessageBox.Show("Kata kunci dan balasan wajib diisi.", "Telegram", MessageBoxButtons.OK, MessageBoxIcon.Warning); } catch (Exception) { } return; }
                rules.Add(new TgRule { pattern = p, response = r });
                txtPattern.Text = "";
                txtResponse.Text = "";
                RefreshGrids();
            }
            catch (Exception) { }
        }

        private void btnDelRule_Click(object sender, EventArgs e)
        {
            try
            {
                if (gridRules.SelectedRows.Count == 0) return;
                int i = gridRules.SelectedRows[0].Index;
                if (i >= 0 && i < rules.Count) rules.RemoveAt(i);
                RefreshGrids();
            }
            catch (Exception) { }
        }

        private void btnSave_Click(object sender, EventArgs e)
        {
            try { SaveState(); } catch (Exception) { }
            try { MessageBox.Show("Tersimpan.", "Telegram", MessageBoxButtons.OK, MessageBoxIcon.Information); } catch (Exception) { }
        }

        private void TelegramBotForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            try { if (cts != null) cts.Cancel(); } catch (Exception) { }
            try { SaveState(); } catch (Exception) { }
            try { if (waSenderForm != null) waSenderForm.formReturn(true); } catch (Exception) { }
        }

        private void InitializeComponentManual()
        {
            this.SuspendLayout();
            this.ClientSize = new Size(860, 620);
            this.StartPosition = FormStartPosition.CenterParent;
            this.MaximizeBox = false;

            var top = new Panel();
            top.Dock = DockStyle.Top;
            top.Height = 44;
            top.Padding = new Padding(12, 8, 12, 4);
            this.Controls.Add(top);

            var lblT = new Label();
            lblT.Text = "Token Bot:";
            lblT.AutoSize = true;
            lblT.Left = 12; lblT.Top = 14;
            top.Controls.Add(lblT);

            txtToken = new TextBox();
            txtToken.Left = 90; txtToken.Top = 10;
            txtToken.Width = 380;
            txtToken.UseSystemPasswordChar = true;
            try { txtToken.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right; } catch (Exception) { }
            top.Controls.Add(txtToken);

            chkShow = new CheckBox();
            chkShow.Text = "Lihat";
            chkShow.AutoSize = true;
            chkShow.Left = 476; chkShow.Top = 12;
            chkShow.CheckedChanged += (s, e) => { try { txtToken.UseSystemPasswordChar = !chkShow.Checked; } catch (Exception) { } };
            top.Controls.Add(chkShow);

            btnTest = new Button();
            btnTest.Text = "Tes Koneksi";
            btnTest.Left = 540; btnTest.Top = 8; btnTest.Width = 100; btnTest.Height = 28;
            btnTest.Click += btnTest_Click;
            top.Controls.Add(btnTest);

            btnStart = new Button();
            btnStart.Text = "Start";
            btnStart.Left = 646; btnStart.Top = 8; btnStart.Width = 80; btnStart.Height = 28;
            btnStart.Click += btnStart_Click;
            top.Controls.Add(btnStart);

            btnStop = new Button();
            btnStop.Text = "Stop";
            btnStop.Left = 732; btnStop.Top = 8; btnStop.Width = 80; btnStop.Height = 28;
            btnStop.Click += btnStop_Click;
            top.Controls.Add(btnStop);

            lblStatus = new Label();
            lblStatus.Dock = DockStyle.Top;
            lblStatus.Height = 24;
            lblStatus.Padding = new Padding(12, 0, 0, 0);
            lblStatus.TextAlign = ContentAlignment.MiddleLeft;
            this.Controls.Add(lblStatus);

            var mid = new SplitContainer();
            mid.Dock = DockStyle.Fill;
            mid.Orientation = Orientation.Horizontal;
            mid.SplitterDistance = 300;
            this.Controls.Add(mid);

            var pnlRules = new Panel();
            pnlRules.Dock = DockStyle.Fill;
            pnlRules.Padding = new Padding(12, 4, 12, 4);
            mid.Panel1.Controls.Add(pnlRules);

            var capR = new Label();
            capR.Text = "Aturan (kata kunci -> balasan, dukung spintax {a|b}):";
            capR.Dock = DockStyle.Top;
            capR.Height = 20;
            pnlRules.Controls.Add(capR);

            var pnlAdd = new Panel();
            pnlAdd.Dock = DockStyle.Top;
            pnlAdd.Height = 60;
            pnlRules.Controls.Add(pnlAdd);

            txtPattern = new TextBox();
            txtPattern.Left = 0; txtPattern.Top = 4; txtPattern.Width = 200;
            pnlAdd.Controls.Add(txtPattern);

            txtResponse = new TextBox();
            txtResponse.Left = 206; txtResponse.Top = 4; txtResponse.Width = 380;
            txtResponse.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            pnlAdd.Controls.Add(txtResponse);

            btnAddRule = new Button();
            btnAddRule.Text = "Tambah";
            btnAddRule.Left = 592; btnAddRule.Top = 2; btnAddRule.Width = 80; btnAddRule.Height = 26;
            btnAddRule.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            btnAddRule.Click += btnAddRule_Click;
            pnlAdd.Controls.Add(btnAddRule);

            btnDelRule = new Button();
            btnDelRule.Text = "Hapus";
            btnDelRule.Left = 592; btnDelRule.Top = 30; btnDelRule.Width = 80; btnDelRule.Height = 26;
            btnDelRule.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            btnDelRule.Click += btnDelRule_Click;
            pnlAdd.Controls.Add(btnDelRule);

            btnSave = new Button();
            btnSave.Text = "Simpan";
            btnSave.Left = 678; btnSave.Top = 2; btnSave.Width = 80; btnSave.Height = 26;
            btnSave.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            btnSave.Click += btnSave_Click;
            pnlAdd.Controls.Add(btnSave);

            var lblHint = new Label();
            lblHint.Text = "Token didapat dari @BotFather. Bot hanya bisa membalas user/grup yang sudah /start ke bot.";
            lblHint.Left = 0; lblHint.Top = 32; lblHint.Width = 580;
            lblHint.ForeColor = Color.Gray;
            lblHint.AutoSize = false;
            pnlAdd.Controls.Add(lblHint);

            gridRules = new DataGridView();
            gridRules.Dock = DockStyle.Fill;
            gridRules.AllowUserToAddRows = false;
            gridRules.ReadOnly = true;
            gridRules.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            gridRules.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            gridRules.Columns.Add("c0", "Kata kunci");
            gridRules.Columns.Add("c1", "Balasan");
            pnlRules.Controls.Add(gridRules);
            pnlRules.Controls.SetChildIndex(gridRules, 0);
            try { GridTheme.Apply(gridRules); } catch (Exception) { }

            var pnlLog = new Panel();
            pnlLog.Dock = DockStyle.Fill;
            pnlLog.Padding = new Padding(12, 4, 12, 8);
            mid.Panel2.Controls.Add(pnlLog);

            var capL = new Label();
            capL.Text = "Log balasan:";
            capL.Dock = DockStyle.Top;
            capL.Height = 20;
            pnlLog.Controls.Add(capL);

            gridLog = new DataGridView();
            gridLog.Dock = DockStyle.Fill;
            gridLog.AllowUserToAddRows = false;
            gridLog.ReadOnly = true;
            gridLog.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            gridLog.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            gridLog.Columns.Add("l0", "Jam");
            gridLog.Columns.Add("l1", "Dari");
            gridLog.Columns.Add("l2", "Pesan");
            gridLog.Columns.Add("l3", "Aturan");
            gridLog.Columns.Add("l4", "Status");
            pnlLog.Controls.Add(gridLog);
            pnlLog.Controls.SetChildIndex(gridLog, 0);
            try { GridTheme.Apply(gridLog); } catch (Exception) { }

            this.FormClosing += TelegramBotForm_FormClosing;
            this.ResumeLayout(false);
        }
    }
}

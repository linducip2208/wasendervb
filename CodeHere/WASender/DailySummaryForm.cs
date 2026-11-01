using MaterialSkin.Controls;
using System;
using System.Text;
using System.Windows.Forms;
using WaAutoReplyBot;

namespace WASender
{
    /// <summary>
    /// Ringkasan Harian di form sendiri (dashboard utama tetap bersih).
    /// Isi: stat kirim/kuota/skor/antrian, akun hari ini, stat AI,
    /// status pembaruan, log terakhir. Refresh manual + otomatis 60 dtk.
    /// </summary>
    public partial class DailySummaryForm : MyMaterialPopOp
    {
        private Timer refreshTimer;
        private MaterialLabel lblSent;
        private MaterialLabel lblQuota;
        private MaterialLabel lblHealth;
        private MaterialLabel lblQueue;
        private DataGridView gridAccounts;
        private MaterialLabel lblAi;
        private MaterialLabel lblUpdate;
        private TextBox txtLog;
        private TextBox txtHistory;

        public DailySummaryForm()
        {
            InitializeComponentManual();
            this.Icon = Strings.AppIcon;
            this.Text = "Ringkasan Hari Ini";
            try { GridTheme.Apply(gridAccounts); } catch (Exception) { }
            RefreshStats();
            try
            {
                refreshTimer = new Timer();
                refreshTimer.Interval = 60000;
                refreshTimer.Tick += (s, e) => { try { RefreshStats(); } catch (Exception) { } };
                refreshTimer.Start();
            }
            catch (Exception) { }
        }

        private void InitializeComponentManual()
        {
            this.SuspendLayout();
            this.ClientSize = new System.Drawing.Size(760, 600);
            this.StartPosition = FormStartPosition.CenterParent;
            this.MaximizeBox = false;
            this.Padding = new Padding(12);

            var top = new FlowLayoutPanel();
            top.Dock = DockStyle.Top;
            top.Height = 110;
            top.Padding = new Padding(0, 0, 0, 8);
            this.Controls.Add(top);

            lblSent = MakeStat(top, "Terkirim Hari Ini");
            lblQuota = MakeStat(top, "Sisa Kuota / Limit");
            lblHealth = MakeStat(top, "Skor Kesehatan WA");
            lblQueue = MakeStat(top, "Antrian Kirim");

            var mid = new TableLayoutPanel();
            mid.Dock = DockStyle.Top;
            mid.Height = 240;
            mid.ColumnCount = 2;
            mid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            mid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            this.Controls.Add(mid);

            var cardAcc = new MaterialCard();
            cardAcc.Dock = DockStyle.Fill;
            cardAcc.Padding = new Padding(12);
            var lblAccTitle = new MaterialLabel();
            lblAccTitle.Text = "Akun Hari Ini";
            lblAccTitle.FontType = MaterialSkin.MaterialSkinManager.fontType.H6;
            lblAccTitle.Dock = DockStyle.Top;
            lblAccTitle.Height = 28;
            cardAcc.Controls.Add(lblAccTitle);
            gridAccounts = new DataGridView();
            gridAccounts.Dock = DockStyle.Fill;
            gridAccounts.ReadOnly = true;
            gridAccounts.AllowUserToAddRows = false;
            gridAccounts.RowHeadersVisible = false;
            gridAccounts.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            gridAccounts.BackgroundColor = System.Drawing.Color.White;
            gridAccounts.BorderStyle = BorderStyle.None;
            gridAccounts.Columns.Add("c0", "Akun");
            gridAccounts.Columns.Add("c1", "Terkirim");
            gridAccounts.Columns.Add("c2", "Limit");
            cardAcc.Controls.Add(gridAccounts);
            mid.Controls.Add(cardAcc, 0, 0);

            var right = new TableLayoutPanel();
            right.Dock = DockStyle.Fill;
            right.RowCount = 3;
            right.RowStyles.Add(new RowStyle(SizeType.Percent, 38F));
            right.RowStyles.Add(new RowStyle(SizeType.Percent, 22F));
            right.RowStyles.Add(new RowStyle(SizeType.Percent, 40F));
            mid.Controls.Add(right, 1, 0);

            var cardAi = new MaterialCard();
            cardAi.Dock = DockStyle.Fill;
            cardAi.Padding = new Padding(12);
            var lblAiTitle = new MaterialLabel();
            lblAiTitle.Text = "AI Hari Ini";
            lblAiTitle.FontType = MaterialSkin.MaterialSkinManager.fontType.H6;
            lblAiTitle.Dock = DockStyle.Top;
            lblAiTitle.Height = 28;
            cardAi.Controls.Add(lblAiTitle);
            lblAi = new MaterialLabel();
            lblAi.Dock = DockStyle.Fill;
            cardAi.Controls.Add(lblAi);
            right.Controls.Add(cardAi, 0, 0);

            var cardUp = new MaterialCard();
            cardUp.Dock = DockStyle.Fill;
            cardUp.Padding = new Padding(12);
            var lblUpTitle = new MaterialLabel();
            lblUpTitle.Text = "Pembaruan";
            lblUpTitle.FontType = MaterialSkin.MaterialSkinManager.fontType.H6;
            lblUpTitle.Dock = DockStyle.Top;
            lblUpTitle.Height = 28;
            cardUp.Controls.Add(lblUpTitle);
            lblUpdate = new MaterialLabel();
            lblUpdate.Dock = DockStyle.Fill;
            cardUp.Controls.Add(lblUpdate);
            right.Controls.Add(cardUp, 0, 1);

            var cardHist = new MaterialCard();
            cardHist.Dock = DockStyle.Fill;
            cardHist.Padding = new Padding(12);
            var lblHistTitle = new MaterialLabel();
            lblHistTitle.Text = "7 Hari Terakhir";
            lblHistTitle.FontType = MaterialSkin.MaterialSkinManager.fontType.H6;
            lblHistTitle.Dock = DockStyle.Top;
            lblHistTitle.Height = 28;
            cardHist.Controls.Add(lblHistTitle);
            txtHistory = new TextBox();
            txtHistory.Dock = DockStyle.Fill;
            txtHistory.Multiline = true;
            txtHistory.ReadOnly = true;
            txtHistory.ScrollBars = ScrollBars.Vertical;
            txtHistory.BorderStyle = BorderStyle.None;
            txtHistory.Font = new System.Drawing.Font("Consolas", 9F);
            cardHist.Controls.Add(txtHistory);
            right.Controls.Add(cardHist, 0, 2);

            var cardLog = new MaterialCard();
            cardLog.Dock = DockStyle.Fill;
            cardLog.Padding = new Padding(12);
            var lblLogTitle = new MaterialLabel();
            lblLogTitle.Text = "Log Terakhir";
            lblLogTitle.FontType = MaterialSkin.MaterialSkinManager.fontType.H6;
            lblLogTitle.Dock = DockStyle.Top;
            lblLogTitle.Height = 28;
            cardLog.Controls.Add(lblLogTitle);
            txtLog = new TextBox();
            txtLog.Dock = DockStyle.Fill;
            txtLog.Multiline = true;
            txtLog.ReadOnly = true;
            txtLog.ScrollBars = ScrollBars.Vertical;
            txtLog.BorderStyle = BorderStyle.None;
            txtLog.Font = new System.Drawing.Font("Consolas", 9F);
            cardLog.Controls.Add(txtLog);
            this.Controls.Add(cardLog);

            var btnRefresh = new MaterialButton();
            btnRefresh.Text = "Refresh";
            btnRefresh.Dock = DockStyle.Bottom;
            btnRefresh.Height = 36;
            btnRefresh.Type = MaterialButton.MaterialButtonType.Contained;
            btnRefresh.UseAccentColor = false;
            btnRefresh.Click += (s, e) => { try { RefreshStats(); } catch (Exception) { } };
            this.Controls.Add(btnRefresh);

            this.FormClosing += (s, e) => { try { if (refreshTimer != null) refreshTimer.Stop(); } catch (Exception) { } };
            this.ResumeLayout(false);
        }

        private MaterialLabel MakeStat(Control parent, string caption)
        {
            var box = new MaterialCard();
            box.Width = 172;
            box.Height = 96;
            box.Margin = new Padding(4);
            box.Padding = new Padding(8);
            var cap = new MaterialLabel();
            cap.Text = caption;
            cap.FontType = MaterialSkin.MaterialSkinManager.fontType.Subtitle2;
            cap.Dock = DockStyle.Top;
            cap.Height = 22;
            box.Controls.Add(cap);
            var v = new MaterialLabel();
            v.Text = "-";
            v.FontType = MaterialSkin.MaterialSkinManager.fontType.H4;
            v.Dock = DockStyle.Fill;
            box.Controls.Add(v);
            parent.Controls.Add(box);
            return v;
        }

        public void RefreshStats()
        {
            try
            {
                int total = 0, limitSum = 0;
                try
                {
                    gridAccounts.Rows.Clear();
                    foreach (string line in SendGovernor.TodayAll())
                    {
                        try
                        {
                            string[] p = (line ?? "").Split('|');
                            if (p.Length < 3) continue;
                            int n, lim;
                            if (!int.TryParse(p[1].Trim(), out n)) n = 0;
                            if (!int.TryParse(p[2].Trim(), out lim)) lim = 0;
                            total += n; limitSum += lim;
                            gridAccounts.Rows.Add(p[0].Trim(), n, lim);
                        }
                        catch (Exception) { }
                    }
                }
                catch (Exception) { }
                int left = limitSum - total;
                if (left < 0) left = 0;
                lblSent.Text = total.ToString();
                lblQuota.Text = left + " / " + limitSum;

                try
                {
                    int sc = SendGovernor.LastScore();
                    lblHealth.Text = sc < 0 ? "-" : sc.ToString();
                    lblHealth.ForeColor = sc < 0 ? System.Drawing.Color.Black : (sc < 40 ? System.Drawing.Color.Red : System.Drawing.Color.Green);
                }
                catch (Exception) { lblHealth.Text = "-"; }

                try { lblQueue.Text = SendQueue.Pending.ToString(); }
                catch (Exception) { lblQueue.Text = "-"; }

                try
                {
                    int[] ai = AiResponder.GetTodayStats();
                    lblAi.Text = "Pertanyaan: " + ai[0] + "\nCache hit: " + ai[1] + " | API call: " + ai[2] + "\nOverride: " + ai[3];
                }
                catch (Exception) { lblAi.Text = "-"; }

                try { lblUpdate.Text = AppUpdater.StatusText(); }
                catch (Exception) { lblUpdate.Text = "-"; }

                try
                {
                    var sb = new StringBuilder();
                    string mdb = "";
                    try { mdb = System.IO.Path.Combine(Config.GetAppDataFolder(), "wa_monitor.db"); } catch (Exception) { }
                    if (!string.IsNullOrEmpty(mdb) && System.IO.File.Exists(mdb))
                    {
                        using (var c = new System.Data.SQLite.SQLiteConnection("Data Source=" + mdb + ";Version=3;"))
                        {
                            c.Open();
                            using (var cmd = c.CreateCommand())
                            {
                                string since = System.DateTime.Now.AddDays(-6).ToString("yyyy-MM-dd");
                                cmd.CommandText = "SELECT date, SUM(n) FROM send_quota WHERE date >= @d GROUP BY date ORDER BY date DESC LIMIT 7;";
                                cmd.Parameters.AddWithValue("@d", since);
                                using (var r = cmd.ExecuteReader())
                                {
                                    while (r.Read())
                                    {
                                        string dt = r.IsDBNull(0) ? "?" : r.GetString(0);
                                        long n = r.IsDBNull(1) ? 0 : r.GetInt64(1);
                                        sb.AppendLine(dt + " | " + n);
                                    }
                                }
                            }
                        }
                    }
                    txtHistory.Text = sb.Length > 0 ? sb.ToString() : "(belum ada data)";
                }
                catch (Exception) { try { txtHistory.Text = "(gagal baca riwayat)"; } catch (Exception) { } }

                try
                {
                    var sb = new StringBuilder();
                    string dir = Config.GetProcessLoggerFolderPath();
                    string lp = System.IO.Path.Combine(dir ?? "", "single_send.log");
                    if (System.IO.File.Exists(lp))
                    {
                        string[] all = System.IO.File.ReadAllLines(lp);
                        int from = all.Length > 8 ? all.Length - 8 : 0;
                        for (int i = from; i < all.Length; i++) sb.AppendLine(all[i]);
                    }
                    txtLog.Text = sb.Length > 0 ? sb.ToString() : "(belum ada log)";
                }
                catch (Exception) { txtLog.Text = "(gagal baca log)"; }
            }
            catch (Exception) { }
        }
    }
}

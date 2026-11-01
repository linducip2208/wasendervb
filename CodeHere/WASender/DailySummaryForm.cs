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
        private Label lblSent;
        private Label lblQuota;
        private Label lblHealth;
        private Label lblQueue;
        private DataGridView gridAccounts;
        private Label lblAi;
        private Label lblUpdate;
        private TextBox txtLog;

        public DailySummaryForm()
        {
            InitializeComponentManual();
            this.Icon = Strings.AppIcon;
            this.Text = "Ringkasan Hari Ini";
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
            this.ClientSize = new System.Drawing.Size(720, 560);
            this.StartPosition = FormStartPosition.CenterParent;
            this.MaximizeBox = false;

            var top = new FlowLayoutPanel();
            top.Dock = DockStyle.Top;
            top.Height = 90;
            top.Padding = new Padding(10);
            this.Controls.Add(top);

            lblSent = MakeStat(top, "Terkirim Hari Ini");
            lblQuota = MakeStat(top, "Sisa Kuota / Limit");
            lblHealth = MakeStat(top, "Skor Kesehatan WA");
            lblQueue = MakeStat(top, "Antrian Kirim");

            var mid = new TableLayoutPanel();
            mid.Dock = DockStyle.Top;
            mid.Height = 220;
            mid.ColumnCount = 2;
            mid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            mid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            mid.Padding = new Padding(10, 0, 10, 0);
            this.Controls.Add(mid);

            var grpAcc = new GroupBox();
            grpAcc.Text = "Akun Hari Ini";
            grpAcc.Dock = DockStyle.Fill;
            gridAccounts = new DataGridView();
            gridAccounts.Dock = DockStyle.Fill;
            gridAccounts.ReadOnly = true;
            gridAccounts.AllowUserToAddRows = false;
            gridAccounts.RowHeadersVisible = false;
            gridAccounts.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            gridAccounts.Columns.Add("c0", "Akun");
            gridAccounts.Columns.Add("c1", "Terkirim");
            gridAccounts.Columns.Add("c2", "Limit");
            grpAcc.Controls.Add(gridAccounts);
            mid.Controls.Add(grpAcc, 0, 0);

            var right = new TableLayoutPanel();
            right.Dock = DockStyle.Fill;
            right.RowCount = 2;
            right.RowStyles.Add(new RowStyle(SizeType.Percent, 55F));
            right.RowStyles.Add(new RowStyle(SizeType.Percent, 45F));
            mid.Controls.Add(right, 1, 0);

            var grpAi = new GroupBox();
            grpAi.Text = "AI Hari Ini";
            grpAi.Dock = DockStyle.Fill;
            lblAi = new Label();
            lblAi.Dock = DockStyle.Fill;
            lblAi.Padding = new Padding(6);
            grpAi.Controls.Add(lblAi);
            right.Controls.Add(grpAi, 0, 0);

            var grpUp = new GroupBox();
            grpUp.Text = "Pembaruan";
            grpUp.Dock = DockStyle.Fill;
            lblUpdate = new Label();
            lblUpdate.Dock = DockStyle.Fill;
            lblUpdate.Padding = new Padding(6);
            grpUp.Controls.Add(lblUpdate);
            right.Controls.Add(grpUp, 0, 1);

            var grpLog = new GroupBox();
            grpLog.Text = "Log Terakhir";
            grpLog.Dock = DockStyle.Fill;
            grpLog.Padding = new Padding(6);
            txtLog = new TextBox();
            txtLog.Dock = DockStyle.Fill;
            txtLog.Multiline = true;
            txtLog.ReadOnly = true;
            txtLog.ScrollBars = ScrollBars.Vertical;
            txtLog.Font = new System.Drawing.Font("Consolas", 9F);
            grpLog.Controls.Add(txtLog);
            this.Controls.Add(grpLog);

            var btnRefresh = new Button();
            btnRefresh.Text = "Refresh";
            btnRefresh.Dock = DockStyle.Bottom;
            btnRefresh.Height = 34;
            btnRefresh.Click += (s, e) => { try { RefreshStats(); } catch (Exception) { } };
            this.Controls.Add(btnRefresh);

            this.FormClosing += (s, e) => { try { if (refreshTimer != null) refreshTimer.Stop(); } catch (Exception) { } };
            this.ResumeLayout(false);
        }

        private Label MakeStat(Control parent, string caption)
        {
            var box = new GroupBox();
            box.Text = caption;
            box.Width = 165;
            box.Height = 70;
            box.Margin = new Padding(4);
            var v = new Label();
            v.Text = "-";
            v.Font = new System.Drawing.Font("Segoe UI", 16F, System.Drawing.FontStyle.Bold);
            v.Dock = DockStyle.Fill;
            v.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
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

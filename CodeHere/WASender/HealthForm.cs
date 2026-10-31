using System;
using System.Diagnostics;
using System.IO;
using System.Windows.Forms;

namespace WASender
{
    public partial class HealthForm : Form
    {
        private Form _main;
        private TextBox txtStatus, txtLog;
        private ListBox lstReports;
        private string _dir = "";

        public HealthForm(Form main)
        {
            _main = main;
            Text = "WA Health & Laporan";
            Width = 720;
            Height = 560;
            StartPosition = FormStartPosition.CenterParent;
            FormClosed += delegate { try { if (_main != null) _main.Show(); } catch (Exception) { } };
            BuildUi();
            RefreshAll();
        }

        private void BuildUi()
        {
            try { _dir = Config.GetAppDataFolder(); } catch (Exception) { }
            Label l1 = new Label();
            l1.Text = "Status WA terakhir:"; l1.Left = 10; l1.Top = 8; l1.Width = 200;
            Controls.Add(l1);
            txtStatus = new TextBox();
            txtStatus.Multiline = true; txtStatus.ReadOnly = true; txtStatus.ScrollBars = ScrollBars.Vertical;
            txtStatus.Left = 10; txtStatus.Top = 28; txtStatus.Width = 680; txtStatus.Height = 120;
            Controls.Add(txtStatus);

            Label l2 = new Label();
            l2.Text = "Log kesehatan (terakhir):"; l2.Left = 10; l2.Top = 154; l2.Width = 200;
            Controls.Add(l2);
            txtLog = new TextBox();
            txtLog.Multiline = true; txtLog.ReadOnly = true; txtLog.ScrollBars = ScrollBars.Both;
            txtLog.Left = 10; txtLog.Top = 174; txtLog.Width = 680; txtLog.Height = 150;
            Controls.Add(txtLog);

            Label l3 = new Label();
            l3.Text = "Laporan harian:"; l3.Left = 10; l3.Top = 330; l3.Width = 200;
            Controls.Add(l3);
            lstReports = new ListBox();
            lstReports.Left = 10; lstReports.Top = 350; lstReports.Width = 680; lstReports.Height = 110;
            Controls.Add(lstReports);

            Button bRef = new Button();
            bRef.Text = "Refresh"; bRef.Left = 10; bRef.Top = 470; bRef.Width = 120;
            bRef.Click += delegate { RefreshAll(); };
            Controls.Add(bRef);
            Button bOpen = new Button();
            bOpen.Text = "Buka Laporan"; bOpen.Left = 140; bOpen.Top = 470; bOpen.Width = 120;
            bOpen.Click += delegate
            {
                try
                {
                    if (lstReports.SelectedItem != null)
                    {
                        string f = Path.Combine(Path.Combine(_dir, "Reports"), lstReports.SelectedItem.ToString());
                        if (File.Exists(f)) Process.Start(f);
                    }
                }
                catch (Exception ex) { MessageBox.Show(ex.Message, "Laporan"); }
            };
            Controls.Add(bOpen);
            Button bDir = new Button();
            bDir.Text = "Buka Folder Data"; bDir.Left = 270; bDir.Top = 470; bDir.Width = 140;
            bDir.Click += delegate
            {
                try { if (Directory.Exists(_dir)) Process.Start(_dir); }
                catch (Exception ex) { MessageBox.Show(ex.Message, "Folder"); }
            };
            Controls.Add(bDir);
        }

        private void RefreshAll()
        {
            try
            {
                string sf = Path.Combine(_dir, "wa_status.txt");
                txtStatus.Text = File.Exists(sf) ? File.ReadAllText(sf) : "(belum ada status — nyalakan Auto-Reply Bot)";
                string lf = Path.Combine(_dir, "wa_health.log");
                if (File.Exists(lf))
                {
                    string[] lines = File.ReadAllLines(lf);
                    int from = Math.Max(0, lines.Length - 120);
                    string[] tail = new string[lines.Length - from];
                    Array.Copy(lines, from, tail, 0, tail.Length);
                    txtLog.Text = string.Join("\r\n", tail);
                }
                else txtLog.Text = "(belum ada log)";
                lstReports.Items.Clear();
                string rep = Path.Combine(_dir, "Reports");
                if (Directory.Exists(rep))
                {
                    string[] fs = Directory.GetFiles(rep);
                    Array.Sort(fs);
                    Array.Reverse(fs);
                    foreach (string f in fs) lstReports.Items.Add(Path.GetFileName(f));
                }
                if (lstReports.Items.Count == 0) lstReports.Items.Add("(belum ada laporan)");
            }
            catch (Exception ex) { txtStatus.Text = "Gagal muat: " + ex.Message; }
        }
    }
}

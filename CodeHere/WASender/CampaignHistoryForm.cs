using MaterialSkin.Controls;
using System;
using System.Data;
using System.Data.SQLite;
using System.Windows.Forms;
using WaAutoReplyBot;

namespace WASender
{
    /// <summary>
    /// Riwayat Pengiriman (read-only): baca tabel send_quota di wa_monitor.db,
    /// agregat per tanggal + akun. Tanpa tulis DB, tanpa ubah schema, fail-soft.
    /// </summary>
    public partial class CampaignHistoryForm : MyMaterialPopOp
    {
        private DataGridView grid;
        private TextBox txtFind;
        private MaterialLabel lblTotal;

        public CampaignHistoryForm()
        {
            InitializeComponentManual();
            this.Icon = Strings.AppIcon;
            this.Text = "Riwayat Pengiriman";
            try { LoadHistory(""); } catch (Exception) { }
        }

        private void InitializeComponentManual()
        {
            this.SuspendLayout();
            this.ClientSize = new System.Drawing.Size(720, 520);
            this.StartPosition = FormStartPosition.CenterParent;
            this.MaximizeBox = false;

            var top = new Panel();
            top.Dock = DockStyle.Top;
            top.Height = 48;
            top.Padding = new Padding(12, 10, 12, 6);
            this.Controls.Add(top);

            txtFind = new TextBox();
            txtFind.Dock = DockStyle.Fill;
            txtFind.Text = "";
            txtFind.TextChanged += (s, e) => { try { LoadHistory(txtFind.Text.Trim()); } catch (Exception) { } };
            top.Controls.Add(txtFind);

            grid = new DataGridView();
            grid.Dock = DockStyle.Fill;
            grid.ReadOnly = true;
            grid.AllowUserToAddRows = false;
            grid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            grid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            this.Controls.Add(grid);
            grid.BringToFront();
            try { GridTheme.Apply(grid); } catch (Exception) { }

            lblTotal = new MaterialLabel();
            lblTotal.Dock = DockStyle.Bottom;
            lblTotal.Height = 30;
            this.Controls.Add(lblTotal);
            this.ResumeLayout(false);
        }

        private void LoadHistory(string filter)
        {
            var dt = new DataTable();
            dt.Columns.Add("Tanggal", typeof(string));
            dt.Columns.Add("Akun", typeof(string));
            dt.Columns.Add("Terkirim", typeof(int));
            long total = 0;
            try
            {
                string db = "";
                try { db = System.IO.Path.Combine(Config.GetAppDataFolder(), "wa_monitor.db"); } catch (Exception) { }
                if (System.IO.File.Exists(db))
                {
                    using (var c = new SQLiteConnection("Data Source=" + db + ";Version=3;"))
                    {
                        c.Open();
                        using (var cmd = c.CreateCommand())
                        {
                            cmd.CommandText = "SELECT date,account,n FROM send_quota ORDER BY date DESC LIMIT 500;";
                            using (var r = cmd.ExecuteReader())
                            {
                                while (r.Read())
                                {
                                    string d = Convert.ToString(r["date"]);
                                    string a = Convert.ToString(r["account"]);
                                    int n = 0;
                                    try { n = Convert.ToInt32(r["n"]); } catch (Exception) { }
                                    if (!string.IsNullOrEmpty(filter) && a.IndexOf(filter, StringComparison.OrdinalIgnoreCase) < 0
                                        && d.IndexOf(filter, StringComparison.OrdinalIgnoreCase) < 0) continue;
                                    dt.Rows.Add(d, a, n);
                                    total += n;
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception) { }
            try
            {
                grid.DataSource = dt;
                lblTotal.Text = "  " + dt.Rows.Count + " baris  •  total terkirim: " + total.ToString("N0");
            }
            catch (Exception) { }
        }
    }
}

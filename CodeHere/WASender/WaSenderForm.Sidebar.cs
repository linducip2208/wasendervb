using System;
using System.Drawing;
using System.Windows.Forms;

namespace WASender
{
    /// <summary>
    /// Shell enterprise T1: sidebar navy permanen + breadcrumb + anchor fix.
    /// UI-ONLY: semua aksi hanya memanggil handler/tab yang SUDAH ADA.
    /// Tidak ada perubahan business logic, tidak ada edit Designer.
    /// </summary>
    public partial class WaSenderForm
    {
        private Panel sideNav;
        private Button[] sideNavBtns;
        private Label sideActiveBar0, sideActiveBar1, sideActiveBar2;
        private Panel[] sideBars;

        private void BuildEnterpriseSidebar()
        {
            try
            {
                if (sideNav != null) return;

                sideNav = new Panel();
                sideNav.Name = "sideNavEnterprise";
                sideNav.Width = 216;
                sideNav.Dock = DockStyle.Left;
                sideNav.BackColor = Theme.Navy;
                sideNav.Padding = new Padding(0, 10, 0, 10);

                var logo = new Label();
                logo.Text = "WASender";
                logo.Font = new Font("Segoe UI", 16f, FontStyle.Bold);
                logo.ForeColor = Color.White;
                logo.Dock = DockStyle.Top;
                logo.Height = 40;
                logo.TextAlign = ContentAlignment.MiddleLeft;
                logo.Padding = new Padding(18, 0, 0, 0);
                sideNav.Controls.Add(logo);

                var ver = new Label();
                string vstr = "6.1";
                try { vstr = Strings.SoftwareVersion; } catch (Exception) { }
                ver.Text = "v" + vstr + "  Enterprise";
                ver.Font = new Font("Segoe UI", 8.5f, FontStyle.Regular);
                ver.ForeColor = Color.FromArgb(0x8B, 0x94, 0xA7);
                ver.Dock = DockStyle.Top;
                ver.Height = 20;
                ver.Padding = new Padding(19, 0, 0, 0);
                sideNav.Controls.Add(ver);

                var div = new Panel();
                div.Dock = DockStyle.Top;
                div.Height = 1;
                div.Margin = new Padding(0);
                div.Padding = new Padding(14, 8, 14, 8);
                var divInner = new Panel();
                divInner.Dock = DockStyle.Fill;
                divInner.BackColor = Theme.NavyLight;
                div.Controls.Add(divInner);
                sideNav.Controls.Add(div);

                AddSideCaption("KIRIM PESAN");
                var bDash = AddSideButton("Dashboard", 2);
                var bSingle = AddSideButton("Single Sender", 0);
                var bGroup = AddSideButton("Group Sender", 1);
                AddSideCaption("APLIKASI");
                var bSesi = AddSideButton("Sesi WhatsApp", -1);
                var bAkun = AddSideButton("Kelola Akun", -1);
                var bSum = AddSideButton("Ringkasan Harian", -1);
                var bSet = AddSideButton("Pengaturan", -1);

                bDash.Click += (s, e) => { try { tabMain.SelectedIndex = 2; } catch (Exception) { } RefreshSidebarState(); };
                bSingle.Click += (s, e) => { try { tabMain.SelectedIndex = 0; } catch (Exception) { } RefreshSidebarState(); };
                bGroup.Click += (s, e) => { try { tabMain.SelectedIndex = 1; } catch (Exception) { } RefreshSidebarState(); };
                bSesi.Click += (s, e) => { try { materialButton35_Click(s, e); } catch (Exception) { } RefreshSidebarState(); };
                bAkun.Click += (s, e) => { try { materialButton34_Click(s, e); } catch (Exception) { } RefreshSidebarState(); };
                bSum.Click += (s, e) => { try { materialButton32_Click(s, e); } catch (Exception) { } RefreshSidebarState(); };
                bSet.Click += (s, e) => { try { materialButton31_Click_1(s, e); } catch (Exception) { } RefreshSidebarState(); };

                sideNavBtns = new Button[] { bDash, bSingle, bGroup, bSesi, bAkun, bSum, bSet };

                this.Controls.Add(sideNav);

                try
                {
                    tabMain.Left = sideNav.Width + 6;
                    tabMain.Width = Math.Max(600, this.ClientSize.Width - tabMain.Left - 6);
                    tabMain.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
                }
                catch (Exception) { }
                try
                {
                    this.MinimumSize = new Size(1100, 700);
                    tabMain.SelectedIndexChanged += (s, e) => { try { RefreshSidebarState(); } catch (Exception) { } };
                }
                catch (Exception) { }
                RefreshSidebarState();
            }
            catch (Exception) { }
        }

        private void AddSideCaption(string text)
        {
            try
            {
                var c = new Label();
                c.Text = text;
                c.Font = new Font("Segoe UI", 7.5f, FontStyle.Bold);
                c.ForeColor = Color.FromArgb(0x8B, 0x94, 0xA7);
                c.Dock = DockStyle.Top;
                c.Height = 26;
                c.TextAlign = ContentAlignment.BottomLeft;
                c.Padding = new Padding(18, 0, 0, 4);
                sideNav.Controls.Add(c);
                c.BringToFront();
            }
            catch (Exception) { }
        }

        private Button AddSideButton(string text, int tag)
        {
            var wrap = new Panel();
            wrap.Dock = DockStyle.Top;
            wrap.Height = 44;
            wrap.BackColor = Color.Transparent;
            wrap.Padding = new Padding(0);

            var bar = new Panel();
            bar.Width = 4;
            bar.Dock = DockStyle.Left;
            bar.BackColor = Color.Transparent;
            wrap.Controls.Add(bar);

            var b = new Button();
            b.Text = "   " + text;
            b.Tag = tag;
            b.Dock = DockStyle.Fill;
            b.FlatStyle = FlatStyle.Flat;
            b.FlatAppearance.BorderSize = 0;
            b.FlatAppearance.MouseOverBackColor = Theme.NavyLight;
            b.BackColor = Color.Transparent;
            b.ForeColor = Color.FromArgb(0xC7, 0xCD, 0xDC);
            b.Font = new Font("Segoe UI", 10f, FontStyle.Regular);
            b.TextAlign = ContentAlignment.MiddleLeft;
            b.Cursor = Cursors.Hand;
            wrap.Controls.Add(b);

            sideNav.Controls.Add(wrap);
            wrap.BringToFront();
            if (sideBars == null) sideBars = new Panel[3];
            if (tag >= 0 && tag <= 2) sideBars[tag] = bar;
            return b;
        }

        private void RefreshSidebarState()
        {
            try
            {
                if (sideNavBtns == null) return;
                int idx = -1;
                try { idx = tabMain.SelectedIndex; } catch (Exception) { }
                foreach (Button b in sideNavBtns)
                {
                    try
                    {
                        int t = b.Tag is int ? (int)b.Tag : -1;
                        bool active = (t >= 0 && t == idx);
                        b.BackColor = active ? Theme.NavyLight : Color.Transparent;
                        b.ForeColor = active ? Color.White : Color.FromArgb(0xC7, 0xCD, 0xDC);
                        b.Font = new Font("Segoe UI", 10f, active ? FontStyle.Bold : FontStyle.Regular);
                    }
                    catch (Exception) { }
                }
                if (sideBars != null)
                {
                    for (int i = 0; i < sideBars.Length; i++)
                    {
                        try { if (sideBars[i] != null) sideBars[i].BackColor = (i == idx) ? Theme.Primary : Color.Transparent; }
                        catch (Exception) { }
                    }
                }
                try
                {
                    lblSection.Show();
                    lblSection.ForeColor = Color.White;
                    lblSection.Text = idx == 0 ? "Single Sender / Kontak & Pesan"
                        : idx == 1 ? "Group Sender / Grup & Pesan"
                        : "Dashboard / Tools & Ekstraktor";
                }
                catch (Exception) { }
            }
            catch (Exception) { }
        }
    }
}

using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace WASender
{
    /// <summary>
    /// Shell enterprise v2 (UI-ONLY): sidebar navy permanen + ikon, gantikan drawer-rail.
    /// Semua aksi hanya memanggil tab/handler/form yang SUDAH ADA. Nol edit Designer.
    /// </summary>
    public partial class WaSenderForm
    {
        private Panel sideNav;
        private FlowLayoutPanel sideFlow;
        private Button[] sideTabBtns;

        private void BuildEnterpriseSidebar()
        {
            try
            {
                if (sideNav != null) return;
                try { this.DrawerShowIconsWhenHidden = false; } catch (Exception) { }

                sideNav = new Panel();
                sideNav.Name = "sideNavEnterprise";
                sideNav.Width = 228;
                sideNav.Dock = DockStyle.Left;
                sideNav.BackColor = Theme.Navy;
                sideNav.Padding = new Padding(0, 10, 0, 10);

                var brand = new Panel();
                brand.Dock = DockStyle.Top;
                brand.Height = 62;
                brand.BackColor = Color.Transparent;
                brand.Padding = new Padding(14, 6, 10, 6);
                var pic = new PictureBox();
                pic.Width = 40; pic.Height = 40;
                pic.Left = 14; pic.Top = 8;
                pic.SizeMode = PictureBoxSizeMode.Zoom;
                try { pic.Image = Icon.ToBitmap(); } catch (Exception) { }
                brand.Controls.Add(pic);
                var nm = new Label();
                nm.Text = "WASender";
                nm.Font = new Font("Segoe UI", 14f, FontStyle.Bold);
                nm.ForeColor = Color.White;
                nm.Left = 62; nm.Top = 6; nm.Width = 150; nm.Height = 26;
                nm.BackColor = Color.Transparent;
                brand.Controls.Add(nm);
                string vstr = "6.1";
                try { vstr = Strings.SoftwareVersion; } catch (Exception) { }
                var vs = new Label();
                vs.Text = "v" + vstr + "  Enterprise";
                vs.Font = new Font("Segoe UI", 8.5f, FontStyle.Regular);
                vs.ForeColor = Color.FromArgb(0x8B, 0x94, 0xA7);
                vs.Left = 62; vs.Top = 32; vs.Width = 150; vs.Height = 18;
                vs.BackColor = Color.Transparent;
                brand.Controls.Add(vs);
                sideNav.Controls.Add(brand);

                sideFlow = new FlowLayoutPanel();
                sideFlow.Dock = DockStyle.Fill;
                sideFlow.FlowDirection = FlowDirection.TopDown;
                sideFlow.WrapContents = false;
                sideFlow.AutoScroll = true;
                sideFlow.BackColor = Color.Transparent;
                sideNav.Controls.Add(sideFlow);

                AddSideCaption("KIRIM PESAN");
                var bDash = AddSideButton("Dashboard", "icons8_whatsapp_24px", true);
                var bSingle = AddSideButton("Bulk Sender", "icons8_email_send_24px", true);
                var bGroup = AddSideButton("Group Sender", "GetWhatsAppGroup_Members_BLACK", true);
                AddSideCaption("EKSTRAKTOR DATA");
                var bGmap = AddSideButton("GMap Extractor", "GoogleMap_WHITE", false);
                var bFilter = AddSideButton("Number Filter", "NumberFilter_BLACK", false);
                var bGrab = AddSideButton("Contact Grabber", "icons8_contact_24px", false);
                AddSideCaption("GRUP & OTOMASI");
                var bMembers = AddSideButton("Grab Anggota Grup", "ActiveGroupMember_BLACK", false);
                var bJoiner = AddSideButton("Auto Group Joiner", "GroupJoiner_WHITE", false);
                var bReply = AddSideButton("Auto Responder", "WhatsAPpBot_BLACK", false);
                var bSched = AddSideButton("Scheduler", "icons8_schedule_24px", false);
                AddSideCaption("APLIKASI");
                var bSesi = AddSideButton("Sesi WhatsApp", "icons8_connected_24px", false);
                var bAkun = AddSideButton("Kelola Akun", "icons8_verified_account_24px", false);
                var bSum = AddSideButton("Ringkasan Harian", "icons8_documents_24px", false);
                var bSet = AddSideButton("Pengaturan", "icons8_settings_24px", false);

                bDash.Click += (s, e) => { try { tabMain.SelectedIndex = 2; } catch (Exception) { } RefreshSidebarState(); };
                bSingle.Click += (s, e) => { try { tabMain.SelectedIndex = 0; } catch (Exception) { } RefreshSidebarState(); };
                bGroup.Click += (s, e) => { try { tabMain.SelectedIndex = 1; } catch (Exception) { } RefreshSidebarState(); };
                bGmap.Click += (s, e) => OpenTool(() => { GmExtractorForm f = new GmExtractorForm(this); this.Hide(); f.Show(); });
                bFilter.Click += (s, e) => OpenTool(() => { NumberFilter f = new NumberFilter(this); this.Hide(); f.Show(); });
                bGrab.Click += (s, e) => OpenTool(() => { ContactGrabber f = new ContactGrabber(this); this.Hide(); f.Show(); });
                bMembers.Click += (s, e) => OpenTool(() => { GetGroupMember f = new GetGroupMember(this); this.Hide(); f.Show(); });
                bJoiner.Click += (s, e) => OpenTool(() => { GroupsJoiner f = new GroupsJoiner(this); this.Hide(); f.Show(); });
                bReply.Click += (s, e) => OpenTool(() => { WaAutoReplyBot.WaAutoReplyForm f = new WaAutoReplyBot.WaAutoReplyForm(this); this.Hide(); f.Show(); });
                bSched.Click += (s, e) => OpenTool(() => { AllSchedules f = new AllSchedules(this); this.Hide(); f.Show(); });
                bSesi.Click += (s, e) => { try { materialButton35_Click(s, e); } catch (Exception) { } RefreshSidebarState(); };
                bAkun.Click += (s, e) => { try { materialButton34_Click(s, e); } catch (Exception) { } RefreshSidebarState(); };
                bSum.Click += (s, e) => { try { materialButton32_Click(s, e); } catch (Exception) { } RefreshSidebarState(); };
                bSet.Click += (s, e) => { try { materialButton31_Click_1(s, e); } catch (Exception) { } RefreshSidebarState(); };

                sideTabBtns = new Button[] { bDash, bSingle, bGroup };

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
                    this.MinimumSize = new Size(1180, 720);
                    tabMain.SelectedIndexChanged += (s, e) => { try { RefreshSidebarState(); } catch (Exception) { } };
                }
                catch (Exception) { }
                try
                {
                    materialButton31.Visible = false;
                    materialButton32.Visible = false;
                    materialButton34.Visible = false;
                    materialButton35.Visible = false;
                }
                catch (Exception) { }
                RefreshSidebarState();
            }
            catch (Exception) { }
        }

        private void OpenTool(Action open)
        {
            try { MarkToolActive(); } catch (Exception) { }
            try { open(); } catch (Exception ex) { try { MessageBox.Show(ex.Message, Strings.Error, MessageBoxButtons.OK, MessageBoxIcon.Error); } catch (Exception) { } }
            try { RefreshSidebarState(); } catch (Exception) { }
        }

        private void MarkToolActive()
        {
            try
            {
                foreach (Control c in sideFlow.Controls)
                {
                    try
                    {
                        if (c is Panel && c.Tag as string == "navrow") c.BackColor = Color.Transparent;
                    }
                    catch (Exception) { }
                }
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
                c.Width = sideNav.Width - 24;
                c.Height = 26;
                c.TextAlign = ContentAlignment.BottomLeft;
                c.Margin = new Padding(14, 2, 0, 2);
                sideFlow.Controls.Add(c);
            }
            catch (Exception) { }
        }

        private Button AddSideButton(string text, string iconName, bool isTab)
        {
            var row = new Panel();
            row.Tag = "navrow";
            row.Width = sideNav.Width - 16;
            row.Height = 46;
            row.Margin = new Padding(8, 1, 8, 1);
            row.BackColor = Color.Transparent;

            var bar = new Panel();
            bar.Width = 4;
            bar.Dock = DockStyle.Left;
            bar.BackColor = Color.Transparent;
            bar.Tag = isTab ? "tabbar" : "bar";
            row.Controls.Add(bar);

            var tile = new Panel();
            tile.Width = 32; tile.Height = 32;
            tile.Left = 12; tile.Top = 7;
            tile.BackColor = Color.FromArgb(0xE8, 0xF5, 0xF3);
            try { tile.Region = new Region(RoundedRect(new Rectangle(0, 0, 32, 32), 8)); } catch (Exception) { }
            var img = new PictureBox();
            img.Dock = DockStyle.Fill;
            img.SizeMode = PictureBoxSizeMode.Zoom;
            img.BackColor = Color.Transparent;
            try
            {
                object o = Properties.Resources.ResourceManager.GetObject(iconName);
                if (o is Bitmap) img.Image = (Bitmap)o;
            }
            catch (Exception) { }
            tile.Controls.Add(img);
            row.Controls.Add(tile);

            var b = new Button();
            b.Text = text;
            b.Tag = isTab ? (object)text : null;
            b.FlatStyle = FlatStyle.Flat;
            b.FlatAppearance.BorderSize = 0;
            b.FlatAppearance.MouseOverBackColor = Theme.NavyLight;
            b.BackColor = Color.Transparent;
            b.ForeColor = Color.FromArgb(0xC7, 0xCD, 0xDC);
            b.Font = new Font("Segoe UI", 10f, FontStyle.Regular);
            b.TextAlign = ContentAlignment.MiddleLeft;
            b.Cursor = Cursors.Hand;
            b.Left = 50; b.Top = 0;
            b.Width = row.Width - 50; b.Height = 46;
            b.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            row.Controls.Add(b);

            row.Click += (s, e) => { try { b.PerformClick(); } catch (Exception) { } };
            sideFlow.Controls.Add(row);
            if (isTab) { try { row.Tag = "navrow-tab:" + text; } catch (Exception) { } }
            return b;
        }

        private static GraphicsPath RoundedRect(Rectangle r, int radius)
        {
            var p = new GraphicsPath();
            int d = radius * 2;
            p.AddArc(r.X, r.Y, d, d, 180, 90);
            p.AddArc(r.Right - d, r.Y, d, d, 270, 90);
            p.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90);
            p.AddArc(r.X, r.Bottom - d, d, d, 90, 90);
            p.CloseFigure();
            return p;
        }

        private void RefreshSidebarState()
        {
            try
            {
                if (sideFlow == null) return;
                int idx = -1;
                try { idx = tabMain.SelectedIndex; } catch (Exception) { }
                string[] tabNames = new string[] { "Dashboard", "Bulk Sender", "Group Sender" };
                foreach (Control c in sideFlow.Controls)
                {
                    try
                    {
                        string tag = c.Tag as string;
                        if (tag == null || !tag.StartsWith("navrow-tab:")) continue;
                        string nm = tag.Substring("navrow-tab:".Length);
                        int want = nm == "Dashboard" ? 2 : (nm == "Bulk Sender" ? 0 : (nm == "Group Sender" ? 1 : -1));
                        bool active = (want >= 0 && want == idx);
                        c.BackColor = active ? Theme.NavyLight : Color.Transparent;
                        foreach (Control k in c.Controls)
                        {
                            try
                            {
                                if (k is Button) { k.ForeColor = active ? Color.White : Color.FromArgb(0xC7, 0xCD, 0xDC); k.Font = new Font("Segoe UI", 10f, active ? FontStyle.Bold : FontStyle.Regular); }
                                if (k is Panel && (k.Tag as string) == "tabbar") k.BackColor = active ? Theme.Primary : Color.Transparent;
                            }
                            catch (Exception) { }
                        }
                    }
                    catch (Exception) { }
                }
                try
                {
                    lblSection.Show();
                    lblSection.ForeColor = Color.White;
                    lblSection.Text = idx == 0 ? "Bulk Sender / Kontak & Pesan"
                        : idx == 1 ? "Group Sender / Grup & Pesan"
                        : "Dashboard / Tools & Ekstraktor";
                }
                catch (Exception) { }
            }
            catch (Exception) { }
        }
    }
}

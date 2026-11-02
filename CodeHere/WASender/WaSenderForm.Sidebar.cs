using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Windows.Forms;

namespace WASender
{
    /// <summary>
    /// Shell enterprise v3 (UI-ONLY): sidebar navy permanen, SEMUA 22+ fitur asli,
    /// ikon putih + section. Semua aksi hanya memanggil tab/handler/form yang SUDAH ADA.
    /// </summary>
    public partial class WaSenderForm
    {
        private Panel sideNav;
        private FlowLayoutPanel sideFlow;

        private void BuildEnterpriseSidebar()
        {
            try
            {
                if (sideNav != null) return;
                try { this.DrawerShowIconsWhenHidden = false; } catch (Exception) { }
                try
                {
                    foreach (Control pg in tabMain.TabPages)
                    { try { pg.BackColor = Color.FromArgb(0xF2, 0xF4, 0xF7); } catch (Exception) { } }
                }
                catch (Exception) { }

                sideNav = new Panel();
                sideNav.Name = "sideNavEnterprise";
                sideNav.Width = 232;
                sideNav.Dock = DockStyle.Left;
                sideNav.BackColor = Theme.Navy;
                sideNav.Padding = new Padding(0, 10, 0, 10);

                var brand = new Panel();
                brand.Dock = DockStyle.Top;
                brand.Height = 62;
                brand.BackColor = Color.Transparent;
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
                nm.Left = 62; nm.Top = 6; nm.Width = 155; nm.Height = 26;
                nm.BackColor = Color.Transparent;
                brand.Controls.Add(nm);
                string vstr = "6.1";
                try { vstr = Strings.SoftwareVersion; } catch (Exception) { }
                var vs = new Label();
                vs.Text = "v" + vstr + "  Enterprise";
                vs.Font = new Font("Segoe UI", 8.5f, FontStyle.Regular);
                vs.ForeColor = Color.FromArgb(0x8B, 0x94, 0xA7);
                vs.Left = 62; vs.Top = 32; vs.Width = 155; vs.Height = 18;
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

                AddSideCaption("UTAMA");
                AddTabItem("Dashboard", 2, "icons8_whatsapp_24px");
                AddTabItem("Bulk Sender", 0, "icons8_email_send_24px");
                AddTabItem("Group Sender", 1, "GetWhatsAppGroup_Members_BLACK");
                AddSideCaption("EKSTRAKTOR DATA");
                AddToolItem("GMap Extractor", "GoogleMap_WHITE", () => { GmExtractorForm f = new GmExtractorForm(this); this.Hide(); f.Show(); });
                AddToolItem("Number Filter", "NumberFilter_BLACK", () => { NumberFilter f = new NumberFilter(this); this.Hide(); f.Show(); });
                AddToolItem("Contact Grabber", "WContactList_BLACK", () => { ContactGrabber f = new ContactGrabber(this); this.Hide(); f.Show(); });
                AddToolItem("Web Extractor", "WebExtract", () => { WebScrapper f = new WebScrapper(this); this.Hide(); f.Show(); });
                AddToolItem("Social Media", "icons8_globe_24px", () => { SocialMediaExtractor f = new SocialMediaExtractor(this); this.Hide(); f.Show(); });
                AddToolItem("Google CSV", "CSV_BLACK", () => { GoogleCSVGenerator f = new GoogleCSVGenerator(this); this.Hide(); f.Show(); });
                AddToolItem("Chat List", "ChatList_BLACK", () => { GrabChatList f = new GrabChatList(this); this.Hide(); f.Show(); });
                AddToolItem("Grab Link Grup", "GetGroupLinksFromWeb_BLACK", () => { GrabGroupLinks f = new GrabGroupLinks(this); this.Hide(); f.Show(); });
                AddSideCaption("GRUP");
                AddToolItem("Grab Anggota", "ActiveGroupMember_BLACK", () => { GetGroupMember f = new GetGroupMember(this); this.Hide(); f.Show(); });
                AddToolItem("Anggota Aktif", "icons8_checked_checkbox_24px", () => { GrabGroupActiveMembers f = new GrabGroupActiveMembers(this); this.Hide(); f.Show(); });
                AddToolItem("Tambah Anggota", "AddGroupMember_BLACK", () => { GroupMemberAdder f = new GroupMemberAdder(this); this.Hide(); f.Show(); });
                AddToolItem("Auto Joiner", "GroupJoiner_WHITE", () => { GroupsJoiner f = new GroupsJoiner(this); this.Hide(); f.Show(); });
                AddToolItem("Group Finder", "GriupFinder_BLACK", () => { GroupFinder f = new GroupFinder(this); this.Hide(); f.Show(); });
                AddToolItem("Bulk Generator", "CreateBulkWhatsAppGroup_BLACK", () => { GroupGenerator f = new GroupGenerator(this); this.Hide(); f.Show(); });
                AddToolItem("Verifikasi Link", "icons8_linking_24px", () => { GroupLinkVerify f = new GroupLinkVerify(this); this.Hide(); f.Show(); });
                AddSideCaption("OTOMASI");
                AddToolItem("Auto Responder", "WhatsAPpBot_BLACK", () => { WaAutoReplyBot.WaAutoReplyForm f = new WaAutoReplyBot.WaAutoReplyForm(this); this.Hide(); f.Show(); });
                AddToolItem("Scheduler", "icons8_schedule_24px", () => { AllSchedules f = new AllSchedules(this); this.Hide(); f.Show(); });
                AddToolItem("WA Warmer", "warmer", () => { Warmer f = new Warmer(this); this.Hide(); f.Show(); });
                AddToolItem("Group Warmer", "warmer1", () => { Warmer f = new Warmer(this, true); this.Hide(); f.Show(); });
                AddToolItem("Hasil Poll", "icons8_checkmark_24px", () => { GetPollResults f = new GetPollResults(this); this.Hide(); f.Show(); });
                AddToolItem("AI Setup", "icons8_natural_user_interface_2_24px", () => { AiSettingsForm f = new AiSettingsForm(this); this.Hide(); f.Show(); });
                AddToolItem("WA Health", "icons8_refresh_shield_24px", () => { HealthForm f = new HealthForm(this); this.Hide(); f.Show(); });
                AddSideCaption("APLIKASI");
                AddActionItem("Sesi WhatsApp", "icons8_connected_24px", (s, e) => { try { materialButton35_Click(s, e); } catch (Exception) { } });
                AddActionItem("Kelola Akun", "icons8_verified_account_24px", (s, e) => { try { materialButton34_Click(s, e); } catch (Exception) { } });
                AddActionItem("Ringkasan Harian", "icons8_documents_24px", (s, e) => { try { materialButton32_Click(s, e); } catch (Exception) { } });
                AddActionItem("Riwayat Kirim", "success", (s, e) => { try { CampaignHistoryForm f = new CampaignHistoryForm(); f.ShowDialog(this); } catch (Exception) { } });
                AddActionItem("Tutorial", "icons8_help_24px", (s, e) => { try { TutorialForm t = new TutorialForm(); t.Show(); } catch (Exception) { } });
                AddActionItem("Pengaturan", "icons8_settings_24px", (s, e) => { try { materialButton31_Click_1(s, e); } catch (Exception) { } });

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
                    try { tabMain.SelectedIndexChanged -= tabMain_SelectedIndexChanged; } catch (Exception) { }
                    tabMain.SelectedIndexChanged += (s, e) => { try { RefreshSidebarState(); } catch (Exception) { } };
                }
                catch (Exception) { }
                try { tabMain.SelectedIndex = 2; } catch (Exception) { }
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

        private void AddSideCaption(string text)
        {
            try
            {
                var c = new Label();
                c.Text = text;
                c.Font = new Font("Segoe UI", 7.5f, FontStyle.Bold);
                c.ForeColor = Color.FromArgb(0x8B, 0x94, 0xA7);
                c.Width = sideNav.Width - 24;
                c.Height = 24;
                c.TextAlign = ContentAlignment.BottomLeft;
                c.Margin = new Padding(14, 4, 0, 2);
                sideFlow.Controls.Add(c);
            }
            catch (Exception) { }
        }

        private void AddTabItem(string text, int tabIdx, string iconName)
        {
            try
            {
                Button b = AddSideRow(text, iconName);
                b.Tag = "tab:" + tabIdx;
                b.Click += (s, e) => { try { tabMain.SelectedIndex = tabIdx; } catch (Exception) { } RefreshSidebarState(); };
                try { b.Parent.Tag = "navrow-tab:" + text; } catch (Exception) { }
            }
            catch (Exception) { }
        }

        private void AddToolItem(string text, string iconName, Action open)
        {
            try
            {
                Button b = AddSideRow(text, iconName);
                b.Click += (s, e) =>
                {
                    try
                    {
                        MarkToolActive(b);
                        try { open(); }
                        catch (Exception ex) { try { MessageBox.Show(ex.Message, Strings.Error, MessageBoxButtons.OK, MessageBoxIcon.Error); } catch (Exception) { } }
                    }
                    catch (Exception) { }
                    RefreshSidebarState();
                };
            }
            catch (Exception) { }
        }

        private void AddActionItem(string text, string iconName, Action<object, EventArgs> act)
        {
            try
            {
                Button b = AddSideRow(text, iconName);
                b.Click += (s, e) => { try { act(s, e); } catch (Exception) { } RefreshSidebarState(); };
            }
            catch (Exception) { }
        }

        private Button AddSideRow(string text, string iconName)
        {
            var row = new Panel();
            row.Tag = "navrow";
            row.Width = sideNav.Width - 14;
            row.Height = 42;
            row.Margin = new Padding(7, 1, 7, 1);
            row.BackColor = Theme.Navy;
            Button b = null;

            var bar = new Panel();
            bar.Width = 4;
            bar.Dock = DockStyle.Left;
            bar.BackColor = Color.Transparent;
            bar.Tag = "bar";
            row.Controls.Add(bar);

            var img = new PictureBox();
            img.Width = 22; img.Height = 22;
            img.Left = 14; img.Top = 10;
            img.SizeMode = PictureBoxSizeMode.Zoom;
            img.BackColor = Color.Transparent;
            Bitmap iconBmp = null;
            try { iconBmp = WhiteIcon(Properties.Resources.ResourceManager.GetObject(iconName)); } catch (Exception) { }
            if (iconBmp != null)
            {
                img.Image = iconBmp;
                row.Controls.Add(img);
            }
            else
            {
                var fb = new Label();
                fb.Text = string.IsNullOrEmpty(text) ? "•" : text.Substring(0, 1).ToUpper();
                fb.Font = new Font("Segoe UI", 13f, FontStyle.Bold);
                fb.ForeColor = Color.White;
                fb.BackColor = Theme.Navy;
                fb.Left = 14; fb.Top = 6; fb.Width = 24; fb.Height = 28;
                fb.TextAlign = ContentAlignment.MiddleCenter;
                fb.Click += (s, e) => { try { b.PerformClick(); } catch (Exception) { } };
                row.Controls.Add(fb);
            }

            b = new Button();
            b.Text = text;
            b.FlatStyle = FlatStyle.Flat;
            b.FlatAppearance.BorderSize = 0;
            b.FlatAppearance.MouseOverBackColor = Theme.NavyLight;
            b.BackColor = Theme.Navy;
            b.ForeColor = Color.FromArgb(0xC7, 0xCD, 0xDC);
            b.Font = new Font("Segoe UI", 10f, FontStyle.Regular);
            b.TextAlign = ContentAlignment.MiddleLeft;
            b.Cursor = Cursors.Hand;
            b.Left = 44; b.Top = 0;
            b.Width = row.Width - 44; b.Height = 42;
            b.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            row.Controls.Add(b);

            row.Click += (s, e) => { try { b.PerformClick(); } catch (Exception) { } };
            img.Click += (s, e) => { try { b.PerformClick(); } catch (Exception) { } };
            sideFlow.Controls.Add(row);
            return b;
        }

        /// <summary>Hitam/berwarna -&gt; putih solid dgn alpha asli. Pixel-loop: anti-gagal utk semua format PNG.</summary>
        private static Bitmap WhiteIcon(object o)
        {
            try
            {
                Bitmap src = o as Bitmap;
                if (src == null) return null;
                Bitmap dst = new Bitmap(src.Width, src.Height, PixelFormat.Format32bppArgb);
                using (Graphics g = Graphics.FromImage(dst))
                    g.DrawImage(src, 0, 0, src.Width, src.Height);
                for (int y = 0; y < dst.Height; y++)
                    for (int x = 0; x < dst.Width; x++)
                    {
                        Color p = dst.GetPixel(x, y);
                        if (p.A > 0) dst.SetPixel(x, y, Color.FromArgb(p.A, 255, 255, 255));
                    }
                return dst;
            }
            catch (Exception) { return null; }
        }

        private void MarkToolActive(Button b)
        {
            try
            {
                foreach (Control c in sideFlow.Controls)
                {
                    try { if (c is Panel && (c.Tag as string) == "navrow") c.BackColor = Theme.Navy; } catch (Exception) { }
                }
                try { if (b != null && b.Parent != null) b.Parent.BackColor = Theme.NavyLight; } catch (Exception) { }
            }
            catch (Exception) { }
        }

        private void RefreshSidebarState()
        {
            try
            {
                if (sideFlow == null) return;
                int idx = -1;
                try { idx = tabMain.SelectedIndex; } catch (Exception) { }
                foreach (Control c in sideFlow.Controls)
                {
                    try
                    {
                        string tag = c.Tag as string;
                        if (tag == null || !tag.StartsWith("navrow-tab:")) continue;
                        string nm = tag.Substring("navrow-tab:".Length);
                        int want = nm == "Dashboard" ? 2 : (nm == "Bulk Sender" ? 0 : (nm == "Group Sender" ? 1 : -1));
                        bool active = (want >= 0 && want == idx);
                        c.BackColor = active ? Theme.NavyLight : Theme.Navy;
                        foreach (Control k in c.Controls)
                        {
                            try
                            {
                                if (k is Button) { k.ForeColor = active ? Color.White : Color.FromArgb(0xC7, 0xCD, 0xDC); k.Font = new Font("Segoe UI", 10f, active ? FontStyle.Bold : FontStyle.Regular); }
                                if (k is Panel && (k.Tag as string) == "bar") k.BackColor = active ? Theme.Primary : Color.Transparent;
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

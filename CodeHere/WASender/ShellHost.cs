using MaterialSkin;
using MaterialSkin.Controls;
using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Windows.Forms;

namespace WASender
{
    /// <summary>
    /// Shell utama modern (UI-ONLY, nol ubah logic):
    /// MaterialForm hijau + sidebar navy permanen + strip breadcrumb putih + area konten.
    /// Tiga halaman tab (Bulk/Group/Dashboard) dipindah (reparent) dari WaSenderForm
    /// yang berjalan tersembunyi sebagai pemilik logika. Semua kontrol &amp; event
    /// adalah instance ASLI — tidak ada duplikat, tidak ada perubahan perilaku.
    /// Gagal kapan pun -&gt; fallback ke WaSenderForm normal.
    /// </summary>
    public partial class ShellHost : MaterialForm
    {
        private WaSenderForm main;
        private Panel sideNav;
        private FlowLayoutPanel sideFlow;
        private Panel content;
        private Label crumb;
        private Control pageBulk;
        private Control pageGroup;
        private Control pageTools;
        private bool hosting = false;

        public ShellHost(string[] args)
        {
            try
            {
                this.Text = "WASender v6.1.0";
                this.StartPosition = FormStartPosition.CenterScreen;
                this.ClientSize = new Size(1400, 860);
                this.MinimumSize = new Size(1180, 720);
                try { this.Icon = Strings.AppIcon; } catch (Exception) { }

                var mgr = MaterialSkinManager.Instance;
                mgr.AddFormToManage(this);
                mgr.Theme = MaterialSkinManager.Themes.LIGHT;
                mgr.ColorScheme = new ColorScheme(Primary.Green700, Primary.Green700, Primary.Green200, Accent.Green700, TextShade.WHITE);

                BuildChrome();
                BootMain(args);
            }
            catch (Exception ex)
            {
                try { LogStep("FATAL ctor: " + ex.ToString()); } catch (Exception) { }
                try { Fallback(ex); } catch (Exception) { }
                try { MessageBox.Show("Shell gagal dibuka, kembali ke tampilan lama.\n\nDetail: " + ex.Message, "WASender", MessageBoxButtons.OK, MessageBoxIcon.Warning); } catch (Exception) { }
            }
        }

        private static void LogStep(string s)
        {
            try
            {
                string dir = System.IO.Path.Combine(System.Environment.GetFolderPath(System.Environment.SpecialFolder.CommonApplicationData), "WaSender");
                if (!System.IO.Directory.Exists(dir)) System.IO.Directory.CreateDirectory(dir);
                System.IO.File.AppendAllText(System.IO.Path.Combine(dir, "crash.log"),
                    System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + " [ShellHost] " + s + System.Environment.NewLine);
            }
            catch (Exception) { }
        }

        private void BuildChrome()
        {
            sideNav = new Panel();
            sideNav.Width = 232;
            sideNav.Dock = DockStyle.Left;
            sideNav.BackColor = Theme.Navy;
            sideNav.Padding = new Padding(0, 10, 0, 10);
            this.Controls.Add(sideNav);

            var brand = new Panel();
            brand.Dock = DockStyle.Top;
            brand.Height = 62;
            brand.BackColor = System.Drawing.Color.Transparent;
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
            nm.BackColor = System.Drawing.Color.Transparent;
            brand.Controls.Add(nm);
            string vstr = "6.1";
            try { vstr = Strings.SoftwareVersion; } catch (Exception) { }
            var vs = new Label();
            vs.Text = "v" + vstr + "  Enterprise";
            vs.Font = new Font("Segoe UI", 8.5f, FontStyle.Regular);
            vs.ForeColor = Color.FromArgb(0x8B, 0x94, 0xA7);
            vs.Left = 62; vs.Top = 32; vs.Width = 155; vs.Height = 18;
            vs.BackColor = System.Drawing.Color.Transparent;
            brand.Controls.Add(vs);
            sideNav.Controls.Add(brand);

            sideFlow = new FlowLayoutPanel();
            sideFlow.Dock = DockStyle.Fill;
            sideFlow.FlowDirection = FlowDirection.TopDown;
            sideFlow.WrapContents = false;
            sideFlow.AutoScroll = true;
            sideFlow.BackColor = System.Drawing.Color.Transparent;
            sideNav.Controls.Add(sideFlow);

            var topbar = new Panel();
            topbar.Dock = DockStyle.Top;
            topbar.Height = 54;
            topbar.BackColor = Color.White;
            topbar.Padding = new Padding(18, 0, 18, 0);
            this.Controls.Add(topbar);

            crumb = new Label();
            crumb.Text = "Dashboard";
            crumb.Font = new Font("Segoe UI", 12f, FontStyle.Bold);
            crumb.ForeColor = Theme.TextPrimary;
            crumb.Dock = DockStyle.Left;
            crumb.TextAlign = ContentAlignment.MiddleLeft;
            crumb.Width = 600;
            topbar.Controls.Add(crumb);

            var vr = new Label();
            vr.Font = new Font("Segoe UI", 9f, FontStyle.Regular);
            vr.ForeColor = Theme.TextSecondary;
            vr.Dock = DockStyle.Right;
            vr.TextAlign = ContentAlignment.MiddleRight;
            vr.Width = 220;
            try { vr.Text = "WASender v" + Strings.SoftwareVersion; } catch (Exception) { vr.Text = "WASender"; }
            topbar.Controls.Add(vr);

            var line = new Panel();
            line.Dock = DockStyle.Top;
            line.Height = 1;
            line.BackColor = Theme.CardBorder;
            this.Controls.Add(line);

            content = new Panel();
            content.Dock = DockStyle.Fill;
            content.BackColor = Color.FromArgb(0xF2, 0xF4, 0xF7);
            this.Controls.Add(content);

            try
            {
                this.Controls.SetChildIndex(content, 0);
                this.Controls.SetChildIndex(line, 1);
                this.Controls.SetChildIndex(topbar, 2);
                this.Controls.SetChildIndex(sideNav, 3);
                sideNav.Controls.SetChildIndex(brand, 1);
            }
            catch (Exception) { }
        }

        private void BuildMenu()
        {
            AddSideCaption("UTAMA");
            AddTabItem("Dashboard", "P_DASH", "icons8_whatsapp_24px");
            AddTabItem("Bulk Sender", "P_BULK", "icons8_email_send_24px");
            AddTabItem("Group Sender", "P_GROUP", "GetWhatsAppGroup_Members_BLACK");
            AddSideCaption("EKSTRAKTOR DATA");
            AddToolItem("GMap Extractor", "GoogleMap_WHITE", () => { GmExtractorForm f = new GmExtractorForm(main); f.Show(); });
            AddToolItem("Number Filter", "NumberFilter_BLACK", () => { NumberFilter f = new NumberFilter(main); f.Show(); });
            AddToolItem("Contact Grabber", "WContactList_BLACK", () => { ContactGrabber f = new ContactGrabber(main); f.Show(); });
            AddToolItem("Web Extractor", "WebExtract", () => { WebScrapper f = new WebScrapper(main); f.Show(); });
            AddToolItem("Social Media", "icons8_globe_24px", () => { SocialMediaExtractor f = new SocialMediaExtractor(main); f.Show(); });
            AddToolItem("Google CSV", "CSV_BLACK", () => { GoogleCSVGenerator f = new GoogleCSVGenerator(main); f.Show(); });
            AddToolItem("Chat List", "ChatList_BLACK", () => { GrabChatList f = new GrabChatList(main); f.Show(); });
            AddToolItem("Grab Link Grup", "GetGroupLinksFromWeb_BLACK", () => { GrabGroupLinks f = new GrabGroupLinks(main); f.Show(); });
            AddSideCaption("GRUP");
            AddToolItem("Grab Anggota", "ActiveGroupMember_BLACK", () => { GetGroupMember f = new GetGroupMember(main); f.Show(); });
            AddToolItem("Anggota Aktif", "icons8_checked_checkbox_24px", () => { GrabGroupActiveMembers f = new GrabGroupActiveMembers(main); f.Show(); });
            AddToolItem("Tambah Anggota", "AddGroupMember_BLACK", () => { GroupMemberAdder f = new GroupMemberAdder(main); f.Show(); });
            AddToolItem("Auto Joiner", "GroupJoiner_WHITE", () => { GroupsJoiner f = new GroupsJoiner(main); f.Show(); });
            AddToolItem("Group Finder", "GriupFinder_BLACK", () => { GroupFinder f = new GroupFinder(main); f.Show(); });
            AddToolItem("Bulk Generator", "CreateBulkWhatsAppGroup_BLACK", () => { GroupGenerator f = new GroupGenerator(main); f.Show(); });
            AddToolItem("Verifikasi Link", "icons8_linking_24px", () => { GroupLinkVerify f = new GroupLinkVerify(main); f.Show(); });
            AddSideCaption("OTOMASI");
            AddToolItem("Auto Responder", "WhatsAPpBot_BLACK", () => { WaAutoReplyBot.WaAutoReplyForm f = new WaAutoReplyBot.WaAutoReplyForm(main); f.Show(); });
            AddToolItem("Scheduler", "icons8_schedule_24px", () => { AllSchedules f = new AllSchedules(main); f.Show(); });
            AddToolItem("WA Warmer", "warmer", () => { Warmer f = new Warmer(main); f.Show(); });
            AddToolItem("Group Warmer", "warmer1", () => { Warmer f = new Warmer(main, true); f.Show(); });
            AddToolItem("Hasil Poll", "icons8_checkmark_24px", () => { GetPollResults f = new GetPollResults(main); f.Show(); });
            AddToolItem("AI Setup", "icons8_natural_user_interface_2_24px", () => { AiSettingsForm f = new AiSettingsForm(main); f.Show(); });
            AddToolItem("WA Health", "icons8_refresh_shield_24px", () => { HealthForm f = new HealthForm(main); f.Show(); });
            AddSideCaption("APLIKASI");
            AddActionItem("Sesi WhatsApp", "icons8_connected_24px", () =>
            {
                if (Utils.waSenderBrowser != null) { try { Utils.waSenderBrowser.BringToFront(); } catch (Exception) { } }
                else { var browser = new WaSenderBrowser(); Utils.waSenderBrowser = browser; browser.Show(); }
            });
            AddActionItem("Kelola Akun", "icons8_verified_account_24px", () => { ManageAccounts f = new ManageAccounts(); f.ShowDialog(this); });
            AddActionItem("Ringkasan Harian", "icons8_documents_24px", () => { DailySummaryForm f = new DailySummaryForm(); f.ShowDialog(this); });
            AddActionItem("Riwayat Kirim", "success", () => { CampaignHistoryForm f = new CampaignHistoryForm(); f.ShowDialog(this); });
            AddActionItem("Tutorial", "icons8_help_24px", () => { TutorialForm f = new TutorialForm(); f.Show(); });
            AddActionItem("Pengaturan", "icons8_settings_24px", () => { GeneralSettings g = new GeneralSettings(main); g.ShowDialog(this); });
        }

        private void BootMain(string[] args)
        {
            LogStep("BootMain mulai");
            main = new WaSenderForm(args);
            LogStep("WaSenderForm dibuat");
            main.ShowInTaskbar = false;
            main.Opacity = 0;
            main.Show();
            LogStep("main.Show OK (Form1_Load lewat)");
            try { main.Hide(); } catch (Exception) { }
            try
            {
                main.VisibleChanged += (s, e) => { try { if (hosting && main.Visible) main.Hide(); } catch (Exception) { } };
                main.FormClosed += (s, e) => { try { if (!hosting) Application.Exit(); } catch (Exception) { } };
            }
            catch (Exception) { }
            hosting = true;

            TabControl tc = null;
            try
            {
                var found = main.Controls.Find("tabMain", true);
                if (found != null && found.Length > 0) tc = found[0] as TabControl;
            }
            catch (Exception) { }
            if (tc == null || tc.TabPages.Count < 3) throw new Exception("tabMain tidak ditemukan");
            LogStep("tabMain ketemu, pages=" + tc.TabPages.Count);

            pageBulk = tc.TabPages[0];
            pageGroup = tc.TabPages[1];
            pageTools = tc.TabPages[2];
            foreach (Control pg in new Control[] { pageBulk, pageGroup, pageTools })
            {
                try
                {
                    tc.TabPages.Remove(pg as TabPage);
                    pg.Dock = DockStyle.Fill;
                    pg.Visible = false;
                    try { pg.BackColor = Color.FromArgb(0xF2, 0xF4, 0xF7); } catch (Exception) { }
                    content.Controls.Add(pg);
                }
                catch (Exception) { }
            }

            BuildMenu();
            LogStep("menu OK");
            ShowPage("P_DASH");
            LogStep("BootMain selesai OK");
        }

        private void ShowPage(string key)
        {
            try
            {
                foreach (Control pg in new Control[] { pageBulk, pageGroup, pageTools })
                    try { if (pg != null) pg.Visible = false; } catch (Exception) { }
                Control want = key == "P_BULK" ? pageBulk : (key == "P_GROUP" ? pageGroup : pageTools);
                if (want != null) { want.Visible = true; try { want.BringToFront(); } catch (Exception) { } }
                try
                {
                    crumb.Text = key == "P_BULK" ? "Bulk Sender / Kontak & Pesan"
                        : key == "P_GROUP" ? "Group Sender / Grup & Pesan"
                        : "Dashboard / Tools & Ekstraktor";
                }
                catch (Exception) { }
                RefreshSidebarState(key);
            }
            catch (Exception) { }
        }

        private void Fallback(Exception ex)
        {
            try
            {
                hosting = false;
                if (main != null)
                {
                    try { main.Opacity = 1; } catch (Exception) { }
                    try { main.ShowInTaskbar = true; } catch (Exception) { }
                    try { main.Show(); } catch (Exception) { }
                    try { if (main.Visible) this.Hide(); } catch (Exception) { }
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
                c.Height = 24;
                c.TextAlign = ContentAlignment.BottomLeft;
                c.Margin = new Padding(14, 4, 0, 2);
                sideFlow.Controls.Add(c);
            }
            catch (Exception) { }
        }

        private void AddTabItem(string text, string key, string iconName)
        {
            try
            {
                Button b = AddSideRow(text, iconName);
                try { b.Parent.Tag = "navrow-tab:" + key; } catch (Exception) { }
                b.Click += (s, e) => { try { ShowPage(key); } catch (Exception) { } };
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
                    try { RefreshSidebarState(null); } catch (Exception) { }
                };
            }
            catch (Exception) { }
        }

        private void AddActionItem(string text, string iconName, Action act)
        {
            try
            {
                Button b = AddSideRow(text, iconName);
                b.Click += (s, e) => { try { act(); } catch (Exception) { } try { RefreshSidebarState(null); } catch (Exception) { } };
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
            row.BackColor = System.Drawing.Color.Transparent;
            Button b = null;

            var bar = new Panel();
            bar.Width = 4;
            bar.Dock = DockStyle.Left;
            bar.BackColor = System.Drawing.Color.Transparent;
            bar.Tag = "bar";
            row.Controls.Add(bar);

            var img = new PictureBox();
            img.Width = 22; img.Height = 22;
            img.Left = 14; img.Top = 10;
            img.SizeMode = PictureBoxSizeMode.Zoom;
            img.BackColor = System.Drawing.Color.Transparent;
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
                fb.Text = string.IsNullOrEmpty(text) ? "\u2022" : text.Substring(0, 1).ToUpper();
                fb.Font = new Font("Segoe UI", 13f, FontStyle.Bold);
                fb.ForeColor = Color.White;
                fb.BackColor = System.Drawing.Color.Transparent;
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
            b.BackColor = System.Drawing.Color.Transparent;
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
                    try { if (c is Panel && (c.Tag as string) == "navrow") c.BackColor = System.Drawing.Color.Transparent; } catch (Exception) { }
                }
                try { if (b != null && b.Parent != null) b.Parent.BackColor = Theme.NavyLight; } catch (Exception) { }
            }
            catch (Exception) { }
        }

        private void RefreshSidebarState(string activeKey)
        {
            try
            {
                if (sideFlow == null) return;
                foreach (Control c in sideFlow.Controls)
                {
                    try
                    {
                        string tag = c.Tag as string;
                        if (tag == null || !tag.StartsWith("navrow-tab:")) continue;
                        string key = tag.Substring("navrow-tab:".Length);
                        bool active = (key == activeKey);
                        c.BackColor = active ? Theme.NavyLight : System.Drawing.Color.Transparent;
                        foreach (Control k in c.Controls)
                        {
                            try
                            {
                                if (k is Button) { k.ForeColor = active ? Color.White : Color.FromArgb(0xC7, 0xCD, 0xDC); k.Font = new Font("Segoe UI", 10f, active ? FontStyle.Bold : FontStyle.Regular); }
                                if (k is Panel && (k.Tag as string) == "bar") k.BackColor = active ? Theme.Primary : System.Drawing.Color.Transparent;
                            }
                            catch (Exception) { }
                        }
                    }
                    catch (Exception) { }
                }
            }
            catch (Exception) { }
        }
    }
}

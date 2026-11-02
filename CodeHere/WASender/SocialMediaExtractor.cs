using MaterialSkin.Controls;
using Microsoft.Win32;
using Microsoft.Web.WebView2.WinForms;
using OfficeOpenXml;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Media;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using System.Windows.Forms;
using WASender.enums;


namespace WASender
{
    public partial class SocialMediaExtractor : MaterialForm
    {

        public static string finalURL = "";
        public static int pageIndex = 0;
        BackgroundWorker worker;
        public static bool isStopped = true;
        public static int WaitFrom = 0;
        public static int WaitTo = 0;
        public static bool isCaptchaDitected = false;
        public static bool IsLoading = false;
        List<WebBrowser> webBrowserList = new List<WebBrowser>();
        public static bool EnableProxy = false;
        CampaignStatusEnum campaignStatusEnum;
        WaSenderForm waSenderForm;
        struct TooltipInfo
        {
            public readonly string title;
            public readonly ToolTipIcon icon;
            public readonly string message;

            public TooltipInfo(string _title, ToolTipIcon _icon, string _message)
            {
                title = _title;
                icon = _icon;
                message = _message;
            }
        }

        readonly Dictionary<Control, TooltipInfo> m_ttipMessages;

        private WebView2 wvSerp;
        private string serpHtml = "";
        private System.Threading.ManualResetEventSlim serpDone = new System.Threading.ManualResetEventSlim(false);
        private int emptySerpPages = 0;
        private string serpSiteFilter = "";
        private const string SerpUA = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36";

        BalloonToolTip m_ttip;
        public SocialMediaExtractor(WaSenderForm _waSenderForm)
        {
            InitializeComponent();
            try { GridTheme.Apply(dataGridView1); } catch (System.Exception) { }
            this.Icon = Strings.AppIcon;
            waSenderForm = _waSenderForm;
            m_ttipMessages = new Dictionary<Control, TooltipInfo>()
			{
				{ pictureBox1, new TooltipInfo(Strings.KeyWords, ToolTipIcon.Info, Strings.WhatareyouLookingFor +" ." + Environment.NewLine + Strings.ieWebDevelopersWebDesignersect + Environment.NewLine + Strings.Youcanaddmultiplekeywords+" , " + Strings.KeepeachKeywordinonline) },
				{ pictureBox2, new TooltipInfo(Strings.Mobile, ToolTipIcon.Info,  Strings.TograbMobileNumbers + "." + Environment.NewLine + Strings.AddYourCountryCodeonly + Environment.NewLine  + Strings.KeepeachCountrycodeinonline) },
                { pictureBox3, new TooltipInfo(Strings.EmailId, ToolTipIcon.Info, Strings.TograbEmailIds + Environment.NewLine + Strings.Addemailidsourcetotargetlike + Environment.NewLine  + Strings.Keepeachemailinonline) },
			};

            m_ttip = new BalloonToolTip(this);
        }

        private void initBackgroundWorker()
        {
            worker = new BackgroundWorker();
            worker.WorkerReportsProgress = true;
            worker.WorkerSupportsCancellation = true;

            worker.DoWork += new DoWorkEventHandler(worker_DoWork);
            worker.ProgressChanged += new ProgressChangedEventHandler(worker_ProgressChanged);
            worker.RunWorkerCompleted += new RunWorkerCompletedEventHandler(worker_RunWorkerCompleted);

            ChangeCampStatus(CampaignStatusEnum.NotStarted);
        }


        private void ChangeCampStatus(CampaignStatusEnum _campaignStatus)
        {
            this.campaignStatusEnum = _campaignStatus;
            AutomationCommon.ChangeCampStatus(_campaignStatus, label2);
        }
        private void worker_DoWork(object sender, DoWorkEventArgs e)
        {
            while (isStopped)
            {
                if (!IsLoading)
                {
                    if (isCaptchaDitected == false)
                    {
                        NavigateBrowser();
                    }


                    List<DataResultModel> list = Start();


                    pageIndex = pageIndex + 10;
                    if (list.Count() == 0 && isCaptchaDitected == false)
                    {
                        emptySerpPages++;
                        if (emptySerpPages >= 2)
                        {
                            worker.CancelAsync();
                            isStopped = false;
                        }
                    }
                    else
                    {
                        emptySerpPages = 0;
                        dataGridView1.Invoke((MethodInvoker)delegate
                        {
                            foreach (DataResultModel item in list)
                            {
                                var globalCounter = dataGridView1.Rows.Count;
                                dataGridView1.Rows.Add();
                                dataGridView1.Rows[globalCounter].Cells[0].Value = dataGridView1.Rows.Count;
                                dataGridView1.Rows[globalCounter].Cells[1].Value = item.title;
                                dataGridView1.Rows[globalCounter].Cells[2].Value = item.email;
                                dataGridView1.Rows[globalCounter].Cells[3].Value = item.mobileNumber;

                                dataGridView1.FirstDisplayedScrollingRowIndex = dataGridView1.RowCount - 1;
                            }
                        });
                    }

                    var rnd = new Random().Next(WaitFrom * 1000, WaitTo * 1000);
                    Thread.Sleep(rnd);
                }


            }
        }

        private void worker_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {

        }

        private void worker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {

            try
            {
                if (e.Result is Exception)
                {
                    Exception eex = (Exception)e.Result;
                    ChangeCampStatus(CampaignStatusEnum.Error);
                    MessageBox.Show(eex.Message, Strings.Error, MessageBoxButtons.OK, MessageBoxIcon.Error);
                    stopProgressbar();
                    return;
                }
            }
            catch (Exception x)
            {

            }
            stopProgressbar();
            ChangeCampStatus(CampaignStatusEnum.Finish);
        }

        private void startProgressBar()
        {
            progressBar1.Show();
            progressBar1.Style = ProgressBarStyle.Marquee;
            progressBar1.MarqueeAnimationSpeed = 30;
        }
        private void stopProgressbar()
        {
            progressBar1.Hide();
            progressBar1.Style = ProgressBarStyle.Blocks;
            progressBar1.MarqueeAnimationSpeed = 0;
        }


        private string _sanitiseit(string word)
        {
            word = word.Trim().Replace("\r", "").Replace("+", "%2B").Replace(" ", "+");
            return word;
        }


        private async void materialButton1_Click(object sender, EventArgs e)
        {
            if (worker.IsBusy)
            {
                return;
            }


            if ((textBox1.Text == "") || (textBox2.Text == ""))
            {
                MessageBox.Show(Strings.ProvideInputs + Environment.NewLine + "*" + Strings.KeyWords + Environment.NewLine + "*" + Strings.MobileNumber, Strings.Error, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            emptySerpPages = 0;
            try { serpSiteFilter = ((materialComboBox1.SelectedValue as string) ?? "").Replace("https://", "").Replace("http://", "").Trim('/').Trim().ToLowerInvariant(); } catch (System.Exception) { serpSiteFilter = ""; }
            try
            {
                if (!await EnsureSerpBrowserAsync()) return;
            }
            catch (Exception ex)
            {
                try { MessageBox.Show(ex.Message, Strings.Error, MessageBoxButtons.OK, MessageBoxIcon.Error); } catch (Exception) { }
                return;
            }

            WebScraper.SetBrowserFeatureControl();
            webBrowser1.ScriptErrorsSuppressed = true;

            webBrowser1.Navigating += new WebBrowserNavigatingEventHandler(webBrowser1_Navigating);

            string KeyWords = "";

            var splitter = textBox1.Text.Split('\n');

            List<string> newsplitter = new List<string>();
            if (textBox1.Text != "")
            {
                
                foreach (string item in splitter)
                {
                    newsplitter.Add("\""+  _sanitiseit(item ) +"\"");
                }
            }
            


            splitter = textBox2.Text.Split('\n');
            //newsplitter = new List<string>();
            if (textBox2.Text != "")
            {
                foreach (var item in splitter)
                {
                    //if (KeyWords == "")
                    //{
                    //    KeyWords = item.Trim().Replace("+", "%2B").Replace(" ", "+");
                    //}
                    //else
                    //{
                    //    KeyWords = KeyWords + "+AND+" + item.Trim().Replace("+", "%2B").Replace(" ", "+");
                    //}
                    newsplitter.Add("\"" + _sanitiseit(item) + "\"");
                }
            }


            splitter = textBox3.Text.Split('\n');
            if (textBox3.Text != "")
            {
                foreach (var item in splitter)
                {
                    //if (KeyWords == "")
                    //{
                    //    KeyWords = item.Trim().Replace("+", "%2B").Replace(" ", "+");
                    //}
                    //else
                    //{
                    //    KeyWords = KeyWords + "+AND+" + item.Trim().Replace("+", "%2B").Replace(" ", "+");
                    //}

                    newsplitter.Add("\"" + _sanitiseit(item) + "\"");
                }
            }

            if (materialTextBox21.Text != "")
            {
                //if (KeyWords == "")
                //{
                //    KeyWords = materialTextBox21.Text.Trim().Replace("+", "%2B").Replace(" ", "+");
                //}
                //else
                //{
                //    KeyWords = KeyWords + "+AND+" + materialTextBox21.Text.Trim().Replace("+", "%2B").Replace(" ", "+");
                //}

                newsplitter.Add("\"" + _sanitiseit(materialTextBox21.Text) + "\"");
            }


            List<string> dd = newsplitter.ToList();
            KeyWords = String.Join("+AND+", dd); ;

            string siteText = materialComboBox1.SelectedValue != "" ? "+site:" + materialComboBox1.SelectedValue : "";
            finalURL = "https://www.google.com/search?q=" + KeyWords + siteText + "/&ei=kXe_ZIPXBv6RseMPx4m04AM&start=";

            try
            {
                WaitFrom = Convert.ToInt32(materialTextBox23.Text.ToString());
                WaitTo = Convert.ToInt32(materialTextBox24.Text.ToString());
            }
            catch (Exception)
            {

            }
            startProgressBar();
            isStopped = true;
            pageIndex = 0;
            dataGridView1.Rows.Clear();
            IsLoading = false;
            worker.RunWorkerAsync();
            ChangeCampStatus(CampaignStatusEnum.Running);
        }

        private void webBrowser1_Navigating(object sender, WebBrowserNavigatingEventArgs e)
        {
            IsLoading = true;
        }


        private void NavigateBrowserWithProxy()
        {

        }

        void NavigateBrowser()
        {
            IsLoading = true;
            serpHtml = "";
            try
            {
                string url = finalURL + pageIndex.ToString();
                try { serpDone.Reset(); } catch (System.Exception) { }
                try
                {
                    if (wvSerp != null && !wvSerp.IsDisposed)
                    {
                        if (wvSerp.InvokeRequired)
                        {
                            wvSerp.Invoke(new Action(() =>
                            {
                                try { wvSerp.CoreWebView2.Navigate(url); }
                                catch (System.Exception) { try { serpDone.Set(); } catch (System.Exception) { } }
                            }));
                        }
                        else
                        {
                            try { wvSerp.CoreWebView2.Navigate(url); }
                            catch (System.Exception) { try { serpDone.Set(); } catch (System.Exception) { } }
                        }
                    }
                    else { try { serpDone.Set(); } catch (System.Exception) { } }
                }
                catch (System.Exception) { try { serpDone.Set(); } catch (System.Exception) { } }
                for (int w = 0; w < 60; w++)
                {
                    if (!isStopped) break;
                    try { if (serpDone.Wait(500)) break; } catch (System.Exception) { break; }
                }
            }
            catch (System.Exception) { }
            IsLoading = false;
        }

        private async System.Threading.Tasks.Task<bool> EnsureSerpBrowserAsync()
        {
            try
            {
                if (wvSerp == null || wvSerp.IsDisposed)
                {
                    wvSerp = new WebView2();
                    try
                    {
                        var parent = webBrowser1.Parent;
                        int idx = 0;
                        try { idx = parent.Controls.GetChildIndex(webBrowser1); } catch (System.Exception) { }
                        wvSerp.Bounds = webBrowser1.Bounds;
                        try { wvSerp.Anchor = webBrowser1.Anchor; } catch (System.Exception) { }
                        try { wvSerp.Dock = webBrowser1.Dock; } catch (System.Exception) { }
                        parent.Controls.Add(wvSerp);
                        try { parent.Controls.SetChildIndex(wvSerp, idx); } catch (System.Exception) { }
                        webBrowser1.Visible = false;
                    }
                    catch (System.Exception) { }
                    wvSerp.NavigationCompleted += async (s, e) =>
                    {
                        try
                        {
                            bool consentClicked = false;
                            try { consentClicked = await DismissGoogleConsentAsync(); } catch (System.Exception) { }
                            if (consentClicked) return;
                            try { await System.Threading.Tasks.Task.Delay(3500); } catch (System.Exception) { }
                            string raw = "";
                            try { raw = await wvSerp.ExecuteScriptAsync("document.documentElement.outerHTML"); } catch (System.Exception) { }
                            try { serpHtml = UnquoteJson(raw); } catch (System.Exception) { serpHtml = ""; }
                        }
                        catch (System.Exception) { try { serpHtml = ""; } catch (System.Exception) { } }
                        try { serpDone.Set(); } catch (System.Exception) { }
                    };
                    await wvSerp.EnsureCoreWebView2Async();
                }
                return true;
            }
            catch (System.Exception ex)
            {
                try { MessageBox.Show("WebView2 gagal diinisialisasi: " + ex.Message + "\n\nInstall Microsoft Edge WebView2 Runtime.", Strings.Error, MessageBoxButtons.OK, MessageBoxIcon.Warning); } catch (System.Exception) { }
                return false;
            }
        }

        private async System.Threading.Tasks.Task<bool> DismissGoogleConsentAsync()
        {
            try
            {
                if (wvSerp == null || wvSerp.IsDisposed) return false;
                string js = "(function(){try{var u=location.href;var bodyTxt=(document.body&&document.body.innerText)||'';"
                    + "if(u.indexOf('consent.google.')<0&&bodyTxt.indexOf('Before you continue')<0&&bodyTxt.indexOf('Sebelum melanjutkan')<0)return 'NOCONSENT';"
                    + "var btns=document.querySelectorAll('button,input[type=submit],input[type=button],div[role=button]');"
                    + "var rx=/reject all|tolak semua/i;"
                    + "for(var i=0;i<btns.length;i++){var tx=(btns[i].innerText||btns[i].value||'');if(rx.test(tx)){btns[i].click();return 'CLICKED';}}"
                    + "var f=document.querySelector('form');if(f){var b=f.querySelector('button,input[type=submit]');if(b){b.click();return 'CLICKED_FORM';}}"
                    + "return 'NO_MATCH';}catch(e){return 'ERR';}})();";
                string r = "";
                try { r = await wvSerp.ExecuteScriptAsync(js); } catch (System.Exception) { return false; }
                if ((r ?? "").IndexOf("CLICKED", System.StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    try { await System.Threading.Tasks.Task.Delay(3000); } catch (System.Exception) { }
                    return true;
                }
                return false;
            }
            catch (System.Exception) { return false; }
        }

        private static string UnquoteJson(string raw)
        {
            try
            {
                if (string.IsNullOrEmpty(raw)) return "";
                string s = raw.Trim();
                if (s.Length >= 2 && s[0] == '"' && s[s.Length - 1] == '"')
                {
                    try { return Newtonsoft.Json.JsonConvert.DeserializeObject<string>(s) ?? ""; }
                    catch (System.Exception) { }
                }
                return s;
            }
            catch (System.Exception) { return ""; }
        }

        private void MainForm_Load(object sender, EventArgs e)
        {
            Init();
            initLanguages();

            initBackgroundWorker();
            m_ttip.Create();
        }

        private void initLanguages()
        {
            this.Text = Strings.SocialMediaDataExtractor;
            materialLabel3.Text = Strings.KeyWords;
            materialLabel4.Text = Strings.Mobile;
            materialTextBox21.Hint = Strings.LocationOptional;
            materialComboBox1.Hint = Strings.SocialMedicaWebsite;
            materialLabel5.Text = Strings.EmailId;
            materialLabel1.Text = Strings.DelayInSecondsAfterEachPage;
            materialLabel2.Text = Strings.to;
            materialButton1.Text = Strings.Start;
            materialButton2.Text = Strings.Stop;
            label1.Text = Strings.Status;
            materialButton3.Text = Strings.Export;
            linkLabel1.Text = "<< " + Strings.ImportNumberinSender;

            dataGridView1.Columns[0].HeaderText = "#";
            dataGridView1.Columns[1].HeaderText = Strings.Title;
            dataGridView1.Columns[2].HeaderText = Strings.EmailId;
            dataGridView1.Columns[3].HeaderText = Strings.MobileNumber;
        }

        private void Init()
        {
            Dictionary<string, string> test = new Dictionary<string, string>();
            test.Add("", "(" + Strings.DefaultGoogleSearch + ")");
            test.Add("linkedin.com", "linkedin");
            test.Add("facebook.com", "facebook");
            test.Add("instagram.com/", "instagram");
            test.Add("https://t.me/", "telegram");
            test.Add("https://tiktok.com/", "tiktok");
            test.Add("https://youtube.com/", "youtube");
            materialComboBox1.DataSource = new BindingSource(test, null);
            materialComboBox1.DisplayMember = "Value";
            materialComboBox1.ValueMember = "Key";

            textBox3.Text = "@gmail.com" + Environment.NewLine;
            textBox3.Text += "@yahoo.com";

        }


        private  void webBrowser1_DocumentCompleted(object sender, WebBrowserDocumentCompletedEventArgs e)
        {
            //Thread.Sleep(1000);
            //await Task.Delay(1000);
            Start();
            IsLoading = false;
        }


        private bool IsCaptha()
        {
            try
            {
                List<HtmlElement> _div = WebScraper.GetHTMLElementsCollectionBySimilarId(webBrowser1, "div", "recaptcha");
                if (_div.Count() > 0)
                {
                    return true;
                }
            }
            catch (Exception)
            {

                return false;
            }
            return false;
        }

        bool CaptchaAlertMessageShown = false;
        private List<DataResultModel> Start()
        {
            List<DataResultModel> list = new List<DataResultModel>();
            try
            {
                string html = serpHtml ?? "";
                if (string.IsNullOrEmpty(html)) return list;
                if (html.IndexOf("recaptcha", System.StringComparison.OrdinalIgnoreCase) >= 0
                    || html.IndexOf("/sorry/", System.StringComparison.OrdinalIgnoreCase) >= 0
                    || html.IndexOf("unusual traffic", System.StringComparison.OrdinalIgnoreCase) >= 0)
                {
                        if (isCaptchaDitected == false)
                        {
                            if (CaptchaAlertMessageShown == false)
                            {
                                CaptchaAlertMessageShown = true;
                                using (var soundPlayer = new SoundPlayer(@"beep-beep-6151.wav"))
                                {
                                    soundPlayer.Play();
                                }
                                Thread.Sleep(1000);
                                using (var soundPlayer = new SoundPlayer(@"beep-beep-6151.wav"))
                                {
                                    soundPlayer.Play();
                                }
                                MessageBox.Show(Strings.Pleasefillthecaptcha, Strings.Error, MessageBoxButtons.OK, MessageBoxIcon.Error);
                                CaptchaAlertMessageShown = false;
                            }


                        }
                        isCaptchaDitected = true;
                    return list;
                }
                isCaptchaDitected = false;

                HtmlAgilityPack.HtmlDocument hapDoc = new HtmlAgilityPack.HtmlDocument();
                hapDoc.LoadHtml(html);
                var anchors = hapDoc.DocumentNode.SelectNodes("//a[@href]");
                if (anchors == null) return list;
                string siteFilter = serpSiteFilter ?? "";
                System.Collections.Generic.HashSet<string> seenLinks = new System.Collections.Generic.HashSet<string>(System.StringComparer.OrdinalIgnoreCase);
                foreach (HtmlAgilityPack.HtmlNode anc in anchors)
                {
                    try
                    {
                        if (!isStopped) break;
                        string href = "";
                        try { href = (anc.GetAttributeValue("href", "") ?? "").Trim(); } catch (System.Exception) { continue; }
                        if (string.IsNullOrEmpty(href)) continue;
                        if (href.StartsWith("#") || href.StartsWith("javascript:", System.StringComparison.OrdinalIgnoreCase)) continue;
                        string url = href;
                        if (url.StartsWith("/url?", System.StringComparison.OrdinalIgnoreCase))
                        {
                            var mq = System.Text.RegularExpressions.Regex.Match(url, @"[?&]q=([^&]+)");
                            if (!mq.Success) continue;
                            try { url = System.Uri.UnescapeDataString(mq.Groups[1].Value); } catch (System.Exception) { continue; }
                        }
                        else if (url.StartsWith("/")) continue;
                        else if (!url.StartsWith("http", System.StringComparison.OrdinalIgnoreCase)) continue;
                        string host = "";
                        try { host = new System.Uri(url).Host.ToLowerInvariant(); } catch (System.Exception) { continue; }
                        if (host.Contains("google.")) continue;
                        if (!string.IsNullOrEmpty(siteFilter) && !host.Contains(siteFilter)) continue;
                        if (!seenLinks.Add(url)) continue;
                        DataResultModel Listitem = new DataResultModel();
                        string rTitle = "";
                        try { rTitle = System.Net.WebUtility.HtmlDecode(anc.InnerText ?? ""); } catch (System.Exception) { }
                        try { rTitle = System.Text.RegularExpressions.Regex.Replace(rTitle ?? "", @"\s+", " ").Trim(); } catch (System.Exception) { }
                        if (rTitle.Length > 200) rTitle = rTitle.Substring(0, 200);
                        if (string.IsNullOrEmpty(rTitle)) rTitle = host;
                        Listitem.title = rTitle;
                        string rAddr = "";
                        try
                        {
                            var par = anc.ParentNode;
                            if (par != null) rAddr = System.Net.WebUtility.HtmlDecode(par.InnerText ?? "");
                            rAddr = System.Text.RegularExpressions.Regex.Replace(rAddr ?? "", @"\s+", " ").Trim();
                            if (rAddr.Length > 400) rAddr = rAddr.Substring(0, 400);
                        }
                        catch (System.Exception) { }
                        Listitem.address = (rAddr ?? "").Replace(";", "-").Replace(",", "-").Replace("-", "")
                            .Replace("(", "").Replace(")", "").Replace(" ...", ".")
                            .Replace("Tel: ", "Tel: +").Replace("Tel.: ", "Tel.: +").Replace("call ", "call +").Replace("++", "+");
                        Listitem.link = url;
                            Match match = new Regex("(?:\\+)[0-9\\s.\\/-]{10,17}").Match(Listitem.title + Listitem.address);

                            if (match.Success && match.Value.Count(char.IsDigit) > 7)
                            {
                                Listitem.mobileNumber = match.Value.Replace("/", "").Replace(" ", "").Replace(".", "")
                                    .Replace("-", "");
                            }

                            match = new Regex("(?:[A-Za-z0-9!#$%&'*+/=?^_`{|}~-]+(?:\\.[A-Za-z0-9!#$%&'*+/=?^_`{|}~-]+)*|\"(?:[\\x01-\\x08\\x0b\\x0c\\x0e-\\x1f\\x21\\x23-\\x5b\\x5d-\\x7f]|\\\\[\\x01-\\x09\\x0b\\x0c\\x0e-\\x7f])*\")@(?:(?:[a-z0-9](?:[a-z0-9-]*[a-z0-9])?\\.)+[a-z0-9](?:[A-Za-z0-9-]*[a-z0-9])?|\\[(?:(?:25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)\\.){3}(?:25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?|[a-z0-9-]*[a-z0-9]:(?:[\\x01-\\x08\\x0b\\x0c\\x0e-\\x1f\\x21-\\x5a\\x53-\\x7f]|\\\\[\\x01-\\x09\\x0b\\x0c\\x0e-\\x7f])+)\\])").Match(Listitem.title + Listitem.address);
                            if (match.Success)
                            {
                                Listitem.email = match.Value.Replace("\\","");
                            }
                            else
                            {
                                Listitem.email = EmailMiner.GetEmail(Listitem.link, new string[1] { "conta" });
                            }
                            if ((Listitem.email != null && Listitem.email != "") || (Listitem.mobileNumber != null && Listitem.mobileNumber != ""))
                            {
                                list.Add(Listitem);
                            }
                    }
                    catch (System.Exception) { }
                }
            }
            catch (System.Exception ex)
            {

            }
            return list;
        }

        private void materialButton2_Click(object sender, EventArgs e)
        {
            isStopped = false;
            stopProgressbar();
            worker.CancelAsync();
            ChangeCampStatus(CampaignStatusEnum.Stopped);
        }

        private void pictureBox1_MouseHover(object sender, EventArgs e)
        {
            setToolTip(sender);
        }

        private void pictureBox1_MouseLeave(object sender, EventArgs e)
        {
            m_ttip.Hide();
        }

        private void pictureBox2_MouseHover(object sender, EventArgs e)
        {
            setToolTip(sender);
        }

        private void setToolTip(object sender)
        {
            TooltipInfo tti = m_ttipMessages[sender as Control];

            m_ttip.strTitle = tti.title;
            m_ttip.icon = tti.icon;
            m_ttip.strText = tti.message;

            Point ptWhere = this.PointToClient(Cursor.Position);
            ptWhere.X++;
            ptWhere.Y++;
            m_ttip.Show(ptWhere);
        }

        private void pictureBox2_MouseLeave(object sender, EventArgs e)
        {
            m_ttip.Hide();
        }

        private void pictureBox3_MouseHover(object sender, EventArgs e)
        {
            setToolTip(sender);
        }

        private void pictureBox3_MouseLeave(object sender, EventArgs e)
        {
            m_ttip.Hide();
        }

        private void materialButton3_Click(object sender, EventArgs e)
        {
            String FolderPath = Config.GetTempFolderPath();
            String file = Path.Combine(FolderPath, "Number_Filter_" + Guid.NewGuid().ToString() + ".xlsx");
            string NewFileName = file.ToString();
            File.Copy("MemberListTemplate.xlsx", NewFileName, true);
            var newFile = new FileInfo(NewFileName);
            ExcelPackage.LicenseContext = OfficeOpenXml.LicenseContext.NonCommercial;

            using (ExcelPackage xlPackage = new ExcelPackage(newFile))
            {
                var ws = xlPackage.Workbook.Worksheets[0];
                ws.Cells[1, 1].Value = "#";
                ws.Cells[1, 2].Value = Strings.Title;
                ws.Cells[1, 3].Value = Strings.EmailId;
                ws.Cells[1, 4].Value = Strings.Mobile;

                int j = 0;
                foreach (DataGridViewRow row in dataGridView1.Rows)
                {
                    if (row.IsNewRow) continue;
                    ws.Cells[j + 2, 1].Value = row.Cells[0].Value != null ? row.Cells[0].Value.ToString() : "";
                    ws.Cells[j + 2, 2].Value = row.Cells[1].Value != null ? row.Cells[1].Value.ToString() : "";
                    ws.Cells[j + 2, 3].Value = row.Cells[2].Value != null ? row.Cells[2].Value.ToString() : "";
                    ws.Cells[j + 2, 4].Value = row.Cells[3].Value != null ? row.Cells[3].Value.ToString() : "";
                    j++;
                }
                xlPackage.Save();
            }

            savesampleExceldialog.FileName = "SocialMediaData.xlsx";
            savesampleExceldialog.Filter = "Excel Files (*.xlsx)|*.xlsx";
            if (savesampleExceldialog.ShowDialog() == DialogResult.OK)
            {
                File.Copy(NewFileName, savesampleExceldialog.FileName.EndsWith(".xlsx") ? savesampleExceldialog.FileName : savesampleExceldialog.FileName + ".xlsx", true);
                Utils.showAlert(Strings.Filedownloadedsuccessfully, WASender.Alerts.Alert.enmType.Success);
            }
        }

        private void SocialMediaExtractor_FormClosing(object sender, FormClosingEventArgs e)
        {
            this.waSenderForm.formReturn(true);
        }

        private void linkLabel1_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            List<string> number = new List<string>();
            foreach (DataGridViewRow row in dataGridView1.Rows)
            {
                string _number = row.Cells[3].Value != null ? row.Cells[3].Value.ToString() : "";
                _number = _number.Replace(" ", "").Replace("+", "").Replace("-", "");
                if (_number != "")
                {
                    number.Add(_number);
                }
                
            }
            this.Close();
            this.waSenderForm.ImportNumbers(number);
        }


    }
}

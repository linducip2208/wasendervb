using HtmlAgilityPack;
using Microsoft.Web.WebView2.Core;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;
using WaAutoReplyBot;
using WASender.Model;

namespace WASender
{
    public partial class Browser : MyMaterialPopOp
    {
        /// <summary>Expose WebView2 untuk scraper baru (ganti Selenium).</summary>
        public Microsoft.Web.WebView2.WinForms.WebView2 View { get { return webView; } }

        public event EventHandler browserInitDoneEvnet;
        public event EventHandler browserCloseEvent;
        public event EventHandler browserWAPI_InjectedEvent;
        public event EventHandler browserWAPI_do_Counter_increment;
        //public event EventHandler browser_do_add_new_data_row;
        public event Action<Browser, _DataItem_GoogleMap> on_DataItem_GoogleMap;


        public string url = "";
        public int scollDelay = 4;
        public bool isGmap = false;
        bool isWPPInjected = false;

        List<_DataItem_GoogleMap> _DataItem_GoogleMapList;



        protected virtual void do_add_new_data_row(_DataItem_GoogleMap e)
        {
            if (on_DataItem_GoogleMap != null)
            {
                on_DataItem_GoogleMap.Invoke(this, e);
            }
        }

        protected virtual void OnSearch_Completed(EventArgs e)
        {
            if (browserWAPI_do_Counter_increment != null)
            {
                browserWAPI_do_Counter_increment.Invoke(this, e);
            }
        }

        protected virtual void OnWAPI_Injected(EventArgs e)
        {
            if (browserWAPI_InjectedEvent != null)
            {
                browserWAPI_InjectedEvent.Invoke(this, e);
            }
        }

        protected virtual void OnbroserClose(EventArgs e)
        {
            if (browserCloseEvent != null)
            {
                browserCloseEvent.Invoke(this, e);
            }
        }
        protected virtual void OnbroserInitDoneEvnet(EventArgs e)
        {
            //CloseButtonClicked?.Invoke(this, e);
            if (browserInitDoneEvnet != null)
                browserInitDoneEvnet.Invoke(this, e);
        }

        string[] protocols = {
            "http:",
            "https:",
            "ftp:",  
            "sftp:",
            "scp:",
            "edge:",
            "javascript:",
            "chrome-search:",
            "data:",
            "about:",
            "file:"
        };
        // The URL of the home page.
        const string home = "https://ntp.msn.com/edge/ntp?locale=en&dsp=0&sp=Google";
        // Prefix for searching on Google.
        const string googlePrefix = "https://www.google.com/search?q=";
        // The default home page prompt.
        const string prompt = "Where do you want to go today?";
        // JavaScript for going back and forward in the history.
        const string goBack = "window.history.back();";
        const string goForward = "window.history.forward();";
        // JavaScript for reloading the page.
        const string refreshScript = "location.reload();";
        // A list of all loaded userscripts.
        List<string> userscripts = new List<string>();

        public Browser()
        {
            InitializeComponent();
            this.Icon = Strings.AppIcon;

            webView.NavigationStarting += UrlCheck;
            // Add listener for NavigationComplete to load userscripts
            webView.NavigationCompleted += RunUserscripts;
            // And to get the title
            webView.NavigationCompleted += NavigationComplete;
            webView.NavigationStarting += webView_NavigationStarting;
        }


        private void webView_NavigationStarting(object sender, Microsoft.Web.WebView2.Core.CoreWebView2NavigationStartingEventArgs e)
        {
            try { GmapDebugLog("NavigationStarting: " + e.Uri); } catch (Exception) { }
            string _ur = e.Uri.ToString();
            addressBar.Text = _ur;
        }

        public static void GmapDebugLog(string msg)
        {
            try
            {
                string dir = Config.GetProcessLoggerFolderPath();
                File.AppendAllText(Path.Combine(dir, "gmap_browser.log"),
                    DateTime.Now.ToString("HH:mm:ss") + " " + msg + Environment.NewLine);
            }
            catch (Exception)
            {
            }
        }

        private async void NavigationComplete(object sender, Microsoft.Web.WebView2.Core.CoreWebView2NavigationCompletedEventArgs e)
        {
            try { GmapDebugLog("NavigationCompleted success=" + e.IsSuccess + " id=" + e.NavigationId); } catch (Exception) { }
            try
            {
                await webView.ExecuteScriptAsync("window.chrome.webview.postMessage(\"title=\" + window.document.title);");
                if (this.isGmap)
                {
                    await injectWPP();
                }
            }
            catch (Exception ex)
            {
                try { GmapDebugLog("NavigationComplete error: " + ex.Message); } catch (Exception) { }
            }
            finally
            {
                // Init harus selalu selesai saat halaman selesai dimuat,
                // kalau tidak user stuck di "wajib inisiasi dulu" selamanya.
                try { GmapDebugLog("OnWAPI_Injected fired"); } catch (Exception) { }
                OnWAPI_Injected(e);
            }
        }

        private async Task<bool> injectWPP()
        {
            try
            {
                if (isWPPInjected == false)
                {
                    await Task.Delay(1000);
                    string scritp = getWPP();
                    if (!string.IsNullOrEmpty(scritp))
                    {
                        // Batasi ukuran script yg di-inject agar ExecuteScriptAsync tidak gagal
                        if (scritp.Length > 150000)
                        {
                            scritp = "";
                        }
                        if (!string.IsNullOrEmpty(scritp))
                        {
                            await webView.ExecuteScriptAsync(scritp);
                        }
                    }
                    try
                    {
                        webView.CoreWebView2.WebMessageReceived += ReceiveLoginData;
                    }
                    catch (Exception)
                    {
                    }
                    isWPPInjected = true;
                }
                _DataItem_GoogleMapList = new List<_DataItem_GoogleMap>();
                await Task.Delay(500);
            }
            catch (Exception)
            {
            }
            return true;
        }

        private string getWPP()
        {
            try
            {
                // Untuk Google Maps pakai wapi_gmaps.js (hook XHR + tulis #fb-group-data),
                // BUKAN wapi.js (itu script WhatsApp).
                string localGmap = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "wapi_gmaps.js");
                if (File.Exists(localGmap))
                {
                    return File.ReadAllText(localGmap);
                }
                string sysGmap = Path.Combine(Config.GetSysFolderPath(), "wapi_gmaps.js");
                if (File.Exists(sysGmap))
                {
                    return File.ReadAllText(sysGmap);
                }
                return File.ReadAllText(Config.GetWAPIFolferPath());
            }
            catch (Exception)
            {
                return null;
            }
        }


        private string sanitizeNumber(string MobileNumber)
        {
            if (string.IsNullOrEmpty(MobileNumber))
            {
                return "";
            }
            if (MobileNumber.StartsWith("0"))
            {
                MobileNumber = MobileNumber.Substring(1);
            }

            MobileNumber = MobileNumber.Replace(@" ", "");
            MobileNumber = MobileNumber.Replace(@"(", "");
            MobileNumber = MobileNumber.Replace(@")", "");
            MobileNumber = MobileNumber.Replace(@"+", "");
            MobileNumber = MobileNumber.Replace(@"-", "");
            return MobileNumber;
        }
        void ReceiveLoginData(object sender, CoreWebView2WebMessageReceivedEventArgs args)
        {
            try
            {
                string loginDataAsJson = args.WebMessageAsJson;
                if (string.IsNullOrEmpty(loginDataAsJson))
                {
                    return;
                }
                // Abaikan pesan noise ("title=...", "uri=...") yg bukan data maps
                if (!loginDataAsJson.Contains("Business_name"))
                {
                    return;
                }
                _DataItem_GoogleMap data = JsonConvert.DeserializeObject<_DataItem_GoogleMap>(loginDataAsJson);
                if (data == null)
                {
                    return;
                }
                data.Phone = sanitizeNumber(data.Phone ?? "");
                if (_DataItem_GoogleMapList == null)
                {
                    _DataItem_GoogleMapList = new List<_DataItem_GoogleMap>();
                }
                // Hindari dobel (hook bisa mengirim item yg sama berulang)
                bool dup = _DataItem_GoogleMapList.Any(x => x.Business_name == data.Business_name && x.Phone == data.Phone);
                if (dup)
                {
                    return;
                }
                _DataItem_GoogleMapList.Add(data);
                //this.newUI.addtoRow(data);
                var newargs = new EventArgs();


                //browser_do_add_new_(this,browser_do_add_new_data_row);
                do_add_new_data_row(data);
            }
            catch (Exception)
            {
                // Jangan crash gara2 1 pesan tak dikenal
            }
        }


        private async void RunUserscripts(object sender, Microsoft.Web.WebView2.Core.CoreWebView2NavigationCompletedEventArgs e)
        {
            foreach (string script in userscripts)
            {
                // Execute it
                await webView.ExecuteScriptAsync(script);
            }
        }

        public void stop()
        {
            isEnd = true;
        }

        private void UrlCheck(object sender, Microsoft.Web.WebView2.Core.CoreWebView2NavigationStartingEventArgs e)
        {
            // Get the URL
            string uri = e.Uri;
            // If it's a secure URL (starts with https or sftp) or a settings page (edge or sv):
            if (uri.StartsWith("https:") || uri.StartsWith("sftp:") ||
                uri.StartsWith("edge:") || addressBar.Text.StartsWith("sv:"))
            {
                // Set the padlock icon to a padlock
                //padlock.Text = "";
                // Color it green
                //padlock.Foreground = Brushes.LightGreen;
                // Set its tooltip to "Secure"
                //padlock.ToolTip = "Secure";
                // Else:
            }
            else
            {
                // Set the padlock icon to a warning shield
                //padlock.Text = "";
                // Color it red
                //padlock.Foreground = Brushes.Red;
                // Set its tooltip to "Not secure"
                //padlock.ToolTip = "Not secure";
            }
        }
        private void Browser_Load(object sender, System.EventArgs e)
        {
            initLanguages();

            InitializeAsync();
        }

        private void initLanguages()
        {
            this.Text = Strings.Browser;
        }

        async void InitializeAsync()
        {
            webView.Visible = false;
            materialLabel1.Visible = true;
            materialLabel1.Text = Strings.Loading;
            try { GmapDebugLog("InitializeAsync start, url=" + url); } catch (Exception) { }
            webView.CreationProperties = new Microsoft.Web.WebView2.WinForms.CoreWebView2CreationProperties();
            webView.CreationProperties.UserDataFolder = Config.GetProfilesFolderPath() + "\\" + "PrivateBrowserProfile";

            // Initialize the WebView
            try
            {
                await webView.EnsureCoreWebView2Async(null);
                try { GmapDebugLog("EnsureCoreWebView2 OK"); } catch (Exception) { }
            }
            catch (Exception ex)
            {
                try { GmapDebugLog("EnsureCoreWebView2 FAIL: " + ex.Message); } catch (Exception) { }
                throw;
            }
            webView.Visible = true;
            materialLabel1.Visible = false;
            // Add listeners to the WebMessageReceived event
            webView.CoreWebView2.WebMessageReceived += UpdateAddressBar;
            webView.CoreWebView2.WebMessageReceived += ReadPayload;
            // Add a script to run when a page is loading
            await webView.CoreWebView2.AddScriptToExecuteOnDocumentCreatedAsync(
                // This script just posts a message with the window's URL
                "window.chrome.webview.postMessage(\"uri=\" + window.document.URL);"
            );
            // If there is no userscript folder, create one
            //if (!Directory.Exists("userscripts")) Directory.CreateDirectory("userscripts");
            //// For each userscript:
            //foreach (string userscript in Directory.GetFiles("userscripts", "*.js"))
            //{
            //    // Read its content
            //    var usContent = File.ReadAllText(userscript);
            //    // Add it to the userscript list
            //    userscripts.Add(usContent);
            //}

            string nav = url == "" ? "https://www.google.com/search?q=whatsapp+group+links&oq=whatsapp+group+links&aqs=chrome.0.69i59j0i433i512j0i512j0i457i512j0i402j69i60l3.2696j0j7&sourceid=chrome&ie=UTF-8" : url;

            webView.CoreWebView2.Navigate(nav);

            OnbroserInitDoneEvnet(null);
        }

        private void ReadPayload(object sender, Microsoft.Web.WebView2.Core.CoreWebView2WebMessageReceivedEventArgs e)
        {
            // Get the received message
            //string uri = e.TryGetWebMessageAsString();
            //// If the URI isn't a config page, return
            //if (!uri.Contains(Directory.GetCurrentDirectory().Replace('\\', '/') + "/config/")) return;
            //// If the message isn't a payload, return
            //if (!uri.StartsWith(":svpl:")) return;
            //// Remove header
            //uri = uri.Replace(":svpl:", "");
            //MessageBox.Show(uri);
        }

        bool isEnd = false;
        public async void start(string searchQuery)
        {
            isEnd = false;
            try
            {
                // Google Maps 2025+: id "searchboxinput" sudah tidak ada.
                // Pakai fallback berlapis: id lama -> input[name=q] -> input pertama di form.
                string safeQuery = (searchQuery ?? "").Replace("\\", "\\\\").Replace("'", "\\'").Replace("\r", " ").Replace("\n", " ");
                string setValueJs = "(function(){var q='" + safeQuery + "';"
                    + "var input=document.getElementById('searchboxinput')||document.querySelector('input[name=q]')||document.querySelector('form input');"
                    + "if(!input)return 'NO_INPUT';"
                    + "input.focus();"
                    + "try{var setter=Object.getOwnPropertyDescriptor(window.HTMLInputElement.prototype,'value').set;setter.call(input,q);}catch(e){input.value=q;}"
                    + "input.dispatchEvent(new Event('input',{bubbles:true}));"
                    + "input.dispatchEvent(new Event('change',{bubbles:true}));"
                    + "return 'SET:'+(input.value||'').substring(0,80);})();";
                await webView.ExecuteScriptAsync(setValueJs);
                await Task.Delay(2500);
                // Tombol search (.mL3xi) masih ada; klik 2x seperti sebelumnya.
                // Kalau tidak ada, submit form sebagai fallback.
                string clickJs = "(function(){"
                    + "var b=document.getElementsByClassName('mL3xi');"
                    + "if(b&&b.length){b[0].click();return 'CLICKED';}"
                    + "var f=document.querySelector('form');"
                    + "if(f){try{f.requestSubmit();return 'SUBMITTED';}catch(e){f.submit();return 'SUBMITTED';}}"
                    + "return 'NO_BUTTON';})();";
                await webView.ExecuteScriptAsync(clickJs);
                await Task.Delay(5000);
                await webView.ExecuteScriptAsync(clickJs);
                await Task.Delay(2000);
            }
            catch (Exception ex)
            {

            }
            perfotmScroller();
        }


        private static string ScrollContainerJs(string action)
        {
            // Selector modern yg stabil: div[role=feed]. Fallback ke selector lama.
            // PENTING: ExecuteScriptAsync hanya mengembalikan nilai dari top-level "return".
            return "return (function(){var feed=document.querySelector('div[role=feed]')"
                + "||document.querySelectorAll('.m6QErb.DxyBCb.kA9KIf')[1]"
                + "||document.querySelectorAll('.m6QErb.DxyBCb.kA9KIf')[0];"
                + "if(!feed)return 'NO_FEED';"
                + (action == "read" ? "return ''+feed.scrollTop;" : "feed.scrollTop=1000000;return 'SCROLLED';")
                + "})();";
        }

        private static string ScrollStepJs()
        {
            // Langkah kecil + event scroll: meniru scroll manusia agar lazy-load/XHR ke-hook.
            // Sekali lompat ke bawah sering diabaikan feed — makanya dulu harus scroll manual.
            return "(function(){try{var feed=document.querySelector('div[role=feed]')"
                + "||document.querySelectorAll('.m6QErb.DxyBCb.kA9KIf')[1]"
                + "||document.querySelectorAll('.m6QErb.DxyBCb.kA9KIf')[0];"
                + "if(!feed)return;"
                + "feed.scrollTop=feed.scrollTop+1500;"
                + "try{feed.dispatchEvent(new Event('scroll',{bubbles:true}));}catch(x){}"
                + "try{window.scrollBy(0,1500);}catch(x){}"
                + "}catch(e){}})();";
        }

        private async void perfotmScroller()
        {
            int scrollTopValue = 0;
            int _oldScrolltppValue = 0;
            int _sameCOunter = 0;
            int _noNewRounds = 0; // ronde tanpa data baru: rem utama anti loading-terus
            try
            {
                while (isEnd == false)
                {
                    // Scroll bertahap (bukan sekali lompat) + panen tiap putaran,
                    // agar data muncul otomatis tanpa scroll manual.
                    for (int s = 0; s < 6 && isEnd == false; s++)
                    {
                        try { await webView.ExecuteScriptAsync(ScrollStepJs()); } catch (Exception) { }
                        await Task.Delay(800);
                    }
                    await Task.Delay((scollDelay == 0 ? 4 : scollDelay) * 1000);

                    int added = 0;
                    try { added = await harvestGmapResults(); } catch (Exception) { }
                    if (added > 0) { _noNewRounds = 0; _sameCOunter = 0; }
                    else { _noNewRounds++; }
                    if (_noNewRounds >= 6)
                    {
                        // 6 putaran tanpa data baru = halaman habis / hook macet: berhenti.
                        try { GmapDebugLog("perfotmScroller: stop, 6 ronde tanpa data baru"); } catch (Exception) { }
                        isEnd = true;
                        break;
                    }

                    string _scrollTopValue = await webView.ExecuteScriptAsync(ScrollContainerJs("read"));
                    string cleaned = (_scrollTopValue ?? "").Trim().Trim('"');
                    int parsed;
                    if (cleaned == "NO_FEED")
                    {
                        _sameCOunter = _sameCOunter + 1;
                    }
                    else if (int.TryParse(cleaned, out parsed))
                    {
                        _oldScrolltppValue = scrollTopValue;
                        scrollTopValue = parsed;
                        if (scrollTopValue == _oldScrolltppValue)
                        {
                            _sameCOunter = _sameCOunter + 1;
                        }
                        else
                        {
                            _sameCOunter = 0;
                        }
                    }
                    if (_sameCOunter > 7)
                    {
                        isEnd = true;
                    }
                }
            }
            catch (Exception ex)
            {

            }

            await harvestGmapResults();

            OnSearch_Completed(null);
            //this.newUI.doCounerIncremet();

        }

        // Ambil hasil yg dikumpulkan wapi_gmaps.js (XHR hook) di input #fb-group-data,
        // terus teruskan ke grid via do_add_new_data_row. Return: jumlah data BARU.
        private async Task<int> harvestGmapResults()
        {
            int added = 0;
            try
            {
                string raw = await webView.ExecuteScriptAsync(
                    "return (function(){var el=document.getElementById('fb-group-data');if(!el||!el.value)return '[]';return el.value;})();");
                if (string.IsNullOrEmpty(raw))
                {
                    return 0;
                }
                // ExecuteScriptAsync mengembalikan string JSON yg di-escape
                string json = System.Text.RegularExpressions.Regex.Unescape(raw.Trim().Trim('"'));
                // Kadang double-encoded
                if (json.StartsWith("\"") && json.EndsWith("\""))
                {
                    json = System.Text.RegularExpressions.Regex.Unescape(json.Trim('"'));
                }
                var list = JsonConvert.DeserializeObject<List<Dictionary<string, object>>>(json);
                if (list == null)
                {
                    return added;
                }
                if (_DataItem_GoogleMapList == null)
                {
                    _DataItem_GoogleMapList = new List<_DataItem_GoogleMap>();
                }
                foreach (var row in list)
                {
                    try
                    {
                        Func<string, string> get = (key) =>
                        {
                            object v;
                            if (row.TryGetValue(key, out v) && v != null)
                            {
                                return v.ToString();
                            }
                            return "";
                        };
                        var data = new _DataItem_GoogleMap();
                        data.Business_name = get("Business_name");
                        data.Website_url = get("Website_url");
                        data.Phone = sanitizeNumber(get("Phone"));
                        data.Review = get("Review");
                        data.Rating = get("Rating");
                        data.Address = get("Address");
                        data.Category = get("Category");
                        // script memakai huruf kecil: city/state
                        data.City = get("City");
                        if (string.IsNullOrEmpty(data.City)) data.City = get("city");
                        data.State = get("State");
                        if (string.IsNullOrEmpty(data.State)) data.State = get("state");
                        data.Lat = get("Lat");
                        data.Long = get("Long");
                        data.Working_hour = get("Working_hour");
                        data.image1 = get("Image1");
                        if (string.IsNullOrEmpty(data.Business_name) && string.IsNullOrEmpty(data.Phone))
                        {
                            continue;
                        }
                        // Hindari duplikat (hook XHR bisa menulis ulang data yg sama)
                        bool dup = _DataItem_GoogleMapList.Any(x => x.Business_name == data.Business_name && x.Phone == data.Phone);
                        if (dup)
                        {
                            continue;
                        }
                        _DataItem_GoogleMapList.Add(data);
                        do_add_new_data_row(data);
                        added++;
                    }
                    catch (Exception)
                    {
                    }
                }
            }
            catch (Exception)
            {
            }
            return added;
        }

        private void UpdateAddressBar(object sender, Microsoft.Web.WebView2.Core.CoreWebView2WebMessageReceivedEventArgs e)
        {
            try
            {
                // Get the received message (the URI of the current page)
                string msg = e.TryGetWebMessageAsString();
                // If the message is a payload, return
                if (msg.StartsWith(":svpl:")) return;
                // If it's a URI, get the URI
                if (msg.StartsWith("url="))
                {
                    string uri = msg.Replace("url=", "");
                    // If the URI isn't a config page:
                    if (!uri.Contains(Directory.GetCurrentDirectory().Replace('\\', '/') + "/config/"))
                    {
                        // If the URI isn't the homepage, set the address bar's text to it
                        if (uri != home) addressBar.Text = uri;
                        // Else, set it to the browsing prompt
                        else addressBar.Text = prompt;
                    }
                }
                // If it's a title:
                if (msg.StartsWith("title="))
                {
                    // If there is a title sent:
                    //if (msg != "title=")
                    // Update the window title
                    //Title = msg.Replace("title=", "") + " - SurfView";
                }
            }
            catch (Exception ex)
            {
                string ee= e.Source;
                //if (IsValidURL(ee))
                    addressBar.Text = ee;
            }
        }

        bool IsValidURL(string URL)
        {
            string Pattern = @"^(?:http(s)?:\/\/)?[\w.-]+(?:\.[\w\.-]+)+[\w\-\._~:/?#[\]@!\$&'\(\)\*\+,;=.]+$";
            Regex Rgx = new Regex(Pattern, RegexOptions.Compiled | RegexOptions.IgnoreCase);
            return Rgx.IsMatch(URL);
        }

        bool hasProtocol(string url)
        {
            // For each protocol:
            foreach (string p in protocols)
            {
                // If the URL starts with it, return true
                if (url.StartsWith(p)) return true;
            }
            // Return false if the URL doesn't start with any protocol
            return false;
        }
        // Is a given string a URL?
        bool isUrl(string url)
        {
            // Create a regex that matches a URL
            Regex reg = new Regex(@"^(^|\s)((https?:\/\/)?[\w-]+(\.[\w-]+)+\.?(:\d+)?(\/\S*)?)$");
            // Return if the given string matches it (so the string is a URL)
            return reg.IsMatch(url);
        }

        private void addressBar_KeyDown(object sender, KeyEventArgs e)
        {
            // If the user pressed Enter:
            if (e.KeyCode == Keys.Enter)
            {
                // If the WebView is initialized (just for safety):
                if (webView != null && webView.CoreWebView2 != null)
                {
                    addressBar.Text = addressBar.Text.Replace("‭", "");
                    // If the address bar's text has a supported protocol:
                    if (hasProtocol(addressBar.Text))
                    {
                        // Navigate to the text
                        webView.CoreWebView2.Navigate(addressBar.Text);
                        // Else, if the address bar's text is a URL:
                    }
                    else if (isUrl(addressBar.Text))
                    {
                        // Navigate to "http://" + the address bar's text
                        // Secure sites should redirect us to the https version
                        webView.CoreWebView2.Navigate("http://" + addressBar.Text);
                        // Else, if it's a config page (the text contains the sv: protocol)
                    }
                    else if (addressBar.Text.StartsWith("sv:"))
                    {
                        // Navigate to the correct config page
                        webView.CoreWebView2.Navigate(Directory.GetCurrentDirectory() + "/config/" + addressBar.Text.Replace("sv:", "") + ".html");
                        // Else:
                    }
                    else
                    {
                        // Search the address bar's text with Google, replacing the spaces with plus signs and removing the LRO
                        webView.CoreWebView2.Navigate(googlePrefix + addressBar.Text.Replace(' ', '+'));
                    }
                }
            }
        }

        private void back_Click(object sender, EventArgs e)
        {
            webView.CoreWebView2.ExecuteScriptAsync(goBack);
        }

        private void next_Click(object sender, EventArgs e)
        {
            webView.CoreWebView2.ExecuteScriptAsync(goForward);
        }

        private void refresh_Click(object sender, EventArgs e)
        {
            webView.CoreWebView2.ExecuteScriptAsync(refreshScript);
        }

        List<string> chatNames;
        public async Task<List<string>> getAllLinks()
        {
            string allHtml = await webView.ExecuteScriptAsync("document.documentElement.outerHTML;");

            //string sHtml = await webView21.CoreWebView2.ExecuteScriptAsync("document.documentElement.outerHTML");
            string sHtmlDecoded = System.Text.RegularExpressions.Regex.Unescape(allHtml);

            HtmlWeb hw = new HtmlWeb();
            HtmlAgilityPack.HtmlDocument doc = new HtmlAgilityPack.HtmlDocument();
            doc.LoadHtml(sHtmlDecoded);

            var nodes = doc.DocumentNode.SelectNodes("//a[@href]");
            List<HtmlNode> links = nodes != null ? nodes.ToList() : new List<HtmlNode>();

            chatNames = new List<string>();
            foreach (HtmlNode link in links)
            {
                string hrefValue = link.GetAttributeValue("href", string.Empty);

                if (hrefValue.StartsWith("https://chat.whatsapp.com/"))
                {
                    chatNames.Add(hrefValue);
                }
            }
            return chatNames;
        }

        private void Browser_FormClosed(object sender, FormClosedEventArgs e)
        {
            OnbroserClose(e);
        }


    }
}

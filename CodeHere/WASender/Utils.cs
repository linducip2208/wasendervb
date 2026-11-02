using FontAwesome.Sharp;
using MaterialSkin;
using MaterialSkin.Controls;
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.WinForms;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;
using WASender.Models;

namespace WASender
{
    public static class Utils
    {

        private static OpenQA.Selenium.IWebDriver _driver;
        private static WaSenderBrowser _waSenderBrowser { get; set; }

        private static TestClass _TestClass;
        public static WASender.Model.ColorSchemeenum? currentColorscheme;

        public static TestClass testClass
        {
            get
            {
                if (_TestClass == null)
                    _TestClass = new TestClass();

                return _TestClass;
            }
            set { _TestClass = value; }
        }


        public static WaSenderBrowser waSenderBrowser
        {
            get
            {
                try { if (_waSenderBrowser != null && _waSenderBrowser.IsDisposed) _waSenderBrowser = null; }
                catch (Exception) { try { _waSenderBrowser = null; } catch (Exception) { } }
                return _waSenderBrowser;
            }
            set { _waSenderBrowser = value; }
        }

        [System.Obsolete("Selenium legacy. WA Web wajib WebView2 (BrowserPolicy). Scraper pakai ScraperWebView2.")]
        public static OpenQA.Selenium.IWebDriver Driver
        {
            get
            {
                return _driver;
            }
            set { _driver = value; }
        }


        public static TabPage GetTabPageById(WaSenderBrowser browser, string id)
        {
            TabPage tp = null;
            foreach (TabPage page in browser.tabControl1.TabPages)
            {
                var _tag = (System.Data.DataRow)page.Tag;
                if (_tag["ID"].ToString() == id)
                {
                    tp = page;
                }
            }
            return tp;
        }

        public static void showError(string messgae)
        {
            MessageBox.Show(messgae, Strings.Error, MessageBoxButtons.OK, MessageBoxIcon.Error);
        }


        public static bool isWaLoaded(OpenQA.Selenium.IWebDriver driver)
        {
            try
            {
                bool isElementDisplayed = AutomationCommon.IsElementPresent(OpenQA.Selenium.By.ClassName("_1XkO3"), driver);
                if (isElementDisplayed == true)
                {
                    return true;
                }
            }
            catch (Exception ex)
            {
                throw;
            }
            try
            {
                bool isElementDisplayed = AutomationCommon.IsElementPresent(OpenQA.Selenium.By.ClassName("_1jJ70"), driver);
                if (isElementDisplayed)
                {
                    return true;
                }
            }
            catch (Exception ex)
            {
                throw;
            }
            try
            {
                bool isElementDisplayed = AutomationCommon.IsElementPresent(OpenQA.Selenium.By.ClassName("_aigu"), driver);
                if (isElementDisplayed)
                {
                    return true;
                }
            }
            catch (Exception ex)
            {
                
                throw;
            }
            try
            {
                bool isElementDisplayed = WAPIHelper.isWaInited(driver);
                if (isElementDisplayed == true)
                {
                    return true;
                }
            }
            catch (Exception)
            {
                
                throw;
            }

            return false;
        }

        public static string sanitizeNumber(string number)
        {
            string newNumber = number.Replace(" ", "").Replace(Environment.NewLine, "").Replace("+", "").Trim().Replace("-", "");
            return newNumber;
        }

        public static WebView2 GetWebViewById(WaSenderBrowser browser, string id)
        {
            WebView2 vw = null;
            foreach (TabPage page in browser.tabControl1.TabPages)
            {
                var _tag = (System.Data.DataRow)page.Tag;
                if (_tag["ID"].ToString() == id)
                {
                    vw = (WebView2)page.Controls.Find("webView21", true)[0];

                }
            }
            return vw;
        }



        public static string sanitizeFileName(string currentName)
        {
            string ss = currentName.Replace("/", "_").Replace("\\", "");

            return currentName.Replace("/", "_").Replace("\\","");
        }

        public static bool IsValidEmail(string emailId)
        {

            var email = new EmailAddressAttribute();

            bool _ok=email.IsValid(emailId);

            if (_ok)
            {
                bool isEmail = Regex.IsMatch(emailId, @"\A(?:[a-z0-9!#$%&'*+/=?^_`{|}~-]+(?:\.[a-z0-9!#$%&'*+/=?^_`{|}~-]+)*@(?:[a-z0-9](?:[a-z0-9-]*[a-z0-9])?\.)+[a-z0-9](?:[a-z0-9-]*[a-z0-9])?)\Z", RegexOptions.IgnoreCase);
                if (isEmail)
                {
                    if ((emailId.Contains("\\")) || (emailId.Contains("~")) || (emailId.Contains("^")) || (emailId.Contains("`")) || (emailId.Contains("#")) || (emailId.Contains("!")) || (emailId.Contains("%")) || (emailId.Contains("//")) || (emailId.Contains("|")) || (emailId.Contains("*")) || (emailId.Contains("{")) || (emailId.Contains("}")) || (emailId.Length > 100))
                    {
                        return false;
                    }
                    else
                    {
                        return true;
                    }
                }
                else
                {
                    return false;
                }
            }
            else
            {
                return _ok;
            }

            

            //var trimmedEmail = email.Trim();

            //if (trimmedEmail.EndsWith("."))
            //{
            //    return false; // suggested by @TK-421
            //}
            //try
            //{
            //    var addr = new System.Net.Mail.MailAddress(email);
            //    return addr.Address == trimmedEmail;
            //}
            //catch
            //{
            //    return false;
            //}
        }

        public static string Base64Encode(string plainText)
        {
            var plainTextBytes = System.Text.Encoding.UTF8.GetBytes(plainText);
            return System.Convert.ToBase64String(plainTextBytes);
        }

        public static string Base64Decode(string base64EncodedData)
        {
            var base64EncodedBytes = System.Convert.FromBase64String(base64EncodedData);
            return System.Text.Encoding.UTF8.GetString(base64EncodedBytes);
        }

        public static MainUC GetMainUCId(WaSenderBrowser browser, string id)
        {
            MainUC vw = null;
            foreach (TabPage page in browser.tabControl1.TabPages)
            {
                var _tag = (System.Data.DataRow)page.Tag;
                if (_tag["ID"].ToString() == id)
                {
                    vw = (MainUC)page.Controls.Find("MainUC", true)[0];

                }
            }
            return vw;
        }


        public static WebView2 GetActiveWebView(WaSenderBrowser browser)
        {
            try
            {
                int index = browser.tabControl1.SelectedIndex;
                string tabName = browser.tabControl1.TabPages[index].Name;
                TabPage tp = browser.tabControl1.SelectedTab;
                WebView2 vw = (WebView2)tp.Controls.Find("webView21", true)[0];

                return vw;
            }
            catch (Exception)
            {
                throw;
            }
        }

        [System.Obsolete("Jangan panggil untuk WA. WebView2 adalah satu-satunya jalur (lihat BrowserPolicy + WaSenderBrowser). Dibiarkan no-op agar kompil lama tidak rusak.")]
        public static void SetDriver()
        {
            // Migrasi WebView2: tidak lagi membuat ChromeDriver untuk WA Web.
            // Scraper yang masih butuh browser memakai ScraperWebView2 (WebView2).
            // Biarkan _driver null agar pemanggil legacy masuk jalur WebView2 (initWABrowser).
            return;
        }


        public static void showAlert(string message, Alerts.Alert.enmType alertType)
        {
            Alerts.Alert alert = new Alerts.Alert();
            alert.showAlert(message, alertType);
        }

        public static string ExtractBetweenTwoStrings(string FullText, string StartString, string EndString, bool IncludeStartString, bool IncludeEndString)
        {
            try
            {
                int Pos1 = FullText.IndexOf(StartString) + StartString.Length;
                int Pos2 = FullText.IndexOf(EndString, Pos1);
                return ((IncludeStartString) ? StartString : "") + FullText.Substring(Pos1, Pos2 - Pos1) + ((IncludeEndString) ? EndString : "");
            }
            catch (Exception ex)
            {
                return ex.ToString();
            }
        }

        public static int getRandom(int min, int max)
        {
            try
            {
                int delay = new Random().Next(min, max);
                return delay;
            }
            catch (Exception ex)
            {

                return min;
            }
        }


        public static string SanitizeNumber(this string number)
        {
            number = number.Replace("+", "");
            number = number.Replace(" ", "");
            number = number.Replace(",", "");
            number = number.Replace("-", "");
            number = number.Replace("(", "");
            number = number.Replace(")", "");
            number = Regex.Replace(number, "[^a-zA-Z0-9_.]+", "", RegexOptions.Compiled);
            return number;
        }

        public static string exactDatetime()
        {
            string date = "";

            var datr = DateTime.Now;

            date = datr.Year + "-" + datr.Month + "-" + datr.Day + " " + datr.Hour + ":" + datr.Minute + ":" + datr.Second;

            return date;
        }
        public static void setTooltiop(Control control, string caption)
        {
            System.Windows.Forms.ToolTip ToolTip1 = new System.Windows.Forms.ToolTip();
            ToolTip1.BackColor = System.Drawing.Color.Black;
            ToolTip1.ForeColor = System.Drawing.Color.Wheat;
            ToolTip1.SetToolTip(control, caption);
        }

        //public static string sanitizeFileName(string originalName)
        //{
        //    return originalName.Replace("\\", "").Replace("/", "");
        //}

        public static void selectFileForMessage(MaterialListView lstView)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Title = Strings.SelectExcel;
            openFileDialog.DefaultExt = "";
            openFileDialog.Multiselect = false;
            if (openFileDialog.ShowDialog() == DialogResult.OK)
            {
                string file = openFileDialog.FileName;
                string[] itemsss = new string[] { file, "[No Caption]" };
                ListViewItem i = new ListViewItem(itemsss);
                lstView.Items.Add(i);
            }
        }

        public static string getatachmenttype(string type)
        {
            string strtype = "";
            if (type == StaticMethods.media)
            {
                strtype = Strings.Media;
            }
            else if (type == StaticMethods.location)
            {
                strtype = Strings.Location;
            }
            else if (type == StaticMethods.contact)
            {
                strtype = Strings.Contact;
            }
            else if (type == StaticMethods.audio)
            {
                strtype = Strings.Audio;
            }
            else if (type == StaticMethods.sticker)
            {
                strtype = Strings.Sticker;
            }
            else if (type == StaticMethods.videoNote)
            {
                strtype = Strings.VideoNote;
            }
            return strtype;

        }

        public static void selectFileForMessage(DataGridView lstView,string fileType="")
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            //openFileDialog.Title = Strings.SelectExcel;

            openFileDialog.DefaultExt = "";
            openFileDialog.Multiselect = true;

            if (fileType == "")
            {
                //openFileDialog.Filter = filters;
            }
            else if (fileType == StaticMethods.audio)
            {
                openFileDialog.Filter = "Audio Files (*.ogg, *.mp3)| *.ogg;*.mp3";
            }
            else if (fileType == StaticMethods.sticker)
            {
                openFileDialog.Filter = "Image Files|*.jpg;*.jpeg;*.png;*.gif;*.tif;*.webp";
            }
            else if (fileType == StaticMethods.videoNote)
            {
                openFileDialog.Filter = "Video Files|*.mp4;";
            }

            if (openFileDialog.ShowDialog() == DialogResult.OK)
            {
                foreach (String file in openFileDialog.FileNames)
                {
                    long length = new System.IO.FileInfo(file).Length;
                    double fileSize = (length / 1024f) / 1024f;

                    if (fileSize > 12)
                    {
                        MessageBox.Show(Strings.YouraretryingtosendLargefile, Strings.Warning, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    }

                    FilesModel model = new FilesModel();
                    model.Id = Guid.NewGuid().ToString();
                    model.filePath = file;
                    model.attachWithMainMessage = false;

                    if (fileType == "")
                    {
                        model.type = StaticMethods.media;
                        model.Caption = "";
                        lstView.Rows.Add(getatachmenttype(StaticMethods.media), file, "");
                    }
                    else if (fileType == StaticMethods.audio)
                    {
                        model.type = fileType;
                        model.Caption = "";
                        lstView.Rows.Add(getatachmenttype(fileType), file, "");
                    }
                    else if (fileType == StaticMethods.sticker)
                    {
                        model.type = fileType;
                        model.Caption = "";
                        lstView.Rows.Add(getatachmenttype(fileType), file, "");
                    }
                    else if (fileType == StaticMethods.videoNote)
                    {
                        model.type = fileType;
                        model.Caption = "";
                        lstView.Rows.Add(getatachmenttype(fileType), file, "");
                    }
                    
                    lstView.Rows[lstView.Rows.Count - 2].Tag = model;
                    
                }

            }
        }

        public static void removeListViewItem(KeyEventArgs e, MaterialListView lstView)
        {
            if (e.KeyCode == Keys.Delete)
            {
                var removables = new List<ListViewItem>();
                foreach (ListViewItem item in lstView.SelectedItems)
                {
                    removables.Add(item);
                }
                foreach (var item in removables)
                {
                    lstView.Items.Remove(item);
                }
            }
        }

        public static MaterialSkin.MaterialSkinManager SetColorScheme(MaterialSkin.MaterialSkinManager materialSkinManager, MaterialForm materialForm, Model.ColorSchemeenum? colorSchemeenum = null)
        {
            currentColorscheme = colorSchemeenum;

            materialSkinManager = MaterialSkin.MaterialSkinManager.Instance;
            materialSkinManager.AddFormToManage(materialForm);
            materialSkinManager.Theme = MaterialSkin.MaterialSkinManager.Themes.LIGHT;

            Primary primary;
            Accent accent;
            if (colorSchemeenum == null || colorSchemeenum == Model.ColorSchemeenum.Green)
            {
                primary = Primary.Green700; accent = Accent.Green700;
            }
            else if (colorSchemeenum == Model.ColorSchemeenum.Red)
            {
                primary = Primary.Red700; accent = Accent.Red700;
            }
            else if (colorSchemeenum == Model.ColorSchemeenum.Amber)
            {
                primary = Primary.Amber700; accent = Accent.Amber700;
            }
            else if (colorSchemeenum == Model.ColorSchemeenum.Blue)
            {
                primary = Primary.Blue700; accent = Accent.Blue700;
            }
            else if (colorSchemeenum == Model.ColorSchemeenum.BlueGrey)
            {
                primary = Primary.BlueGrey700; accent = Accent.Blue700;
            }
            else if (colorSchemeenum == Model.ColorSchemeenum.Brown)
            {
                primary = Primary.Brown700; accent = Accent.Blue700;
            }
            else if (colorSchemeenum == Model.ColorSchemeenum.Cyan)
            {
                primary = Primary.Cyan700; accent = Accent.Cyan700;
            }
            else if (colorSchemeenum == Model.ColorSchemeenum.DeepOrange)
            {
                primary = Primary.DeepOrange700; accent = Accent.DeepOrange700;
            }
            else if (colorSchemeenum == Model.ColorSchemeenum.DeepPurple)
            {
                primary = Primary.DeepPurple700; accent = Accent.DeepPurple700;
            }
            else if (colorSchemeenum == Model.ColorSchemeenum.Grey)
            {
                primary = Primary.Grey700; accent = Accent.Blue700;
            }
            else if (colorSchemeenum == Model.ColorSchemeenum.Indigo)
            {
                primary = Primary.Indigo700; accent = Accent.Indigo700;
            }
            else if (colorSchemeenum == Model.ColorSchemeenum.LightBlue)
            {
                primary = Primary.LightBlue700; accent = Accent.LightBlue700;
            }
            else if (colorSchemeenum == Model.ColorSchemeenum.Orange)
            {
                primary = Primary.Orange700; accent = Accent.Orange700;
            }
            else if (colorSchemeenum == Model.ColorSchemeenum.Pink)
            {
                primary = Primary.Pink700; accent = Accent.Pink700;
            }
            else if (colorSchemeenum == Model.ColorSchemeenum.Purple)
            {
                primary = Primary.Purple700; accent = Accent.Purple700;
            }
            else if (colorSchemeenum == Model.ColorSchemeenum.Teal)
            {
                primary = Primary.Teal700; accent = Accent.Teal700;
            }
            else if (colorSchemeenum == Model.ColorSchemeenum.Yellow)
            {
                primary = Primary.Yellow700; accent = Accent.Yellow700;
            }
            else
            {
                primary = Primary.Green700; accent = Accent.Green700;
            }

            materialSkinManager.ColorScheme = new ColorScheme(primary, GetPrimary400(primary), GetPrimary900(primary), accent, TextShade.WHITE);
            return materialSkinManager;
        }

        private static Primary GetPrimary400(Primary p)
        {
            switch (p)
            {
                case Primary.Green700: return Primary.Green400;
                case Primary.Red700: return Primary.Red400;
                case Primary.Amber700: return Primary.Amber400;
                case Primary.Blue700: return Primary.Blue400;
                case Primary.BlueGrey700: return Primary.BlueGrey400;
                case Primary.Brown700: return Primary.Brown400;
                case Primary.Cyan700: return Primary.Cyan400;
                case Primary.DeepOrange700: return Primary.DeepOrange400;
                case Primary.DeepPurple700: return Primary.DeepPurple400;
                case Primary.Grey700: return Primary.Grey400;
                case Primary.Indigo700: return Primary.Indigo400;
                case Primary.LightBlue700: return Primary.LightBlue400;
                case Primary.Lime700: return Primary.Lime400;
                case Primary.Orange700: return Primary.Orange400;
                case Primary.Pink700: return Primary.Pink400;
                case Primary.Purple700: return Primary.Purple400;
                case Primary.Teal700: return Primary.Teal400;
                case Primary.Yellow700: return Primary.Yellow400;
                default: return Primary.Grey400;
            }
        }

        private static Primary GetPrimary900(Primary p)
        {
            switch (p)
            {
                case Primary.Green700: return Primary.Green900;
                case Primary.Red700: return Primary.Red900;
                case Primary.Amber700: return Primary.Amber900;
                case Primary.Blue700: return Primary.Blue900;
                case Primary.BlueGrey700: return Primary.BlueGrey900;
                case Primary.Brown700: return Primary.Brown900;
                case Primary.Cyan700: return Primary.Cyan900;
                case Primary.DeepOrange700: return Primary.DeepOrange900;
                case Primary.DeepPurple700: return Primary.DeepPurple900;
                case Primary.Grey700: return Primary.Grey900;
                case Primary.Indigo700: return Primary.Indigo900;
                case Primary.LightBlue700: return Primary.LightBlue900;
                case Primary.Lime700: return Primary.Lime900;
                case Primary.Orange700: return Primary.Orange900;
                case Primary.Pink700: return Primary.Pink900;
                case Primary.Purple700: return Primary.Purple900;
                case Primary.Teal700: return Primary.Teal900;
                case Primary.Yellow700: return Primary.Yellow900;
                default: return Primary.Grey900;
            }
        }

    }
}

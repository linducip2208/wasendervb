using MaterialSkin.Controls;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using WaAutoReplyBot;
using WASender.Models;
using System.Web.Script.Serialization;
using System.Collections;
using System.Net;
using System.IO.Compression;
using Microsoft.Win32;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Remote;
namespace WASender
{
    public partial class GeneralSettings : MyMaterialPopOp
    {
        WaSenderForm waSenderForm;
        GeneralSettingsModel generalSettingsModel;
        public GeneralSettings(WaSenderForm _waSenderForm)
        {
            this.waSenderForm = _waSenderForm;
            InitializeComponent();
            this.Icon = Strings.AppIcon;
            init();
            initLanguages();
        }

        public GeneralSettings()
        {
            InitializeComponent();
            this.Icon = Strings.AppIcon;
            init();
            initLanguages();
        }

        private void initLanguages()
        {
            this.Text = Strings.GeneralSettings;
            materialButton2.Text = Strings.BlockList;
            btnSave.Text = Strings.Save;
            materialButton6.Text = Strings.CheckforInternalUpdate;

            materialButton5.Text = Strings.clearsessions;
            materialButton3.Text = Strings.ClearProfileCache;
            materialButton4.Text = Strings.ClearBOTCache;
            txtChromePath.Hint = Strings.ChromeEXEpath;

            materialButton7.Text = Strings.AboutLicence;
            
            materialCheckbox2.Text = Strings.Dontlinkpreviewwhilesendingmessage;
            materialCheckbox3.Text = Strings.FilterNumbersBeforeSendingMessages;
            materialLabel1.Text = Strings.Browser;

            tabPage1.Text = Strings.General;
            tabPage2.Text = Strings.MessageSending;
            tabPage3.Text = Strings.GoogleMapExtractor;
            materialButton1.Text = Strings.Save;
            materialLabel3.Text = Strings.GoogleMapUI;
            materialButton11.Text = Strings.Save;

        }

        private void init()
        {
            getData();
        }

        private void getData()
        {

            // Browser dikunci WebView2 (lihat BrowserPolicy) — tidak ada pilihan lagi.
            try
            {
                SendLimits lim = SendGovernor.LoadLimits();
                txtDailyLimit.Text = lim.DailyPerAccount.ToString();
                txtMinDelay.Text = lim.MinDelaySec.ToString();
                txtMaxDelay.Text = lim.MaxDelaySec.ToString();
            }
            catch (Exception) { }

            Dictionary<string, string> test2 = new Dictionary<string, string>();
            test2.Add("1", Strings.NEWUI);
            test2.Add("2", Strings.OldClassicUI);

            materialComboBox2.DataSource = new BindingSource(test2, null);
            materialComboBox2.DisplayMember = "Value";
            materialComboBox2.ValueMember = "Key";

            String GetGeneralSettingsFilePath = Config.GetGeneralSettingsFilePath();

            if (File.Exists(GetGeneralSettingsFilePath))
            {
                string json = File.ReadAllText(GetGeneralSettingsFilePath);
                generalSettingsModel = JsonConvert.DeserializeObject<GeneralSettingsModel>(json);
                if (generalSettingsModel == null)
                {
                    generalSettingsModel = new GeneralSettingsModel();
                    generalSettingsModel.filterNumbersBeforeSendingMessage = false;
                }
                if (generalSettingsModel.ChromeProfilePath != null && generalSettingsModel.ChromeProfilePath != "")
                {
                    txtChromePath.Text = generalSettingsModel.ChromeProfilePath;
                }
               
                if (generalSettingsModel.browserType == 0 || generalSettingsModel.browserType == 1)
                {
                    // Migrasi WebView2: 0=belum set, 1=Selenium legacy -> paksa 2.
                    generalSettingsModel.browserType = 2;
                }

                try
                {
                    materialCheckbox3.Checked = generalSettingsModel.filterNumbersBeforeSendingMessage;
                }
                catch (Exception )
                {
                    
                }
                try
                {
                    materialCheckbox2.Checked = generalSettingsModel.disableLinkPreview;
                }
                catch (Exception)
                {

                }


            }

            materialComboBox2.SelectedValue = generalSettingsModel.crowlingMethod.ToString() == "0" ? "1" : generalSettingsModel.crowlingMethod.ToString();
            materialMaskedTextBox1.Text = generalSettingsModel.scollDelay.ToString() == "0" ? "4" : generalSettingsModel.scollDelay.ToString();


        }

        private void GeneralSettings_FormClosing(object sender, FormClosingEventArgs e)
        {
            
        }

        private void btnSave_Click(object sender, EventArgs e)
        {
            saveSettings();
        }

        private void saveSettings()
        {
            String GetGeneralSettingsFilePath = Config.GetGeneralSettingsFilePath();

            if (!File.Exists(GetGeneralSettingsFilePath))
            {
                File.Create(GetGeneralSettingsFilePath).Close();
            }


            if (this.txtChromePath.Text != "")
            {

                if (!Directory.Exists(txtChromePath.Text))
                {
                    MaterialSnackBar SnackBarMessage = new MaterialSnackBar(Strings.InputPathisnotCorrectfolderpath, Strings.OK, true);
                    SnackBarMessage.Show(this);
                    return;
                }
            }

            // test Save and load letter
            generalSettingsModel.crowlingMethod = Convert.ToInt32(materialComboBox2.SelectedValue);
            generalSettingsModel.scollDelay = Convert.ToInt32(materialMaskedTextBox1.Text);

            generalSettingsModel.filterNumbersBeforeSendingMessage = materialCheckbox3.Checked;
            generalSettingsModel.ChromeProfilePath = txtChromePath.Text == null ? "" : txtChromePath.Text;
            generalSettingsModel.browserType = 2; // WebView2 dikunci (BrowserPolicy)
            generalSettingsModel.disableLinkPreview = materialCheckbox2.Checked;
            try
            {
                SendLimits lim = SendGovernor.LoadLimits();
                int d, mn, mx;
                if (int.TryParse((txtDailyLimit.Text ?? "").Trim(), out d)) lim.DailyPerAccount = Math.Max(1, Math.Min(5000, d));
                if (int.TryParse((txtMinDelay.Text ?? "").Trim(), out mn)) lim.MinDelaySec = Math.Max(0, Math.Min(300, mn));
                if (int.TryParse((txtMaxDelay.Text ?? "").Trim(), out mx)) lim.MaxDelaySec = Math.Max(0, Math.Min(300, mx));
                if (lim.MaxDelaySec < lim.MinDelaySec) lim.MaxDelaySec = lim.MinDelaySec;
                SendGovernor.SaveLimits(lim);
            }
            catch (Exception) { }
            string Json = JsonConvert.SerializeObject(generalSettingsModel, Formatting.Indented);

            File.WriteAllText(GetGeneralSettingsFilePath, Json);

            MaterialSnackBar SnackBarMessage1 = new MaterialSnackBar(Strings.SettingsSavedSuccessfully, Strings.OK, true);
            SnackBarMessage1.Show(this);
            if (this.waSenderForm != null)
            {
                this.waSenderForm.checkBrowserType();
            }
        }


        private void materialButton2_Click(object sender, EventArgs e)
        {
            BlockList blockList = new BlockList();
            blockList.ShowDialog();

        }

        private void GeneralSettings_Load(object sender, EventArgs e)
        {
          
        }
       
       
        private void ClearCHromeSessions()
        {
            try { BrowserPolicy.KillLegacyDrivers(); } catch (Exception) { }
            if (Utils.waSenderBrowser != null)
            {
                try { Utils.waSenderBrowser.Close(); } catch (Exception) { }
                Utils.waSenderBrowser = null;
            }
        }
        private void materialButton5_Click_1(object sender, EventArgs e)
        {
            ClearCHromeSessions();
            MaterialSnackBar SnackBarMessage = new MaterialSnackBar("Done 👍👍👍👍", Strings.OK, true);
            SnackBarMessage.Show(this);
        }


        private void DeleteDirectory(string path)
        {
            if (Directory.Exists(path))
            {
                foreach (string file in Directory.GetFiles(path))
                {
                    File.Delete(file);
                }
                foreach (string directory in Directory.GetDirectories(path))
                {
                    DeleteDirectory(directory);
                }
                Directory.Delete(path);
            }
        }
        private void materialButton3_Click_1(object sender, EventArgs e)
        {
            try
            {
                string ProfileFolderpath = Config.GetChromeProfileFolder();
                DeleteDirectory(ProfileFolderpath);
                MaterialSnackBar SnackBarMessage = new MaterialSnackBar("Done 👍👍👍👍", Strings.OK, true);
                SnackBarMessage.Show(this);
            }
            catch (Exception exc)
            {
                MaterialSnackBar SnackBarMessage = new MaterialSnackBar("Not Done 👎👎 -" + exc.Message, Strings.OK, true);
                SnackBarMessage.Show(this);
            }
        }

        private void materialButton4_Click_1(object sender, EventArgs e)
        {
            try
            {
                string ProfileFolderpath = Config.GetChromeProfileFolderBot();
                DeleteDirectory(ProfileFolderpath);
                MaterialSnackBar SnackBarMessage = new MaterialSnackBar("Done 👍👍👍👍", Strings.OK, true);
                SnackBarMessage.Show(this);
            }
            catch (Exception exc)
            {
                MaterialSnackBar SnackBarMessage = new MaterialSnackBar("Not Done 👎👎 -" + exc.Message, Strings.OK, true);
                SnackBarMessage.Show(this);
            }

        }

        private void materialButton6_Click(object sender, EventArgs e)
        {
            try
            {
                string UpdateFIle = Config.GetTempFolderPath() + "\\" + Guid.NewGuid().ToString() + ".zip";
                string extractPath = Config.WAPIFolderFolder();

                WebClient webClient = new WebClient();
                webClient.DownloadFile("https://download.pandusolusi.com/wasender/update.zip", UpdateFIle);

                var archive = ZipFile.Open(UpdateFIle, ZipArchiveMode.Read);

                DirectoryInfo di = Directory.CreateDirectory(extractPath);
                string destinationDirectoryFullPath = di.FullName;

                foreach (ZipArchiveEntry file in archive.Entries)
                {
                    string completeFileName = Path.GetFullPath(Path.Combine(destinationDirectoryFullPath, file.FullName));

                    if (!completeFileName.StartsWith(destinationDirectoryFullPath, StringComparison.OrdinalIgnoreCase))
                    {
                        throw new IOException("Trying to extract file outside of destination directory. See this link for more info: https://snyk.io/research/zip-slip-vulnerability");
                    }

                    if (file.Name == "")
                    {
                        Directory.CreateDirectory(Path.GetDirectoryName(completeFileName));
                        continue;
                    }
                    file.ExtractToFile(completeFileName, true);

                    try
                    {
                        string readText = File.ReadAllText(completeFileName);
                        File.WriteAllText(completeFileName, readText + Environment.NewLine);
                    }
                    catch (Exception ex)
                    {
                        
                    }
                }

                archive.Dispose();
                File.Delete(UpdateFIle);
                MaterialSnackBar SnackBarMessage = new MaterialSnackBar("Done 👍👍👍👍", Strings.OK, true);
                SnackBarMessage.Show(this);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Not Done 👎👎 -" + ex.Message);
            }
        }

        public void ProductDeActivated()
        {
            if (this.waSenderForm != null)
            {
                this.Hide();
                this.waSenderForm.CHeckForActivation();

            }
        }
        private void materialButton7_Click(object sender, EventArgs e)
        {
            About about = new About(this);
            about.ShowDialog();
        }


        private void materialButton1_Click(object sender, EventArgs e)
        {
            saveSettings();
        }

        private void materialButton9_Click(object sender, EventArgs e)
        {
            string _message = "" + Strings.NEWUI + " - " + Strings.datawillbescrappedacordingtonewGoogleMapUI + ". " + Environment.NewLine + Environment.NewLine;
            _message += "" + Strings.OldClassicUI + " - " + " " + Strings.datawillbescrappedacordingtooldClassicgooglemapUI;
            MessageBox.Show(_message, Strings.Info, MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void materialButton8_Click(object sender, EventArgs e)
        {
            MessageBox.Show(Strings.IntheGoogleMapNewUInewdataloadedafterpagescroll, Strings.Info, MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void materialButton11_Click(object sender, EventArgs e)
        {
            saveSettings();
        }

        private void materialComboBox2_SelectedIndexChanged(object sender, EventArgs e)
        {
            try
            {
                int _val = Convert.ToInt32(materialComboBox2.SelectedValue);
                if (_val == 1)
                {
                    visibleScrollSettings(true);
                }
                else
                {
                    visibleScrollSettings(false);
                }
            }
            catch (Exception ex)
            {
                
            }
        }
        private void visibleScrollSettings(bool isVisible)
        {
            materialLabel2.Visible = isVisible;
            materialMaskedTextBox1.Visible = isVisible;
            materialButton8.Visible = isVisible;
        }
    }
}

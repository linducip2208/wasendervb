using MaterialSkin.Controls;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using WaAutoReplyBot;

namespace WASender
{
    public partial class About : MyMaterialPopOp
    {
        GeneralSettings generalSettings;
        public About(GeneralSettings _generalSettings)
        {
            InitializeComponent();
            this.Icon = Strings.AppIcon;
            this.generalSettings = _generalSettings;
            lblSoftwarename.Text = Strings.AppName;
            materialLabel2.Text = Strings.SoftwareVersion;
            DateTime? _date = Config.getEndDate();
            var _online = LicenseServer.Load();
            if (_online != null)
            {
                // Lisensi panel online: tampilkan masa aktif dari server, bukan file legacy.
                string exp = LicenseServer.ExpiryText();
                int sisa = LicenseServer.RemainingDays();
                string mulai = LicenseServer.IssuedText();
                if (string.IsNullOrEmpty(exp) || sisa == int.MaxValue)
                {
                    materialLabel5.Text = string.IsNullOrEmpty(mulai) ? "Lifetime" : "Sejak " + mulai;
                    materialLabel8.Text = "Unlimited";
                    materialLabel8.ForeColor = System.Drawing.Color.FromArgb(0, 150, 0);
                }
                else
                {
                    materialLabel5.Text = exp;
                    if (sisa < 0) sisa = 0;
                    bool en = false;
                    try { en = (Strings.selectedLanguage ?? "").StartsWith("Eng", StringComparison.OrdinalIgnoreCase); } catch (Exception) { }
                    materialLabel8.Text = sisa.ToString() + (en ? " days" : " hari");
                    if (sisa <= 7) materialLabel8.ForeColor = System.Drawing.Color.FromArgb(200, 0, 0);
                    else if (sisa <= 30) materialLabel8.ForeColor = System.Drawing.Color.FromArgb(200, 120, 0);
                    else materialLabel8.ForeColor = System.Drawing.Color.FromArgb(0, 150, 0);
                }
            }
            else if (_date == null)
            {
                materialLabel5.Text = "Lifetime";
                materialLabel8.Text = "Unlimited";
                materialLabel8.ForeColor = System.Drawing.Color.FromArgb(0, 150, 0);
            }
            else
            {
                materialLabel5.Text = _date.Value.Day.ToString() + "-" + _date.Value.ToString("MMM") + "-" + _date.Value.Year + " " + _date.Value.Hour + ":" + _date.Value.Minute;
                int sisa = (int)System.Math.Ceiling((_date.Value - DateTime.Now).TotalDays);
                if (sisa < 0) sisa = 0;
                bool en = false;
                try { en = (Strings.selectedLanguage ?? "").StartsWith("Eng", StringComparison.OrdinalIgnoreCase); } catch (Exception) { }
                materialLabel8.Text = sisa.ToString() + (en ? " days" : " hari");
                if (sisa <= 7) materialLabel8.ForeColor = System.Drawing.Color.FromArgb(200, 0, 0);
                else if (sisa <= 30) materialLabel8.ForeColor = System.Drawing.Color.FromArgb(200, 120, 0);
                else materialLabel8.ForeColor = System.Drawing.Color.FromArgb(0, 150, 0);
            }

            materialButton2.Text = Strings.DeActivateLicence;
        }

        private void materialButton1_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void linkSupport_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            try { System.Diagnostics.Process.Start("https://wa.me/6281296052010"); }
            catch (Exception ex) { try { MessageBox.Show(ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error); } catch (Exception) { } }
        }

        private void materialButton2_Click(object sender, EventArgs e)
        {
            MaterialDialog materialDialog = new MaterialDialog(this, Strings.Delete, Strings.AreyouSure, Strings.Yes, true, Strings.Cancel);
            DialogResult result = materialDialog.ShowDialog(this);
            if (result == DialogResult.OK)
            {
                Config.deActivateProduct();
                this.Hide();
                this.generalSettings.ProductDeActivated();
                
            }
        }

        private void About_Load(object sender, EventArgs e)
        {
            initLanguages();
        }

        private void initLanguages()
        {
            this.Text = Strings.AboutLicence;
            materialLabel1.Text = Strings.Version;
            materialLabel3.Text = Strings.LicenceInformation;
            materialLabel4.Text = Strings.Activated;
            materialLabel6.Text = Strings.LicenceExpiresOn;
            try
            {
                bool en = (Strings.selectedLanguage ?? "").StartsWith("Eng", StringComparison.OrdinalIgnoreCase);
                materialLabel7.Text = en ? "Days Remaining" : "Sisa Masa Aktif";
            }
            catch (Exception) { materialLabel7.Text = "Sisa Masa Aktif"; }
            materialButton1.Text = Strings.OK;
        }
    }
}

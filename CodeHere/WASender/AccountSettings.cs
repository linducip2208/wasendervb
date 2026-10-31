using MaterialSkin.Controls;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using WaAutoReplyBot;

namespace WASender
{
    public partial class AccountSettings : MyMaterialPopOp
    {
        string accountName, sessionId;

        public AccountSettings(string _accountName, string _sessionId)
        {
            InitializeComponent();
            accountName = _accountName;
            sessionId = _sessionId;
            this.Icon = Strings.AppIcon;
            materialCheckbox1.Text = Strings.Applythisproxytoselectedaccount;
        }

        private void AccountSettings_Load(object sender, EventArgs e)
        {
            initLanguages();

            LoadExistingData();
        }

        private void LoadExistingData()
        {
            DataTable dt = new SqLiteBaseRepository().getSessionById(sessionId);

            string Proxy_Server = dt.Rows[0]["Proxy_Server"].ToString();

            

            string Proxy_Port = dt.Rows[0]["Proxy_Port"].ToString();
            string Proxy_UserName = dt.Rows[0]["Proxy_UserName"].ToString();
            string Proxy_Password = dt.Rows[0]["Proxy_Password"].ToString();
            int Proxy_Enabled = 0;

            try
            {
                Proxy_Enabled = Convert.ToInt32(dt.Rows[0]["Proxy_Enabled"]);
            }
            catch (Exception ex)
            {

            }
            materialTextBox21.Text = Proxy_Server;
            materialTextBox22.Text = Proxy_Port;
            materialTextBox23.Text = Proxy_UserName;
            materialTextBox24.Text = Proxy_Password;

            if (Proxy_Enabled == 1)
                materialCheckbox1.Checked = true;
        }

        private void initLanguages()
        {
            this.Text = Strings.AccountSettings + " - " + accountName;
            tabPage1.Text = Strings.Proxy;
            materialTextBox21.Hint = Strings.ProxyServer + " * ";
            materialTextBox22.Hint = Strings.Port + " * ";
            materialTextBox23.Hint = Strings.UserName;
            materialTextBox24.Hint = Strings.Password;
            materialButton2.Text = Strings.TestConnection;
            materialButton1.Text = Strings.Save;
        }

        private bool Validate()
        {
            if ((materialTextBox21.Text == "") || (materialTextBox22.Text == ""))
            {
                Utils.showError(Strings.Pleaseproviderequiredinputs);
                return false;
            }
            return true;
        }

        private async Task<bool> testConnection()
        {
            if (Validate())
            {
                label1.Text = Strings.Status + " : " + Strings.Connecting + "...";

                try
                {
                    WebClient wc = new WebClient();
                    if (materialTextBox23.Text != "" && materialTextBox24.Text != "")
                    {
                        NetworkCredential c = new NetworkCredential(materialTextBox23.Text, materialTextBox24.Text);
                        wc.Proxy = new WebProxy("" + materialTextBox21.Text + ":" + materialTextBox22.Text + "", true, null, c);
                    }
                    else
                    {
                        wc.Proxy = new WebProxy("" + materialTextBox21.Text + ":" + materialTextBox22.Text + "");
                    }

                    Uri u = new Uri("https://web.whatsapp.com/");
                    await wc.DownloadStringTaskAsync(u);
                    label1.Text = Strings.Status + " : " + Strings.OK;
                    return true;
                }
                catch (Exception ex)
                {
                    string ss = ex.Message;
                    Utils.showError(ex.Message);
                    label1.Text = Strings.Status + " : " + Strings.Error;
                    return false;
                }

            }
            return false;
        }

        private async void materialButton2_Click(object sender, EventArgs e)
        {
            materialButton2.Enabled = false;
            await testConnection();
            materialButton2.Enabled = true;
        }

        private void saveProxySettings()
        {
            string Proxy_Server = materialTextBox21.Text;
            string Proxy_Port = materialTextBox22.Text;
            string Proxy_UserName = materialTextBox23.Text;
            string Proxy_Password = materialTextBox24.Text;
            int Proxy_Enabled = 0;

            if (materialCheckbox1.Checked)
                Proxy_Enabled = 1;

            try
            {
                new SqLiteBaseRepository().setProxyDetails(this.sessionId, Proxy_Server, Proxy_Port, Proxy_UserName, Proxy_Password, Proxy_Enabled);

                MaterialSnackBar SnackBarMessage1 = new MaterialSnackBar(Strings.ProxySettingsaresavedSuccessfully, Strings.OK, true);
                SnackBarMessage1.Show(this);
            }
            catch (Exception ex)
            {
                Utils.showError(ex.Message);
            }
        }



        private async void materialButton1_Click(object sender, EventArgs e)
        {
            if (materialCheckbox1.Checked)
            {
                materialButton1.Enabled = false;
                if (await testConnection())
                {
                    saveProxySettings();
                }
                else
                {
                    materialCheckbox1.Checked = false;
                }
                materialButton1.Enabled = true;
            }
            else
            {
                saveProxySettings();
            }
        }
    }
}

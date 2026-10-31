using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO;
using WASender.Model;
using Microsoft.Web.WebView2.Core;

namespace WASender
{
    public partial class MainUC : UserControl
    {
        string profileName;
        string ProfileId;
        public bool _isWPPIJected { get; set; }
        public bool _isFirstMessage { get; set; }


        ProxyModel proxyModel;

        public MainUC(string _ProfileId, ProxyModel _proxyModel)
        {
            ProfileId = _ProfileId;
            proxyModel = _proxyModel;
            InitializeComponent();
            CreateHandle();
        }

        private void MainUC_Load(object sender, EventArgs e)
        {
            try
            {
                string ProfilesFolderPath = Config.GetProfilesFolderPath();   
                profileName = ProfilesFolderPath + "\\" + ProfileId;
                if (!Directory.Exists(profileName))
                {
                    Directory.CreateDirectory(profileName);
                }
            }
            catch (Exception ex)
            {
                if (ex.Message.Contains("compatible Webview2 Runtime"))
                {
                    MessageBox.Show(
                    Strings.YourComputerdonthaveCompatiblewebviewinstallation,
                    Strings.Error,
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information,
                    MessageBoxDefaultButton.Button1,
                    0,
                    "https://developer.microsoft.com/en-us/microsoft-edge/webview2/consumer/",
                    "");
                }
                else
                {
                    MessageBox.Show(ex.Message, "Error");
                }
            }
            InitBrowser();
        }

        public async void InitBrowser()
        {
            await initizated();      
            webView21.CoreWebView2.Navigate("https://web.whatsapp.com/");
        }

        private async Task initizated()
        {
            try
            {
                if (this.proxyModel != null)
                {
                    CoreWebView2EnvironmentOptions options = new CoreWebView2EnvironmentOptions();
                    options.AdditionalBrowserArguments = "--proxy-server=" + proxyModel.Proxy_Server + ":" + proxyModel.Proxy_Port + "";

                    CoreWebView2Environment envt = await CoreWebView2Environment.CreateAsync(null, profileName, options);
                    await webView21.EnsureCoreWebView2Async(envt);

                    webView21.CoreWebView2.BasicAuthenticationRequested += CoreWebView2_BasicAuthenticationRequested;
                }
                else
                {
                    webView21.CreationProperties = new Microsoft.Web.WebView2.WinForms.CoreWebView2CreationProperties();
                    webView21.CreationProperties.UserDataFolder = profileName;
                    await webView21.EnsureCoreWebView2Async(null);
                }

                
            }
            catch (Exception ex)
            {
                if (ex.Message.Contains("compatible Webview2 Runtime"))
                {
                    MessageBox.Show(
                    Strings.YourComputerdonthaveCompatiblewebviewinstallation,
                    Strings.Error,
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information,
                    MessageBoxDefaultButton.Button1,
                    0,
                    "https://developer.microsoft.com/en-us/microsoft-edge/webview2/consumer/",
                    "");
                }
                else
                {
                    MessageBox.Show(ex.Message, "Error");
                }
                
            }
        }

        private void CoreWebView2_BasicAuthenticationRequested(object sender, CoreWebView2BasicAuthenticationRequestedEventArgs e)
        {
            {
                e.Response.UserName = this.proxyModel.Proxy_UserName;
                e.Response.Password = this.proxyModel.Proxy_Password;
            }
        }

      

    }
}

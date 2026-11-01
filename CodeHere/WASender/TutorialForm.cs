using Microsoft.Web.WebView2.WinForms;
using System;
using System.IO;
using System.Windows.Forms;
using WaAutoReplyBot;

namespace WASender
{
    /// <summary>
    /// Panduan penggunaan (teks, offline penuh). Dibuka dari box Tutorial di dashboard.
    /// </summary>
    public partial class TutorialForm : MyMaterialPopOp
    {
        private WebView2 webView;

        public TutorialForm()
        {
            InitializeComponentManual();
            this.Icon = Strings.AppIcon;
            this.Text = "Tutorial WASender";
            LoadTutorial();
        }

        private void InitializeComponentManual()
        {
            this.SuspendLayout();
            this.ClientSize = new System.Drawing.Size(880, 640);
            this.StartPosition = FormStartPosition.CenterParent;
            this.MinimizeBox = false;

            webView = new WebView2();
            webView.Dock = DockStyle.Fill;
            this.Controls.Add(webView);

            var btnClose = new MaterialSkin.Controls.MaterialButton();
            btnClose.Text = "Tutup";
            btnClose.Dock = DockStyle.Bottom;
            btnClose.Height = 40;
            btnClose.Type = MaterialSkin.Controls.MaterialButton.MaterialButtonType.Contained;
            btnClose.UseAccentColor = false;
            btnClose.Click += (s, e) => { try { this.Close(); } catch (Exception) { } };
            this.Controls.Add(btnClose);
            this.ResumeLayout(false);
        }

        private async void LoadTutorial()
        {
            try
            {
                await webView.EnsureCoreWebView2Async();
                string path = Path.Combine(Directory.GetCurrentDirectory(), "HelpDocs", "Tutorial.html");
                if (!File.Exists(path))
                {
                    path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "HelpDocs", "Tutorial.html");
                }
                webView.Source = new Uri(path);
            }
            catch (Exception ex)
            {
                try { MessageBox.Show("Gagal membuka tutorial: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error); } catch (Exception) { }
            }
        }
    }
}

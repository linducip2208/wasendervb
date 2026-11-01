using MaterialSkin.Controls;
using Microsoft.Web.WebView2.WinForms;
using System;
using System.Web;
using System.Windows.Forms;
using WaAutoReplyBot;

namespace WASender
{
    /// <summary>
    /// Pratinjau pesan ala chat WhatsApp: hasil spintax + hitung karakter/segmen.
    /// Dibuka dari tombol Preview di editor pesan (AddMessage).
    /// </summary>
    public partial class MessagePreviewForm : MyMaterialPopOp
    {
        private readonly string _rawText;
        private readonly int _attachCount;
        private WebView2 webView;
        private MaterialLabel lblInfo;

        public MessagePreviewForm(string rawText, int attachCount = 0)
        {
            _rawText = rawText ?? "";
            _attachCount = attachCount;
            InitializeComponentManual();
            this.Icon = Strings.AppIcon;
            this.Text = "Pratinjau Pesan";
            Render(ProjectCommon.SpinText(_rawText));
        }

        private void InitializeComponentManual()
        {
            this.SuspendLayout();
            this.ClientSize = new System.Drawing.Size(420, 620);
            this.StartPosition = FormStartPosition.CenterParent;
            this.MaximizeBox = false;

            webView = new WebView2();
            webView.Dock = DockStyle.Fill;
            this.Controls.Add(webView);

            var bottom = new Panel();
            bottom.Dock = DockStyle.Bottom;
            bottom.Height = 96;
            bottom.Padding = new Padding(10, 4, 10, 10);
            bottom.BackColor = System.Drawing.Color.White;
            this.Controls.Add(bottom);

            lblInfo = new MaterialLabel();
            lblInfo.Dock = DockStyle.Top;
            lblInfo.Height = 24;
            bottom.Controls.Add(lblInfo);

            var btnSpin = new MaterialButton();
            btnSpin.Text = "Putar Lagi";
            btnSpin.Type = MaterialButton.MaterialButtonType.Outlined;
            btnSpin.UseAccentColor = false;
            btnSpin.Dock = DockStyle.Left;
            btnSpin.Width = 150;
            btnSpin.Click += (s, e) => { try { Render(ProjectCommon.SpinText(_rawText)); } catch (Exception) { } };
            bottom.Controls.Add(btnSpin);

            var btnClose = new MaterialButton();
            btnClose.Text = "Tutup";
            btnClose.Type = MaterialButton.MaterialButtonType.Contained;
            btnClose.UseAccentColor = false;
            btnClose.Dock = DockStyle.Right;
            btnClose.Width = 120;
            btnClose.Click += (s, e) => { try { this.Close(); } catch (Exception) { } };
            bottom.Controls.Add(btnClose);

            this.FormClosing += (s, e) => { try { webView.Dispose(); } catch (Exception) { } };
            this.ResumeLayout(false);
        }

        private static int Segments(string t)
        {
            try
            {
                if (string.IsNullOrEmpty(t)) return 0;
                bool uni = false;
                foreach (char c in t) { if (c > 127) { uni = true; break; } }
                int single = uni ? 70 : 160, multi = uni ? 67 : 153;
                if (t.Length <= single) return 1;
                return 1 + (int)System.Math.Ceiling((t.Length - single) / (double)multi);
            }
            catch (Exception) { return 1; }
        }

        private async void Render(string spun)
        {
            try
            {
                int segs = Segments(spun);
                bool hasSpin = (spun ?? "") != (_rawText ?? "");
                lblInfo.Text = (spun ?? "").Length + " karakter • ~" + segs + " segmen SMS" + (hasSpin ? " • spintax aktif" : "");
                string body = HttpUtility.HtmlEncode(spun ?? "").Replace("\r\n", "<br>").Replace("\n", "<br>");
                string attach = _attachCount > 0
                    ? "<div style='background:#fff;border-radius:8px;padding:8px 10px;margin:0 8px 4px 8px;font-size:13px;color:#54656f;'>&#128206; " + _attachCount + " lampiran</div>"
                    : "";
                string html = "<!DOCTYPE html><html><head><meta charset='utf-8'><meta name='viewport' content='width=device-width, initial-scale=1'>"
                    + "<style>body{margin:0;background:#0b141a;font-family:'Segoe UI',Arial,sans-serif;}"
                    + ".wa{max-width:360px;margin:0 auto;background:#0b141a url('') center;background-color:#0b141a;min-height:100vh;background-image:radial-gradient(rgba(255,255,255,.04) 1px,transparent 1px);background-size:18px 18px;}"
                    + ".hdr{background:#075E54;color:#fff;padding:12px 14px;font-size:15px;font-weight:600;position:sticky;top:0;}"
                    + ".hdr small{display:block;font-weight:400;font-size:12px;opacity:.85;}"
                    + ".day{text-align:center;margin:12px 0;}.day span{background:#182229;color:#8696a0;font-size:12px;padding:5px 12px;border-radius:8px;}"
                    + ".out{background:#005c4b;color:#e7ffdb;border-radius:8px 0 8px 8px;padding:8px 10px;margin:4px 14px 4px 60px;font-size:14px;line-height:1.45;box-shadow:0 1px 1px rgba(0,0,0,.4);}"
                    + ".meta{text-align:right;font-size:11px;color:#b3c6bd;margin-top:4px;}"
                    + "</style></head><body><div class='wa'>"
                    + "<div class='hdr'>Pratinjau<small>UI tidak mengirim — hanya tampilan</small></div>"
                    + "<div class='day'><span>HARI INI</span></div>"
                    + attach
                    + "<div class='out'>" + (string.IsNullOrEmpty(body) ? "<i style='color:#8696a0'>(pesan kosong)</i>" : body) + "<div class='meta'>09.45 &#10003;&#10003;</div></div>"
                    + "</div></body></html>";
                await webView.EnsureCoreWebView2Async();
                webView.NavigateToString(html);
            }
            catch (Exception ex)
            {
                try { MessageBox.Show("Gagal render pratinjau: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error); } catch (Exception) { }
            }
        }
    }
}

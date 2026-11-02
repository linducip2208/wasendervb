using MaterialSkin.Controls;
using System;
using System.Collections.Generic;
using System.Windows.Forms;
using WaAutoReplyBot;

namespace WASender
{
    /// <summary>
    /// Ringkasan kampanye sebelum dikirim: total kontak/template, jeda, limit harian,
    /// estimasi durasi + peringatan anti-ban. [Mulai Kirim] = lanjut, [Batal] = stop.
    /// Dipanggil dari ValidateControls/ValidateControlsGroup setelah validasi lolos.
    /// </summary>
    public partial class CampaignSummaryForm : MyMaterialPopOp
    {
        public CampaignSummaryForm(string kind, int contacts, int templates, string delayText,
            string limitText, string estimateText, List<string> warnings)
        {
            InitializeComponentManual();
            this.Icon = Strings.AppIcon;
            this.Text = "Ringkasan Campaign " + kind;
            try
            {
                lblKind.Text = "Kampanye " + kind;
                lblContacts.Text = contacts.ToString("N0") + " kontak";
                lblTemplates.Text = templates + " template pesan";
                lblDelay.Text = delayText ?? "-";
                lblLimit.Text = limitText ?? "-";
                lblEstimate.Text = estimateText ?? "-";
                if (warnings != null && warnings.Count > 0)
                    lblWarn.Text = string.Join("\n", warnings.ToArray());
                else
                    lblWarn.Text = "Siap dikirim. Pastikan nomor sudah difilter & pesan memakai spintax.";
            }
            catch (Exception) { }
        }

        private MaterialLabel lblKind;
        private MaterialLabel lblContacts;
        private MaterialLabel lblTemplates;
        private MaterialLabel lblDelay;
        private MaterialLabel lblLimit;
        private MaterialLabel lblEstimate;
        private MaterialLabel lblWarn;

        private void InitializeComponentManual()
        {
            this.SuspendLayout();
            this.ClientSize = new System.Drawing.Size(460, 430);
            this.StartPosition = FormStartPosition.CenterParent;
            this.MaximizeBox = false;
            this.MinimizeBox = false;

            var card = new MaterialCard();
            card.Dock = DockStyle.Fill;
            card.Padding = new Padding(16);
            this.Controls.Add(card);

            lblKind = NewRow(card, "Kampanye", 0);
            lblContacts = NewRow(card, "Target", 1);
            lblTemplates = NewRow(card, "Template", 2);
            lblDelay = NewRow(card, "Jeda", 3);
            lblLimit = NewRow(card, "Limit harian", 4);
            lblEstimate = NewRow(card, "Estimasi", 5);
            lblWarn = NewRow(card, "Catatan", 6);

            var pnl = new Panel();
            pnl.Dock = DockStyle.Bottom;
            pnl.Height = 48;
            card.Controls.Add(pnl);

            var btnCancel = new MaterialButton();
            btnCancel.Text = "Batal";
            btnCancel.Type = MaterialButton.MaterialButtonType.Outlined;
            btnCancel.UseAccentColor = false;
            btnCancel.Dock = DockStyle.Right;
            btnCancel.Width = 120;
            btnCancel.DialogResult = DialogResult.Cancel;
            pnl.Controls.Add(btnCancel);

            var btnGo = new MaterialButton();
            btnGo.Text = "Mulai Kirim";
            btnGo.Type = MaterialButton.MaterialButtonType.Contained;
            btnGo.UseAccentColor = false;
            btnGo.Dock = DockStyle.Right;
            btnGo.Width = 150;
            btnGo.DialogResult = DialogResult.OK;
            pnl.Controls.Add(btnGo);

            this.AcceptButton = btnGo;
            this.CancelButton = btnCancel;
            this.ResumeLayout(false);
        }

        private MaterialLabel NewRow(Control parent, string caption, int idx)
        {
            var cap = new MaterialLabel();
            cap.Text = caption + ":";
            cap.FontType = MaterialSkin.MaterialSkinManager.fontType.Subtitle2;
            cap.Top = 16 + idx * 44;
            cap.Left = 16;
            cap.Width = 110;
            parent.Controls.Add(cap);
            var val = new MaterialLabel();
            val.Top = 16 + idx * 44;
            val.Left = 130;
            val.Width = 290;
            val.FontType = idx == 6 ? MaterialSkin.MaterialSkinManager.fontType.Body2 : MaterialSkin.MaterialSkinManager.fontType.H6;
            parent.Controls.Add(val);
            return val;
        }
    }
}

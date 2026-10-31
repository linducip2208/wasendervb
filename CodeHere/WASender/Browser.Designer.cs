namespace WASender
{
    partial class Browser
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.materialCard1 = new MaterialSkin.Controls.MaterialCard();
            this.addressBar = new MaterialSkin.Controls.MaterialTextBox2();
            this.refresh = new MaterialSkin.Controls.MaterialButton();
            this.next = new MaterialSkin.Controls.MaterialButton();
            this.back = new MaterialSkin.Controls.MaterialButton();
            this.webView = new Microsoft.Web.WebView2.WinForms.WebView2();
            this.materialLabel1 = new MaterialSkin.Controls.MaterialLabel();
            this.materialCard1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.webView)).BeginInit();
            this.SuspendLayout();
            // 
            // materialCard1
            // 
            this.materialCard1.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.materialCard1.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(255)))), ((int)(((byte)(255)))), ((int)(((byte)(255)))));
            this.materialCard1.Controls.Add(this.materialLabel1);
            this.materialCard1.Controls.Add(this.addressBar);
            this.materialCard1.Controls.Add(this.refresh);
            this.materialCard1.Controls.Add(this.next);
            this.materialCard1.Controls.Add(this.back);
            this.materialCard1.Controls.Add(this.webView);
            this.materialCard1.Depth = 0;
            this.materialCard1.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(222)))), ((int)(((byte)(0)))), ((int)(((byte)(0)))), ((int)(((byte)(0)))));
            this.materialCard1.Location = new System.Drawing.Point(7, 92);
            this.materialCard1.Margin = new System.Windows.Forms.Padding(14);
            this.materialCard1.MouseState = MaterialSkin.MouseState.HOVER;
            this.materialCard1.Name = "materialCard1";
            this.materialCard1.Padding = new System.Windows.Forms.Padding(14);
            this.materialCard1.Size = new System.Drawing.Size(1026, 669);
            this.materialCard1.TabIndex = 0;
            // 
            // addressBar
            // 
            this.addressBar.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.addressBar.AnimateReadOnly = false;
            this.addressBar.BackgroundImageLayout = System.Windows.Forms.ImageLayout.None;
            this.addressBar.CharacterCasing = System.Windows.Forms.CharacterCasing.Normal;
            this.addressBar.Depth = 0;
            this.addressBar.Font = new System.Drawing.Font("Microsoft Sans Serif", 16F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Pixel);
            this.addressBar.HideSelection = true;
            this.addressBar.LeadingIcon = null;
            this.addressBar.Location = new System.Drawing.Point(186, 7);
            this.addressBar.MaxLength = 32767;
            this.addressBar.MouseState = MaterialSkin.MouseState.OUT;
            this.addressBar.Name = "addressBar";
            this.addressBar.PasswordChar = '\0';
            this.addressBar.PrefixSuffixText = null;
            this.addressBar.ReadOnly = false;
            this.addressBar.RightToLeft = System.Windows.Forms.RightToLeft.No;
            this.addressBar.SelectedText = "";
            this.addressBar.SelectionLength = 0;
            this.addressBar.SelectionStart = 0;
            this.addressBar.ShortcutsEnabled = true;
            this.addressBar.Size = new System.Drawing.Size(823, 48);
            this.addressBar.TabIndex = 5;
            this.addressBar.TabStop = false;
            this.addressBar.TextAlign = System.Windows.Forms.HorizontalAlignment.Left;
            this.addressBar.TrailingIcon = null;
            this.addressBar.UseSystemPasswordChar = false;
            this.addressBar.KeyDown += new System.Windows.Forms.KeyEventHandler(this.addressBar_KeyDown);
            // 
            // refresh
            // 
            this.refresh.AutoSize = false;
            this.refresh.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.refresh.Density = MaterialSkin.Controls.MaterialButton.MaterialButtonDensity.Default;
            this.refresh.Depth = 0;
            this.refresh.HighEmphasis = true;
            this.refresh.Icon = global::WASender.Properties.Resources.icons8_refresh_24px;
            this.refresh.Location = new System.Drawing.Point(121, 11);
            this.refresh.Margin = new System.Windows.Forms.Padding(4, 6, 4, 6);
            this.refresh.MouseState = MaterialSkin.MouseState.HOVER;
            this.refresh.Name = "refresh";
            this.refresh.NoAccentTextColor = System.Drawing.Color.Empty;
            this.refresh.Size = new System.Drawing.Size(50, 50);
            this.refresh.TabIndex = 4;
            this.refresh.Type = MaterialSkin.Controls.MaterialButton.MaterialButtonType.Contained;
            this.refresh.UseAccentColor = false;
            this.refresh.UseVisualStyleBackColor = true;
            this.refresh.Click += new System.EventHandler(this.refresh_Click);
            // 
            // next
            // 
            this.next.AutoSize = false;
            this.next.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.next.Density = MaterialSkin.Controls.MaterialButton.MaterialButtonDensity.Default;
            this.next.Depth = 0;
            this.next.HighEmphasis = true;
            this.next.Icon = global::WASender.Properties.Resources.icons8_fwd_24px;
            this.next.Location = new System.Drawing.Point(67, 11);
            this.next.Margin = new System.Windows.Forms.Padding(4, 6, 4, 6);
            this.next.MouseState = MaterialSkin.MouseState.HOVER;
            this.next.Name = "next";
            this.next.NoAccentTextColor = System.Drawing.Color.Empty;
            this.next.Size = new System.Drawing.Size(50, 50);
            this.next.TabIndex = 3;
            this.next.Type = MaterialSkin.Controls.MaterialButton.MaterialButtonType.Contained;
            this.next.UseAccentColor = false;
            this.next.UseVisualStyleBackColor = true;
            this.next.Click += new System.EventHandler(this.next_Click);
            // 
            // back
            // 
            this.back.AutoSize = false;
            this.back.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.back.Density = MaterialSkin.Controls.MaterialButton.MaterialButtonDensity.Default;
            this.back.Depth = 0;
            this.back.HighEmphasis = true;
            this.back.Icon = global::WASender.Properties.Resources.icons8_back_24px;
            this.back.Location = new System.Drawing.Point(14, 11);
            this.back.Margin = new System.Windows.Forms.Padding(4, 6, 4, 6);
            this.back.MouseState = MaterialSkin.MouseState.HOVER;
            this.back.Name = "back";
            this.back.NoAccentTextColor = System.Drawing.Color.Empty;
            this.back.Size = new System.Drawing.Size(50, 50);
            this.back.TabIndex = 2;
            this.back.Type = MaterialSkin.Controls.MaterialButton.MaterialButtonType.Contained;
            this.back.UseAccentColor = false;
            this.back.UseVisualStyleBackColor = true;
            this.back.Click += new System.EventHandler(this.back_Click);
            // 
            // webView
            // 
            this.webView.AllowExternalDrop = true;
            this.webView.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.webView.CreationProperties = null;
            this.webView.DefaultBackgroundColor = System.Drawing.Color.White;
            this.webView.Location = new System.Drawing.Point(14, 67);
            this.webView.Name = "webView";
            this.webView.Size = new System.Drawing.Size(998, 585);
            this.webView.TabIndex = 1;
            this.webView.ZoomFactor = 1D;
            // 
            // materialLabel1
            // 
            this.materialLabel1.AutoSize = true;
            this.materialLabel1.Depth = 0;
            this.materialLabel1.Font = new System.Drawing.Font("Roboto", 14F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Pixel);
            this.materialLabel1.Location = new System.Drawing.Point(18, 71);
            this.materialLabel1.MouseState = MaterialSkin.MouseState.HOVER;
            this.materialLabel1.Name = "materialLabel1";
            this.materialLabel1.Size = new System.Drawing.Size(107, 19);
            this.materialLabel1.TabIndex = 6;
            this.materialLabel1.Text = "materialLabel1";
            // 
            // Browser
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 16F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1040, 778);
            this.Controls.Add(this.materialCard1);
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "Browser";
            this.Text = "Browser";
            this.FormClosed += new System.Windows.Forms.FormClosedEventHandler(this.Browser_FormClosed);
            this.Load += new System.EventHandler(this.Browser_Load);
            this.materialCard1.ResumeLayout(false);
            this.materialCard1.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.webView)).EndInit();
            this.ResumeLayout(false);

        }


        #endregion

        private MaterialSkin.Controls.MaterialCard materialCard1;
        private Microsoft.Web.WebView2.WinForms.WebView2 webView;
        private MaterialSkin.Controls.MaterialButton back;
        private MaterialSkin.Controls.MaterialButton next;
        private MaterialSkin.Controls.MaterialButton refresh;
        private MaterialSkin.Controls.MaterialTextBox2 addressBar;
        private MaterialSkin.Controls.MaterialLabel materialLabel1;
    }
}
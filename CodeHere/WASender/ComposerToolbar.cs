using MaterialSkin.Controls;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace WASender
{
    /// <summary>
    /// Toolbar composer (B / I / emoji / variabel / hitung karakter) yang dipasang
    /// terprogram di atas tiap kotak pesan (tab Pesan 1..5 single &amp; grup).
    /// Tanpa mengubah file Designer — aman terhadap layout designer.
    /// </summary>
    public static class ComposerToolbar
    {
        private static readonly HashSet<Control> _attached = new HashSet<Control>();
        private static readonly string[] Emojis = new string[]
        {
            "😀", "😁", "😂", "🤣", "😊", "😍", "👍", "🙏", "🎉", "🎁",
            "💰", "🔥", "⭐", "✅", "📢", "💥", "🎊", "👋", "🙏", "💯"
        };

        public static void AttachAll(Control root)
        {
            try
            {
                foreach (var tp in AllTabs(root))
                {
                    try
                    {
                        var box = tp.Controls.OfType<MaterialMultiLineTextBox2>()
                            .FirstOrDefault(b => b.Name != null && b.Name.StartsWith("txtMsg"));
                        if (box == null || _attached.Contains(box)) continue;
                        _attached.Add(box);
                        AttachTo(tp, box);
                    }
                    catch (Exception) { }
                }
            }
            catch (Exception) { }
        }

        private static IEnumerable<TabPage> AllTabs(Control root)
        {
            var stack = new Stack<Control>();
            stack.Push(root);
            while (stack.Count > 0)
            {
                var c = stack.Pop();
                TabControl tc = c as TabControl;
                if (tc != null)
                {
                    foreach (TabPage p in tc.TabPages) yield return p;
                }
                foreach (Control ch in c.Controls) stack.Push(ch);
            }
        }

        private static void AttachTo(TabPage page, MaterialMultiLineTextBox2 box)
        {
            try
            {
                var bar = new Panel();
                bar.Height = 32;
                bar.Left = box.Left;
                bar.Top = box.Top;
                bar.Width = box.Width;
                bar.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
                bar.BackColor = Color.Transparent;

                int x = 0;
                Button b = NewBtn("B", true, false);
                b.Click += (s, e) => WrapSelection(box, "**", "**");
                Button bi = NewBtn("I", false, true);
                bi.Click += (s, e) => WrapSelection(box, "__", "__");

                var bEmo = NewBtn("😊", false, false);
                bEmo.Width = 40;
                var cms = new ContextMenuStrip();
                foreach (var em in Emojis)
                {
                    var mi = new ToolStripMenuItem(em + "  sisipkan");
                    mi.Tag = em;
                    mi.Click += (s, e) =>
                    {
                        try
                        {
                            string ins = Convert.ToString(((ToolStripMenuItem)s).Tag);
                            box.Focus();
                            box.SelectedText = ins;
                        }
                        catch (Exception) { }
                    };
                    cms.Items.Add(mi);
                }
                bEmo.Click += (s, e) => { try { cms.Show(bEmo, new Point(0, bEmo.Height)); } catch (Exception) { } };

                var cmb = new ComboBox();
                cmb.DropDownStyle = ComboBoxStyle.DropDownList;
                cmb.Width = 120;
                cmb.Items.Add("Variabel...");
                cmb.Items.Add("[NAME]");
                cmb.Items.Add("[SPOILER]");
                cmb.Items.Add("{{nama}}");
                cmb.SelectedIndex = 0;
                cmb.SelectedIndexChanged += (s, e) =>
                {
                    try
                    {
                        if (cmb.SelectedIndex <= 0) return;
                        string ins = Convert.ToString(cmb.SelectedItem);
                        box.Focus();
                        box.SelectedText = ins;
                    }
                    catch (Exception) { }
                    finally { try { cmb.SelectedIndex = 0; } catch (Exception) { } }
                };

                var lbl = new Label();
                lbl.AutoSize = false;
                lbl.TextAlign = ContentAlignment.MiddleRight;
                lbl.ForeColor = Theme.TextSecondary;
                lbl.Font = new Font("Segoe UI", 9F);
                lbl.Width = 260;

                Action layout = null;
                layout = () =>
                {
                    try
                    {
                        b.SetBounds(0, 2, 34, 28);
                        bi.SetBounds(38, 2, 34, 28);
                        bEmo.SetBounds(76, 2, 40, 28);
                        cmb.SetBounds(120, 3, 120, 26);
                        lbl.SetBounds(bar.Width - 264, 2, 260, 28);
                    }
                    catch (Exception) { }
                };
                bar.Resize += (s, e) => { try { layout(); } catch (Exception) { } };

                bar.Controls.Add(b);
                bar.Controls.Add(bi);
                bar.Controls.Add(bEmo);
                bar.Controls.Add(cmb);
                bar.Controls.Add(lbl);
                layout();

                EventHandler upd = null;
                upd = (s, e) =>
                {
                    try
                    {
                        string t = box.Text ?? "";
                        int segs = 1;
                        if (t.Length > 0)
                        {
                            bool uni = false;
                            foreach (char c in t) { if (c > 127) { uni = true; break; } }
                            int first = uni ? 70 : 160, rest = uni ? 67 : 153;
                            segs = t.Length <= first ? 1 : 1 + (int)Math.Ceiling((t.Length - first) / (double)rest);
                        }
                        lbl.Text = t.Length + " karakter • ~" + segs + " segmen";
                    }
                    catch (Exception) { }
                };
                box.TextChanged += upd;
                upd(null, EventArgs.Empty);

                box.Top = box.Top + 36;
                box.Height = Math.Max(60, box.Height - 36);
                page.Controls.Add(bar);
                bar.BringToFront();
            }
            catch (Exception) { }
        }

        private static Button NewBtn(string text, bool bold, bool italic)
        {
            var b = new Button();
            b.Text = text;
            b.Width = 34;
            b.Height = 28;
            b.FlatStyle = FlatStyle.Flat;
            b.FlatAppearance.BorderColor = Theme.CardBorder;
            b.BackColor = Color.White;
            b.ForeColor = Theme.TextPrimary;
            FontStyle st = FontStyle.Regular;
            if (bold) st |= FontStyle.Bold;
            if (italic) st |= FontStyle.Italic;
            try { b.Font = new Font("Segoe UI", 10F, st); } catch (Exception) { }
            b.Cursor = Cursors.Hand;
            return b;
        }

        private static void WrapSelection(MaterialMultiLineTextBox2 box, string open, string close)
        {
            try
            {
                box.Focus();
                string sel = "";
                try { sel = box.SelectedText ?? ""; } catch (Exception) { }
                if (string.IsNullOrEmpty(sel))
                {
                    int pos = 0;
                    try { pos = box.SelectionStart; } catch (Exception) { }
                    string t = box.Text ?? "";
                    box.Text = t.Substring(0, pos) + open + close + t.Substring(pos);
                    try { box.SelectionStart = pos + open.Length; } catch (Exception) { }
                }
                else
                {
                    box.SelectedText = open + sel + close;
                }
            }
            catch (Exception) { }
        }
    }
}

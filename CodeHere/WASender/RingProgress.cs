using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace WASender
{
    /// <summary>
    /// Indikator progres donat: persen penuh saat target tercapai, mode putar saat tak terbatas.
    /// Murni visual, digambar manual (tanpa resource gambar).
    /// </summary>
    public class RingProgress : Control
    {
        private int _value;
        private int _maximum = 100;
        private bool _spinning;
        private int _angle;
        private Timer _anim;

        public RingProgress()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
            Size = new Size(78, 78);
            _anim = new Timer();
            _anim.Interval = 90;
            _anim.Tick += (s, e) => { try { _angle = (_angle + 12) % 360; Invalidate(); } catch (Exception) { } };
        }

        public int Value
        {
            get { return _value; }
            set { _value = Math.Max(0, Math.Min(100, value)); try { Invalidate(); } catch (Exception) { } }
        }

        public int Maximum2
        {
            get { return _maximum; }
            set { _maximum = Math.Max(1, value); try { Invalidate(); } catch (Exception) { } }
        }

        public bool Spinning
        {
            get { return _spinning; }
            set
            {
                _spinning = value;
                try
                {
                    if (value) { if (!_anim.Enabled) _anim.Start(); }
                    else _anim.Stop();
                    Invalidate();
                }
                catch (Exception) { }
            }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            try
            {
                base.OnPaint(e);
                Graphics g = e.Graphics;
                g.SmoothingMode = SmoothingMode.AntiAlias;
                int pad = 8;
                Rectangle r = new Rectangle(pad, pad, Math.Max(10, Width - pad * 2), Math.Max(10, Height - pad * 2));
                using (var bg = new Pen(Color.FromArgb(0xE2, 0xE8, 0xF0), 9F))
                {
                    g.DrawArc(bg, r, 0, 360);
                }
                using (var fg = new Pen(Theme.Primary, 9F))
                {
                    fg.StartCap = LineCap.Round;
                    fg.EndCap = LineCap.Round;
                    if (_spinning)
                    {
                        g.DrawArc(fg, r, _angle, 90);
                    }
                    else
                    {
                        float sweep = (float)_value / 100f * 360f;
                        if (sweep > 0) g.DrawArc(fg, r, -90, sweep);
                    }
                }
                string txt = _spinning ? "…" : _value + "%";
                using (var f = new Font("Segoe UI", 11F, FontStyle.Bold))
                using (var b = new SolidBrush(Theme.TextPrimary))
                {
                    var sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
                    g.DrawString(txt, f, b, new RectangleF(0, 0, Width, Height), sf);
                }
            }
            catch (Exception) { }
        }

        protected override void Dispose(bool disposing)
        {
            try { if (disposing && _anim != null) { _anim.Stop(); _anim.Dispose(); } } catch (Exception) { }
            base.Dispose(disposing);
        }
    }
}

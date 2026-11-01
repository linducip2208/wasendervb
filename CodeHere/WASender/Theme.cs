using System.Drawing;

namespace WASender
{
    /// <summary>
    /// Design token terpusat WASender (Modern Enterprise).
    /// Satu sumber kebenaran utk warna, radius, dan spacing agar semua form konsisten.
    /// Dipakai bertahap oleh form-form; tidak mengubah logic apa pun.
    /// </summary>
    public static class Theme
    {
        // Primary: emerald ala WhatsApp
        public static readonly Color Primary = Color.FromArgb(0x00, 0x96, 0x88);
        public static readonly Color PrimaryDark = Color.FromArgb(0x00, 0x77, 0x6E);
        public static readonly Color PrimaryLight = Color.FromArgb(0xE0, 0xF2, 0xF1);

        // Sidebar: deep navy
        public static readonly Color Navy = Color.FromArgb(0x1A, 0x23, 0x38);
        public static readonly Color NavyLight = Color.FromArgb(0x24, 0x30, 0x4D);

        // Background & surfaces
        public static readonly Color Background = Color.FromArgb(0xF2, 0xF4, 0xF7);
        public static readonly Color Card = Color.White;
        public static readonly Color CardBorder = Color.FromArgb(0xE2, 0xE8, 0xF0);

        // Teks
        public static readonly Color TextPrimary = Color.FromArgb(0x1A, 0x23, 0x38);
        public static readonly Color TextSecondary = Color.FromArgb(0x64, 0x74, 0x8B);

        // Status
        public static readonly Color Success = Color.FromArgb(0x00, 0x96, 0x88);
        public static readonly Color Warning = Color.FromArgb(0xF5, 0x9E, 0x0B);
        public static readonly Color Error = Color.FromArgb(0xDC, 0x26, 0x26);
        public static readonly Color Info = Color.FromArgb(0x25, 0x63, 0xEB);

        // Status lisensi/kesehatan -> warna
        public static Color HealthColor(int score)
        {
            if (score < 0) return TextSecondary;
            if (score < 40) return Error;
            if (score < 70) return Warning;
            return Success;
        }

        public static Color DaysLeftColor(int days)
        {
            if (days <= 7) return Error;
            if (days <= 30) return Warning;
            return Success;
        }
    }
}

using System.Drawing;
using System.Windows.Forms;

namespace WASender
{
    /// <summary>
    /// Satu gaya grid untuk seluruh aplikasi: header emerald gelap + teks putih,
    /// baris belang lembut, seleksi hijau muda. Dipanggil sekali di init/Load tiap form.
    /// Murni kosmetik — tidak mengubah kolom, data, maupun event.
    /// </summary>
    public static class GridTheme
    {
        private static Font _headerFont;
        private static Font _cellFont;

        private static Font HeaderFont()
        {
            if (_headerFont == null) { try { _headerFont = new Font("Segoe UI", 9F, FontStyle.Bold); } catch (System.Exception) { _headerFont = Control.DefaultFont; } }
            return _headerFont;
        }

        private static Font CellFont()
        {
            if (_cellFont == null) { try { _cellFont = new Font("Segoe UI", 9F, FontStyle.Regular); } catch (System.Exception) { _cellFont = Control.DefaultFont; } }
            return _cellFont;
        }

        public static void Apply(DataGridView grid)
        {
            if (grid == null) return;
            try
            {
                grid.EnableHeadersVisualStyles = false;
                grid.BorderStyle = BorderStyle.FixedSingle;
                grid.GridColor = Color.FromArgb(0xE2, 0xE8, 0xF0);
                grid.BackgroundColor = Color.White;
                grid.RowHeadersVisible = false;
                grid.RowTemplate.Height = 26;

                var head = grid.ColumnHeadersDefaultCellStyle;
                head.BackColor = Theme.PrimaryDark;
                head.ForeColor = Color.White;
                head.Font = HeaderFont();
                head.Alignment = DataGridViewContentAlignment.MiddleLeft;
                head.Padding = new Padding(4, 0, 4, 0);

                var alt = grid.AlternatingRowsDefaultCellStyle;
                alt.BackColor = Color.FromArgb(0xF2, 0xF7, 0xF7);

                var cell = grid.DefaultCellStyle;
                cell.Font = CellFont();
                cell.ForeColor = Theme.TextPrimary;
                cell.SelectionBackColor = Color.FromArgb(0xC5, 0xCA, 0xE9);
                cell.SelectionForeColor = Theme.TextPrimary;
            }
            catch (System.Exception) { }
        }

        public static void ApplyAll(params DataGridView[] grids)
        {
            if (grids == null) return;
            foreach (var g in grids) Apply(g);
        }
    }
}

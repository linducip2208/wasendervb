using System;
using System.Windows.Forms;

namespace WASender
{
    /// <summary>
    /// Satu slot akun: ganti 1 dari 10 copy-paste di WaSenderForm.Designer.
    /// Tahap 1: wadah DataGridView + browser preview. Logika kirim tetap di WaSenderForm,
    /// migrasi bertahap per event (DocumentCompleted/MouseDown diparameterisasi Slot).
    /// </summary>
    public partial class AccountSessionControl : UserControl
    {
        public int Slot { get; set; }
        public WASender.Model.AccountState State { get; set; }

        public DataGridView Grid { get; private set; }
        public WebBrowser Preview { get; private set; }

        public event EventHandler<int> GridClicked;

        public AccountSessionControl()
        {
            InitializeComponentManual();
        }

        public AccountSessionControl(int slot) : this()
        {
            Slot = slot;
            State = new WASender.Model.AccountState { Slot = slot };
        }

        private void InitializeComponentManual()
        {
            this.Grid = new DataGridView();
            this.Preview = new WebBrowser();
            this.SuspendLayout();
            this.Grid.Dock = DockStyle.Fill;
            this.Grid.ReadOnly = true;
            this.Grid.AllowUserToAddRows = false;
            this.Grid.Columns.Add("c0", "To");
            this.Grid.Columns.Add("c1", "Message");
            this.Preview.Dock = DockStyle.Bottom;
            this.Preview.Height = 220;
            this.Preview.ScriptErrorsSuppressed = true;
            this.Controls.Add(this.Grid);
            this.Controls.Add(this.Preview);
            this.Grid.MouseClick += (s, e) => { try { GridClicked?.Invoke(s, Slot); } catch (Exception) { } };
            this.ResumeLayout(false);
        }

        public void Bind(WASender.Model.AccountState state)
        {
            State = state;
            Slot = state?.Slot ?? Slot;
            RefreshGrid();
        }

        public void RefreshGrid()
        {
            try
            {
                Grid.Rows.Clear();
                if (State?.Buttons == null) return;
                foreach (var b in State.Buttons)
                {
                    try { Grid.Rows.Add(b?.title ?? "", ""); } catch (Exception) { }
                }
            }
            catch (Exception) { }
        }
    }
}

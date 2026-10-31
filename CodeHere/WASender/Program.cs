using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace WASender
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main(string[] args)
        {
            bool _new = false;
            try
            {
                using (var _m = new System.Threading.Mutex(true, "WASenderSingleInstance", out _new))
                {
                    if (!_new) { MessageBox.Show("WASender sudah berjalan.", "WASender"); return; }
                    RunApp(args);
                    System.GC.KeepAlive(_m);
                }
            }
            catch (Exception) { RunApp(args); }
        }

        static void RunApp(string[] args)
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new WaSenderForm(args));

            //Application.Run(new GroupLinkVerify());

        }
    }
}


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
            // Jaring pengaman global: ganti dialog JIT seram dgn pesan ramah + catat ke crash.log.
            try { Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException); } catch (Exception) { }
            try
            {
                Application.ThreadException += (s, e) =>
                {
                    try
                    {
                        CrashLog(e.Exception);
                        MessageBox.Show("Terjadi kesalahan tak terduga, aplikasi akan ditutup.\n\nDetail: " + (e.Exception == null ? "-" : e.Exception.Message) + "\n\nLog tersimpan di crash.log.", "WASender", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                    catch (Exception) { }
                    try { Application.Exit(); } catch (Exception) { }
                };
            }
            catch (Exception) { }
            try
            {
                AppDomain.CurrentDomain.UnhandledException += (s, e) =>
                {
                    try { CrashLog(e.ExceptionObject as Exception); } catch (Exception) { }
                };
            }
            catch (Exception) { }
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

        static void CrashLog(Exception ex)
        {
            try
            {
                string dir = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "WaSender");
                if (!System.IO.Directory.Exists(dir)) System.IO.Directory.CreateDirectory(dir);
                System.IO.File.AppendAllText(System.IO.Path.Combine(dir, "crash.log"),
                    DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + " " + (ex == null ? "-" : ex.ToString()) + Environment.NewLine);
            }
            catch (Exception) { }
        }

        static void RunApp(string[] args)
        {
            try { Config.OnlineActiveCheck = () => LicenseServer.IsActiveLocal(); } catch (Exception) { }
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new ShellHost(args));

            //Application.Run(new GroupLinkVerify());

        }
    }
}


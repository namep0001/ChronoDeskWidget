using System;
using System.Windows.Forms;

namespace ChronoDeskWidget
{
    internal static class Program
    {
        [STAThread]
        private static void Main(string[] args)
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            bool startHidden = false;
            if (args != null)
            {
                foreach (string arg in args)
                {
                    if (string.Equals(arg, "--tray", StringComparison.OrdinalIgnoreCase))
                        startHidden = true;
                }
            }

            try
            {
                MainForm form = new MainForm(startHidden);
                Application.Run(form);
            }
            catch (Exception ex)
            {
                try { System.IO.File.AppendAllText(LocalStore.ErrorPath, DateTime.Now.ToString("s") + "\r\n" + ex + "\r\n\r\n"); }
                catch { }
                MessageBox.Show("ChronoDesk Widget could not start. Details were written to the local error log.\r\n\r\n" + ex.Message,
                    "ChronoDesk Widget", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}
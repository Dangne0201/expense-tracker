using System;
using System.Windows.Forms;

namespace ExpenseTracker.WinForms
{
    internal static class Program
    {
        [STAThread]
        static void Main()
        {
            // If a local connection file is present next to the exe, use it to provide SQL_CONN
            try
            {
                var exeDir = AppContext.BaseDirectory;
                var localConnFile = System.IO.Path.Combine(exeDir, "sqlconn.txt");
                if (System.IO.File.Exists(localConnFile))
                {
                    var conn = System.IO.File.ReadAllText(localConnFile).Trim();
                    if (!string.IsNullOrEmpty(conn))
                    {
                        Environment.SetEnvironmentVariable("SQL_CONN", conn, EnvironmentVariableTarget.Process);
                    }
                }
            }
            catch { /* best-effort, do not block startup */ }

            ApplicationConfiguration.Initialize();
            Application.Run(new MainForm());
        }
    }
}

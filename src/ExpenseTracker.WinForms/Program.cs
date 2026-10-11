using System;
using System.Windows.Forms;

namespace ExpenseTracker.WinForms
{
    // Application entry point for the Windows Forms desktop app.
    internal static class Program
    {
        [STAThread]
        // Initializes WinForms defaults and runs the main window on the UI thread.
        static void Main()
        {
            ApplicationConfiguration.Initialize();
            Application.Run(new MainForm());
        }
    }
}

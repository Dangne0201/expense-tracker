using System;
using System.IO;
using FlaUI.Core;
using FlaUI.UIA3;
using NUnit.Framework;

namespace ExpenseTracker.UiTests
{
    [TestFixture]
    public class MainWindowUiTests
    {
        [Test]
        public void App_launches_and_shows_a_main_window()
        {
            // Path to the WinForms exe built in the repo. Adjust if you publish elsewhere.
            var repoRoot = TestContext.CurrentContext.TestDirectory;
            // Move up from test bin to repo. This relative path works when running from repo build output.
            var exe = Path.Combine(repoRoot, "..", "..", "..", "..", "src", "ExpenseTracker.WinForms", "bin", "Debug", "net10.0-windows", "ExpenseTracker.WinForms.exe");
            exe = Path.GetFullPath(exe);

            Assert.That(File.Exists(exe), Is.True, $"Exe not found at {exe}. Build the WinForms project before running UI tests.");

            using (var app = Application.Launch(exe))
            using (var automation = new UIA3Automation())
            {
                var main = app.GetMainWindow(automation, TimeSpan.FromSeconds(10));
                Assert.IsNotNull(main, "Main window should appear after app launch");

                // basic smoke: window has a title or something visible
                Assert.IsFalse(string.IsNullOrWhiteSpace(main.Title), "Main window title should not be empty");

                // Close the app gracefully
                app.Close();
            }
        }
    }
}

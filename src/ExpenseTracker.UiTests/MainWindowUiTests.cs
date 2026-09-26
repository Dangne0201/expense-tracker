using System;
using System.IO;
using System.Drawing;
using System.Drawing.Imaging;
using FlaUI.Core;
using FlaUI.Core.AutomationElements;
using FlaUI.Core.Definitions;
using FlaUI.Core.Input;
using FlaUI.Core.WindowsAPI;
using FlaUI.UIA3;
using NUnit.Framework;
using FlaUIApplication = FlaUI.Core.Application;

namespace ExpenseTracker.UiTests
{
    [TestFixture]
    public class MainWindowUiTests
    {
        [Test]
        public void App_launches_and_shows_a_main_window()
        {
            var repoRoot = Path.GetFullPath(Path.Combine(TestContext.CurrentContext.TestDirectory, "..", "..", "..", "..", ".."));
            var configuration = Environment.GetEnvironmentVariable("EXPENSE_TRACKER_CONFIGURATION");
            if (string.IsNullOrWhiteSpace(configuration))
            {
                configuration = "Debug";
            }
            var exe = ResolveAppPath(repoRoot, configuration);

            Assert.That(File.Exists(exe), Is.True, $"Exe not found at {exe}. Build the WinForms project before running UI tests.");

            using (var app = FlaUIApplication.Launch(exe))
            using (var automation = new UIA3Automation())
            {
                try
                {
                    var main = app.GetMainWindow(automation, TimeSpan.FromSeconds(20));
                    Assert.IsNotNull(main, "Main window should appear after app launch");
                    Assert.That(main!.Title, Is.EqualTo("Expense Tracker (WinForms)"));
                    CaptureDemoScreenshotIfRequested(main);
                }
                finally
                {
                    app.Close(killIfCloseFails: true);
                }
            }
        }

        [Test]
        public void Expense_can_be_added_edited_cancelled_deleted_and_verified_after_restart()
            {
                var repoRoot = Path.GetFullPath(Path.Combine(TestContext.CurrentContext.TestDirectory, "..", "..", "..", "..", ".."));
                var configuration = Environment.GetEnvironmentVariable("EXPENSE_TRACKER_CONFIGURATION");
                if (string.IsNullOrWhiteSpace(configuration))
                {
                    configuration = "Debug";
                }

                var exe = ResolveAppPath(repoRoot, configuration);
                Assert.That(File.Exists(exe), Is.True, $"Exe not found at {exe}. Build the WinForms project before running UI tests.");

                var categoryName = "UiTest-" + Guid.NewGuid().ToString("N")[..8];
                var note = "UiExpense-" + Guid.NewGuid().ToString("N")[..8];
                var editedNote = note + "-edited";

                using (var app = FlaUIApplication.Launch(exe))
                using (var automation = new UIA3Automation())
                {
                    try
                    {
                        var main = app.GetMainWindow(automation, TimeSpan.FromSeconds(20));
                        Assert.That(main, Is.Not.Null, "Main window should appear before CRUD automation");

                        var categoryInput = Find(main!, "txtNewCategory").AsTextBox();
                        categoryInput.Text = categoryName;
                        Find(main!, "btnAddCategory").AsButton().Invoke();

                        var categories = Find(main!, "lstCategories").AsListBox();
                        categories.Select(categoryName);
                        Find(main!, "txtAmount").AsTextBox().Text = "12.50";
                        Find(main!, "txtNote").AsTextBox().Text = note;
                        Find(main!, "btnAddExpense").AsButton().Invoke();

                        var grid = Find(main!, "dgvExpenses").AsDataGridView();
                        var addedRow = WaitForRow(grid, note, shouldExist: true);
                        Assert.That(addedRow, Is.Not.Null, "The added expense should appear in the grid. " +
                            $"Amount input={Find(main!, "txtAmount").AsTextBox().Text}; note input={Find(main!, "txtNote").AsTextBox().Text}");
                        Assert.That(grid.Rows.Count, Is.EqualTo(2), "The grid should show one expense and one total row");

                        DoubleClickCell(FindNoteCell(addedRow!, note));
                        var amountInput = Find(main!, "txtAmount").AsTextBox();
                        var noteInput = Find(main!, "txtNote").AsTextBox();
                        amountInput.Text = "99.99";
                        noteInput.Text = note + "-cancelled";
                        main!.Focus();
                        Keyboard.Press(VirtualKeyShort.ESC);
                        Assert.That(Find(main!, "btnAddExpense").Name, Is.EqualTo("Add Expense"));
                        Assert.That(FindRow(grid, note), Is.Not.Null, "Escape should cancel edits without changing the saved row");
                        Assert.That(FindRow(grid, note + "-cancelled"), Is.Null);

                        DoubleClickCell(FindNoteCell(FindRow(grid, note)!, note));
                        amountInput.Text = "27.50";
                        noteInput.Text = editedNote;
                        Find(main!, "btnAddExpense").AsButton().Invoke();
                        var updatedRow = FindRow(grid, editedNote);
                        Assert.That(updatedRow, Is.Not.Null, "Saving edits should update the row shown in the grid");
                        Assert.That(FindRow(grid, note), Is.Null, "The old note should no longer be present after edit");
                        Assert.That(grid.Rows.Count, Is.EqualTo(2), "The displayed total should include the edited expense only once");

                    }
                    finally
                    {
                        app.Close(killIfCloseFails: true);
                    }
                }

                using (var restartedApp = FlaUIApplication.Launch(exe))
                using (var automation = new UIA3Automation())
                {
                    try
                    {
                        var main = restartedApp.GetMainWindow(automation, TimeSpan.FromSeconds(20));
                        Assert.That(main, Is.Not.Null, "The app should restart successfully");
                        Find(main!, "lstCategories").AsListBox().Select(categoryName);
                        var restartedGrid = Find(main!, "dgvExpenses").AsDataGridView();
                            var persistedRow = FindRow(restartedGrid, editedNote);
                            Assert.That(persistedRow, Is.Not.Null, "The saved edit and category should persist after restart");
                            Assert.That(restartedGrid.Rows.Count, Is.EqualTo(2), "The persisted expense should still contribute to the total row");
                            FindNoteCell(persistedRow!, editedNote).Click();
                            Find(main!, "btnDeleteExpense").AsButton().Invoke();
                            Assert.That(FindRow(restartedGrid, editedNote), Is.Null, "Deleting after restart should remove the expense");
                            Assert.That(restartedGrid.Rows.Count, Is.EqualTo(1), "Deleting the only expense should leave the zero-total row");
                            Assert.That(FindRow(restartedGrid, "TOTAL"), Is.Not.Null, "The total row should remain visible when there are no expenses");
                        }
                        finally
                        {
                            restartedApp.Close(killIfCloseFails: true);
                        }
                    }

                using (var finalApp = FlaUIApplication.Launch(exe))
                using (var automation = new UIA3Automation())
                {
                    try
                    {
                        var main = finalApp.GetMainWindow(automation, TimeSpan.FromSeconds(20));
                        Assert.That(main, Is.Not.Null, "The app should launch after the deletion");
                        Find(main!, "lstCategories").AsListBox().Select(categoryName);
                        var finalGrid = Find(main!, "dgvExpenses").AsDataGridView();
                        Assert.That(FindRow(finalGrid, editedNote), Is.Null, "The deletion should persist after restart");
                        Assert.That(FindRow(finalGrid, "TOTAL"), Is.Not.Null, "The zero-total row should remain visible after restart");
                    }
                    finally
                    {
                        finalApp.Close(killIfCloseFails: true);
                    }
                }
            }

        private static AutomationElement Find(AutomationElement root, string automationId)
            {
                var element = root.FindFirstDescendant(condition => condition.ByAutomationId(automationId));
                Assert.That(element, Is.Not.Null, $"Could not find UI element '{automationId}'.");
                return element!;
            }

            private static string ResolveAppPath(string repoRoot, string configuration)
            {
                var configuredPath = Environment.GetEnvironmentVariable("EXPENSE_TRACKER_APP_PATH");
                var path = string.IsNullOrWhiteSpace(configuredPath)
                    ? Path.Combine(repoRoot, "src", "ExpenseTracker.WinForms", "bin", configuration, "net10.0-windows", "ExpenseTracker.WinForms.exe")
                    : configuredPath;
                return Path.GetFullPath(path);
            }

        private static FlaUI.Core.AutomationElements.DataGridViewRow? FindRow(
            FlaUI.Core.AutomationElements.DataGridView grid,
            string text)
            {
                foreach (var row in grid.Rows)
                {
                    foreach (var cell in row.Cells)
                    {
                        if (string.Equals(cell.Value, text, StringComparison.Ordinal))
                        {
                            return row;
                        }
                    }
                }

                return null;
            }

            private static FlaUI.Core.AutomationElements.DataGridViewRow? WaitForRow(
                FlaUI.Core.AutomationElements.DataGridView grid,
                string text,
                bool shouldExist)
            {
                var timeout = System.Diagnostics.Stopwatch.StartNew();
                FlaUI.Core.AutomationElements.DataGridViewRow? row;
                do
                {
                    row = FindRow(grid, text);
                    if ((row != null) == shouldExist)
                    {
                        return row;
                    }

                    System.Threading.Thread.Sleep(100);
                }
                while (timeout.Elapsed < TimeSpan.FromSeconds(5));

                return row;
            }

            private static void CaptureDemoScreenshotIfRequested(AutomationElement main)
            {
                var screenshotPath = Environment.GetEnvironmentVariable("EXPENSE_TRACKER_SCREENSHOT_PATH");
                if (string.IsNullOrWhiteSpace(screenshotPath))
                {
                    return;
                }

                var demoExpenses = new[]
                {
                    (Category: "Food", Amount: "42.75", Note: "Weekly groceries"),
                    (Category: "Transport", Amount: "18.50", Note: "Monthly bus pass"),
                    (Category: "Bills", Amount: "64.20", Note: "Electricity bill")
                };

                var grid = Find(main, "dgvExpenses").AsDataGridView();
                foreach (var expense in demoExpenses)
                {
                    Find(main, "lstCategories").AsListBox().Select(expense.Category);
                    Find(main, "txtAmount").AsTextBox().Text = expense.Amount;
                    Find(main, "txtNote").AsTextBox().Text = expense.Note;
                    Find(main, "btnAddExpense").AsButton().Invoke();
                    Assert.That(WaitForRow(grid, expense.Note, shouldExist: true), Is.Not.Null,
                        $"The demo expense '{expense.Note}' should appear before screenshot capture");
                }

                Assert.That(grid.Rows.Count, Is.EqualTo(4), "The demo screenshot should show three expenses and the total row");
                var fullScreenshotPath = Path.GetFullPath(screenshotPath);
                Directory.CreateDirectory(Path.GetDirectoryName(fullScreenshotPath)!);
                main.Patterns.Window.Pattern.SetWindowVisualState(WindowVisualState.Maximized);
                main.Focus();
                Mouse.MoveTo(new System.Drawing.Point(5, 5));
                System.Threading.Thread.Sleep(500);
                var windowBounds = main.BoundingRectangle;
                var gridBounds = Find(main, "dgvExpenses").BoundingRectangle;
                var expenseButtonBounds = Find(main, "btnAddExpense").BoundingRectangle;
                Assert.That(expenseButtonBounds.Top, Is.GreaterThan(gridBounds.Bottom),
                    $"The expense actions must be below the grid. Window={windowBounds}; grid={gridBounds}; button={expenseButtonBounds}");
                foreach (var automationId in new[] { "btnAddCategory", "btnAddExpense", "txtNote" })
                {
                    var controlBounds = Find(main, automationId).BoundingRectangle;
                    Assert.That(controlBounds.Width, Is.GreaterThan(0),
                        $"Control '{automationId}' must have a visible layout rectangle. Window={windowBounds}; control={controlBounds}");
                    Assert.That(controlBounds.Top, Is.GreaterThan(windowBounds.Top + windowBounds.Height * 0.75),
                        $"Control '{automationId}' must be in the visible footer area. Window={windowBounds}; control={controlBounds}");
                    Assert.That(controlBounds.Bottom, Is.LessThanOrEqualTo(windowBounds.Bottom),
                        $"Control '{automationId}' must remain visible within the window. Window={windowBounds}; control={controlBounds}");
                }

                var screenBounds = System.Windows.Forms.Screen.PrimaryScreen!.Bounds;
                using (var bitmap = new Bitmap(screenBounds.Width, screenBounds.Height))
                using (var graphics = Graphics.FromImage(bitmap))
                {
                    graphics.CopyFromScreen(screenBounds.Location, Point.Empty, screenBounds.Size);
                    bitmap.Save(fullScreenshotPath, ImageFormat.Png);
                }
            }

            private static FlaUI.Core.AutomationElements.DataGridViewCell FindNoteCell(
            FlaUI.Core.AutomationElements.DataGridViewRow row,
            string note)
            {
                foreach (var cell in row.Cells)
                {
                    if (string.Equals(cell.Value, note, StringComparison.Ordinal))
                    {
                        return cell;
                    }
                }

                Assert.Fail($"Could not find the grid cell for note '{note}'.");
                throw new InvalidOperationException("Unreachable after assertion failure.");
            }

        private static void DoubleClickCell(FlaUI.Core.AutomationElements.DataGridViewCell cell)
        {
            var bounds = cell.BoundingRectangle;
            Mouse.DoubleClick(new System.Drawing.Point(
                bounds.Left + bounds.Width / 2,
                bounds.Top + bounds.Height / 2));
        }
    }
}

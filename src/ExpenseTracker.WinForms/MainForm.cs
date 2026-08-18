using System;
using System.Data;
using System.Drawing;
using System.Diagnostics;
using System.IO;
using System.Windows.Forms;
using Microsoft.Data.SqlClient;

namespace ExpenseTracker.WinForms
{
    /// <summary>
    /// Main screen for the expense tracker.
    /// It loads categories and expenses, allows inserts/deletes, and resolves the database connection
    /// using SQL_CONN first, then falling back to LocalDB + repository MDF if needed.
    /// </summary>
    public class MainForm : Form
    {
        // Active connection string. This is chosen once during startup and reused for all CRUD actions.
        private string _conn;

        // Legacy LocalDB fallbacks kept for compatibility when the DB is not running in Docker.
        private readonly string _connPrimary = @"Server=(localdb)\MSSQLLocalDB;Database=ExpenseDb;Trusted_Connection=True;";
        private readonly string _connAttachTemplate = @"Data Source=(LocalDB)\MSSQLLocalDB;AttachDbFilename={0};Integrated Security=True;Connect Timeout=30;";

        // UI controls.
        private ListBox lstCategories;
        private Button btnLoadCategories;
        private TextBox txtNewCategory;
        private Button btnAddCategory;

        private DataGridView dgvExpenses;
        private TextBox txtAmount;
        private DateTimePicker dtpDate;
        private TextBox txtNote;
        private Button btnAddExpense;
        private Button btnDeleteExpense;

        public MainForm()
        {
            // Resolve DB connectivity before building the form; the rest of the UI depends on it.
            EnsureDatabaseAvailable();
            InitializeComponents();
        }

        private void InitializeComponents()
        {
            Text = "Expense Tracker (WinForms)";
            AutoScaleMode = AutoScaleMode.Dpi;
            Font = SystemFonts.MessageBoxFont;
            Width = 1300;
            Height = 640;
            MinimumSize = new Size(1300, 640);
            MaximumSize = MinimumSize;
            FormBorderStyle = FormBorderStyle.FixedSingle;
            MaximizeBox = false;

            // Root layout: left panel for categories, right panel for expenses.
            var root = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 1 };
            root.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 560));
            root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            Controls.Add(root);

            // Left panel: categories list + buttons.
            var pnlLeft = new Panel { Dock = DockStyle.Fill };
            var lblCat = new Label { Text = "Categories", Dock = DockStyle.Top, Height = 22 };
            lstCategories = new ListBox { Dock = DockStyle.Fill };

            // Right panel: expense grid + input area.
            var pnlRight = new Panel { Dock = DockStyle.Fill };
            var lblExp = new Label { Text = "Expenses", Dock = DockStyle.Top, Height = 22 };
            dgvExpenses = new DataGridView
            {
                Dock = DockStyle.Fill,
                ReadOnly = true,
                AllowUserToAddRows = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.None,
                ScrollBars = ScrollBars.Both,
                RowHeadersVisible = false,
                ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing,
                AllowUserToResizeColumns = true,
                AllowUserToResizeRows = false
            };

            // Shared footer row keeps category controls aligned with the expense input area.
            var footer = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 1 };
            footer.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 300));
            footer.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

            // Category controls on the left footer.
            var footerLeft = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.LeftToRight, Padding = new Padding(6), WrapContents = false };
            btnLoadCategories = new Button { Text = "Load", AutoSize = false, Width = 140, Height = 44, Padding = new Padding(6), Margin = new Padding(12), TextAlign = ContentAlignment.MiddleCenter };
            txtNewCategory = new TextBox { Width = 200, Anchor = AnchorStyles.Left | AnchorStyles.Right, Margin = new Padding(3, 6, 6, 6) };
            btnAddCategory = new Button { Text = "Add", AutoSize = false, Width = 140, Height = 44, Padding = new Padding(6), Margin = new Padding(12), TextAlign = ContentAlignment.MiddleCenter };

            footerLeft.Controls.Add(btnLoadCategories);
            footerLeft.Controls.Add(txtNewCategory);
            footerLeft.Controls.Add(btnAddCategory);
            btnLoadCategories.Click += (s, e) => LoadCategories();
            btnAddCategory.Click += (s, e) => AddCategory();

            // Expense controls on the right footer.
            var footerRight = new Panel { Dock = DockStyle.Fill };

            var inputTable = new TableLayoutPanel
            {
                Dock = DockStyle.Top,
                Height = 40,
                AutoSize = false,
                ColumnCount = 6,
                RowCount = 1,
                Padding = new Padding(6)
            };

            inputTable.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            inputTable.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 160));
            inputTable.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            inputTable.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 160));
            inputTable.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            inputTable.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

            var lblAmount = new Label { Text = "Amount", AutoSize = true, TextAlign = ContentAlignment.MiddleRight, Anchor = AnchorStyles.Right, Margin = new Padding(3, 8, 6, 3) };
            txtAmount = new TextBox { Width = 120, Anchor = AnchorStyles.Left | AnchorStyles.Right, Margin = new Padding(3, 6, 6, 6) };

            var lblDate = new Label { Text = "Date", AutoSize = true, TextAlign = ContentAlignment.MiddleRight, Anchor = AnchorStyles.Right, Margin = new Padding(12, 8, 6, 3) };
            dtpDate = new DateTimePicker { Width = 160, Format = DateTimePickerFormat.Custom, CustomFormat = "yyyy-MM-dd HH:mm", Margin = new Padding(3, 6, 6, 6) };

            var lblNote = new Label { Text = "Note", AutoSize = true, TextAlign = ContentAlignment.MiddleRight, Anchor = AnchorStyles.Right, Margin = new Padding(12, 8, 6, 3) };
            txtNote = new TextBox { Anchor = AnchorStyles.Left | AnchorStyles.Right, Margin = new Padding(3, 6, 6, 6), Width = 200 };

            inputTable.Controls.Add(lblAmount, 0, 0);
            inputTable.Controls.Add(txtAmount, 1, 0);
            inputTable.Controls.Add(lblDate, 2, 0);
            inputTable.Controls.Add(dtpDate, 3, 0);
            inputTable.Controls.Add(lblNote, 4, 0);
            inputTable.Controls.Add(txtNote, 5, 0);

            var footerRightTable = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 2, ColumnCount = 6 };
            footerRightTable.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            footerRightTable.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            footerRightTable.ColumnStyles.Clear();
            footerRightTable.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            footerRightTable.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 220));
            footerRightTable.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 220));
            footerRightTable.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 220));
            footerRightTable.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            footerRightTable.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

            footerRightTable.Controls.Add(inputTable, 0, 0);
            footerRightTable.SetColumnSpan(inputTable, 6);

            btnDeleteExpense = new Button { Text = "Delete Expense", AutoSize = false, Width = 200, Height = 44, Padding = new Padding(4), Margin = new Padding(12), Anchor = AnchorStyles.Left, TextAlign = ContentAlignment.MiddleCenter, UseCompatibleTextRendering = true };
            var btnLoadExpenses = new Button { Text = "Load Expenses", AutoSize = false, Width = 200, Height = 44, Padding = new Padding(4), Margin = new Padding(12), Anchor = AnchorStyles.Left, TextAlign = ContentAlignment.MiddleCenter, UseCompatibleTextRendering = true };
            btnAddExpense = new Button { Text = "Add Expenses", AutoSize = false, Width = 200, Height = 44, Padding = new Padding(4), Margin = new Padding(12), Anchor = AnchorStyles.Left, TextAlign = ContentAlignment.MiddleCenter, UseCompatibleTextRendering = true };

            footerRightTable.Controls.Add(btnDeleteExpense, 1, 1);
            footerRightTable.Controls.Add(btnLoadExpenses, 2, 1);
            footerRightTable.Controls.Add(btnAddExpense, 3, 1);

            btnAddExpense.Click += (s, e) => AddExpense();
            btnLoadExpenses.Click += (s, e) => LoadExpenses();
            btnDeleteExpense.Click += (s, e) => DeleteSelectedExpense();

            footerRight.Controls.Add(footerRightTable);

            // Final form layout: main working area + footer area.
            root.RowCount = 2;
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 120));

            pnlLeft.Controls.Add(lstCategories);
            pnlLeft.Controls.Add(lblCat);

            pnlRight.Controls.Add(dgvExpenses);
            pnlRight.Controls.Add(lblExp);

            root.Controls.Add(pnlLeft, 0, 0);
            root.Controls.Add(pnlRight, 1, 0);
            root.Controls.Add(footerLeft, 0, 1);
            root.Controls.Add(footerRight, 1, 1);
        }

        /// <summary>
        /// Resolve the most appropriate connection string before the UI is used.
        /// Priority:
        /// 1) SQL_CONN environment variable (Docker or custom deployment)
        /// 2) repository MDF file attach
        /// 3) LocalDB instance fallback
        /// </summary>
        private void EnsureDatabaseAvailable()
        {
            var envConn = GetConnectionStringFromEnvironment();
            if (!string.IsNullOrWhiteSpace(envConn))
            {
                const int maxRetries = 6;
                for (int attempt = 0; attempt < maxRetries; attempt++)
                {
                    if (TryOpenConnection(envConn))
                    {
                        _conn = envConn;
                        return;
                    }

                    System.Threading.Thread.Sleep(2000);
                }

                MessageBox.Show("SQL_CONN is set but the app failed to connect using it. Please check the connection string and ensure the DB is reachable.");
            }

            var mdfPath = FindDataMdf();
            if (!string.IsNullOrEmpty(mdfPath))
            {
                var dataFolder = Path.GetDirectoryName(mdfPath);
                var repoRoot = Directory.GetParent(dataFolder)?.FullName ?? dataFolder;
                AppDomain.CurrentDomain.SetData("DataDirectory", repoRoot);

                var attachConn = $"Data Source=(LocalDB)\\MSSQLLocalDB;AttachDbFilename=|DataDirectory|\\data\\ExpenseDb.mdf;Integrated Security=True;Connect Timeout=30;";

                if (TryOpenConnection(attachConn))
                {
                    _conn = attachConn;
                    return;
                }

                try { StartLocalDbInstance(); } catch { }

                if (TryOpenConnection(attachConn))
                {
                    _conn = attachConn;
                    return;
                }
            }

            try { StartLocalDbInstance(); } catch { }
            if (TryOpenConnection(_connPrimary))
            {
                _conn = _connPrimary;
                return;
            }

            var fullMdf = FindDataMdf();
            if (!string.IsNullOrEmpty(fullMdf))
            {
                var attachFull = string.Format(_connAttachTemplate, fullMdf);
                if (TryOpenConnection(attachFull))
                {
                    _conn = attachFull;
                    return;
                }
            }

            _conn = _connPrimary;
            MessageBox.Show("Could not automatically connect to LocalDB. Please ensure MSSQLLocalDB is installed and running (run 'sqllocaldb start MSSQLLocalDB'), or place data\\ExpenseDb.mdf next to the app and try again.");
        }

        private string GetConnectionStringFromEnvironment()
        {
            // Prefer the process-level environment variable, because this is what setup scripts set.
            var envConn = Environment.GetEnvironmentVariable("SQL_CONN");
            if (!string.IsNullOrWhiteSpace(envConn))
            {
                return envConn;
            }

            // Fallback to the user-level variable if the app was started from a shell that persisted it.
            try
            {
                return Environment.GetEnvironmentVariable("SQL_CONN", EnvironmentVariableTarget.User);
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Opens the connection and logs failure details to a startup.log file for troubleshooting.
        /// </summary>
        private bool TryOpenConnection(string connStr)
        {
            try
            {
                using var c = new SqlConnection(connStr);
                c.Open();
                c.Close();
                return true;
            }
            catch (Exception ex)
            {
                try
                {
                    var logDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "logs");
                    if (!Directory.Exists(logDir)) Directory.CreateDirectory(logDir);

                    var logFile = Path.Combine(logDir, "startup.log");
                    var sanitized = SanitizeConnectionString(connStr);
                    var line = $"[{DateTime.UtcNow:O}] TryOpenConnection failed. Conn={sanitized}; Error={ex.Message}{Environment.NewLine}";
                    File.AppendAllText(logFile, line);
                }
                catch
                {
                    // Best-effort logging only; do not let diagnostics break startup.
                }

                return false;
            }
        }

        private static string SanitizeConnectionString(string conn)
        {
            if (string.IsNullOrEmpty(conn)) return conn;
            try
            {
                // Strip passwords from logs to avoid leaking sensitive information.
                var regex = new System.Text.RegularExpressions.Regex("(?i)(Password=)[^;]+;?");
                return regex.Replace(conn, "Password=******;");
            }
            catch
            {
                return "<could-not-sanitize>";
            }
        }

        private void StartLocalDbInstance()
        {
            // LocalDB is a legacy fallback for developer machines that do not use Docker.
            try
            {
                var psi = new ProcessStartInfo("sqllocaldb", "start MSSQLLocalDB")
                {
                    CreateNoWindow = true,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true
                };

                using var p = Process.Start(psi);
                if (p != null)
                {
                    p.WaitForExit(5000);
                }
            }
            catch
            {
                // Ignore startup failures here; the connection retry loop will surface the real issue.
            }
        }

        private string FindDataMdf()
        {
            // Search upward from the app directory for repo/data/ExpenseDb.mdf.
            var dir = AppDomain.CurrentDomain.BaseDirectory;
            for (int i = 0; i < 8; i++)
            {
                var candidate = Path.GetFullPath(Path.Combine(dir, "data", "ExpenseDb.mdf"));
                if (File.Exists(candidate)) return candidate;

                var parent = Directory.GetParent(dir);
                if (parent == null) break;
                dir = parent.FullName;
            }

            return null;
        }

        /// <summary>
        /// Loads categories from the database into the left-side list box.
        /// </summary>
        private void LoadCategories()
        {
            try
            {
                using var conn = new SqlConnection(_conn);
                using var cmd = new SqlCommand("SELECT Id, Name FROM Categories ORDER BY Name", conn);
                var dt = new DataTable();
                using var da = new SqlDataAdapter(cmd);
                da.Fill(dt);

                lstCategories.DisplayMember = "Name";
                lstCategories.ValueMember = "Id";
                lstCategories.DataSource = dt;
            }
            catch (Exception ex)
            {
                MessageBox.Show("LoadCategories error: " + ex.Message);
            }
        }

        private void AddCategory()
        {
            var name = txtNewCategory.Text.Trim();
            if (string.IsNullOrEmpty(name))
            {
                MessageBox.Show("Enter category name");
                return;
            }

            try
            {
                using var conn = new SqlConnection(_conn);
                using var cmd = new SqlCommand("INSERT INTO Categories (Name) VALUES (@name)", conn);
                cmd.Parameters.AddWithValue("@name", name);
                conn.Open();
                cmd.ExecuteNonQuery();
                txtNewCategory.Text = "";
                LoadCategories();
            }
            catch (Exception ex)
            {
                MessageBox.Show("AddCategory error: " + ex.Message);
            }
        }

        /// <summary>
        /// Loads the expense grid with amount, date, note, and category name.
        /// </summary>
        private void LoadExpenses()
        {
            try
            {
                using var conn = new SqlConnection(_conn);
                using var cmd = new SqlCommand(@"SELECT e.Id, e.Amount, e.Date, e.Note, e.CategoryId, c.Name AS CategoryName
FROM Expenses e
JOIN Categories c ON e.CategoryId = c.Id
ORDER BY e.Id ASC", conn);

                var dt = new DataTable();
                using var da = new SqlDataAdapter(cmd);
                da.Fill(dt);

                if (dt.Rows.Count > 0)
                {
                    var totalAmount = dt.AsEnumerable().Sum(r => r["Amount"] == DBNull.Value ? 0m : Convert.ToDecimal(r["Amount"]));
                    var totalRow = dt.NewRow();
                    totalRow["Id"] = DBNull.Value;
                    totalRow["Amount"] = totalAmount;
                    totalRow["Date"] = DBNull.Value;
                    totalRow["Note"] = DBNull.Value;
                    totalRow["CategoryId"] = DBNull.Value;
                    totalRow["CategoryName"] = DBNull.Value;
                    dt.Rows.Add(totalRow);
                }

                dgvExpenses.DataSource = dt;

                if (dgvExpenses.Columns.Contains("Id")) dgvExpenses.Columns["Id"].Width = 80;
                if (dgvExpenses.Columns.Contains("Amount")) dgvExpenses.Columns["Amount"].Width = 110;
                if (dgvExpenses.Columns.Contains("Date")) dgvExpenses.Columns["Date"].Width = 150;
                if (dgvExpenses.Columns.Contains("Note")) dgvExpenses.Columns["Note"].Width = 150;
                if (dgvExpenses.Columns.Contains("CategoryName")) dgvExpenses.Columns["CategoryName"].Width = 150;
                if (dgvExpenses.Columns.Contains("CategoryId")) dgvExpenses.Columns["CategoryId"].Visible = true;
            }
            catch (Exception ex)
            {
                MessageBox.Show("LoadExpenses error: " + ex.Message);
            }
        }

        private void AddExpense()
        {
            if (lstCategories.Items.Count == 0)
            {
                MessageBox.Show("No category selected. Load categories and select one.");
                return;
            }

            if (lstCategories.SelectedItem == null)
            {
                MessageBox.Show("Select a category from the list.");
                return;
            }

            if (!decimal.TryParse(txtAmount.Text.Trim(), out var amount))
            {
                MessageBox.Show("Invalid amount");
                return;
            }

            var date = dtpDate.Value;
            var note = txtNote.Text.Trim();
            var row = (DataRowView)lstCategories.SelectedItem;
            var categoryId = Convert.ToInt32(row["Id"]);

            try
            {
                using var conn = new SqlConnection(_conn);
                using var cmd = new SqlCommand("INSERT INTO Expenses (Amount, Date, Note, CategoryId) VALUES (@amt, @dt, @note, @cat)", conn);
                cmd.Parameters.AddWithValue("@amt", amount);
                cmd.Parameters.AddWithValue("@dt", date);
                cmd.Parameters.AddWithValue("@note", string.IsNullOrEmpty(note) ? (object)DBNull.Value : note);
                cmd.Parameters.AddWithValue("@cat", categoryId);
                conn.Open();
                cmd.ExecuteNonQuery();
                txtAmount.Text = "";
                txtNote.Text = "";
                LoadExpenses();
            }
            catch (Exception ex)
            {
                MessageBox.Show("AddExpense error: " + ex.Message);
            }
        }

        private void DeleteSelectedExpense()
        {
            if (dgvExpenses.CurrentRow == null)
            {
                MessageBox.Show("Select an expense to delete.");
                return;
            }

            try
            {
                var idObj = dgvExpenses.CurrentRow.Cells["Id"].Value;
                if (idObj == null)
                {
                    MessageBox.Show("Selected row has no Id.");
                    return;
                }

                var id = Convert.ToInt32(idObj);
                using var conn = new SqlConnection(_conn);
                using var cmd = new SqlCommand("DELETE FROM Expenses WHERE Id = @id", conn);
                cmd.Parameters.AddWithValue("@id", id);
                conn.Open();
                cmd.ExecuteNonQuery();
                LoadExpenses();
            }
            catch (Exception ex)
            {
                MessageBox.Show("DeleteExpense error: " + ex.Message);
            }
        }
    }
}

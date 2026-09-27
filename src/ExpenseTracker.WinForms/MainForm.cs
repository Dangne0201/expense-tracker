using System;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Windows.Forms;
using Microsoft.Data.SqlClient;

namespace ExpenseTracker.WinForms
{
    /// <summary>
    /// Main screen for the expense tracker.
    /// It loads categories and expenses, supports expense CRUD, and resolves the database connection
    /// using SQL_CONN first, then falling back to LocalDB + repository MDF for legacy use.
    /// </summary>
    public class MainForm : Form
    {
        // Active connection string. This is chosen once during startup and reused for all CRUD actions.
        private string _conn;
        private ExpenseRepository _repository;

        private const string LocalDbConnectionString =
            @"Server=(localdb)\MSSQLLocalDB;Database=ExpenseDb;Trusted_Connection=True;Connect Timeout=3;";
        private const string LocalDbAttachConnectionTemplate =
            @"Data Source=(LocalDB)\MSSQLLocalDB;AttachDbFilename={0};Integrated Security=True;Connect Timeout=3;";

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
        private bool _isEditingExpense;
        private int _editingExpenseId;

        public MainForm()
        {
            // Resolve DB connectivity before building the form; the rest of the UI depends on it.
            EnsureDatabaseAvailable();
            _repository = new ExpenseRepository(_conn);
            InitializeComponents();
            Shown += (s, e) =>
            {
                LoadCategories();
                LoadExpenses();
            };
        }

        private void InitializeComponents()
        {
            Text = "Expense Tracker (WinForms)";
            AutoScaleMode = AutoScaleMode.Dpi;
            Font = SystemFonts.MessageBoxFont;
            var workingArea = Screen.PrimaryScreen?.WorkingArea ?? new Rectangle(0, 0, 1300, 640);
            Width = Math.Max(640, Math.Min(980, workingArea.Width - 20));
            Height = Math.Max(460, Math.Min(560, workingArea.Height - 20));
            MinimumSize = new Size(Math.Min(640, Width), Math.Min(460, Height));
            FormBorderStyle = FormBorderStyle.Sizable;
            MaximizeBox = true;
            KeyPreview = true;
            KeyDown += (s, e) =>
            {
                if (e.KeyCode == Keys.Escape && _isEditingExpense)
                {
                    ResetExpenseEditor();
                    e.Handled = true;
                }
            };

            // Root layout: left panel for categories, right panel for expenses.
            var root = new TableLayoutPanel { ColumnCount = 2, RowCount = 1 };
            var categoryPanelWidth = Math.Min(380, Math.Max(240, (int)(ClientSize.Width * 0.34)));
            root.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, categoryPanelWidth));
            root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            Controls.Add(root);

            // Left panel: categories list + buttons.
            var pnlLeft = new Panel { Dock = DockStyle.Fill };
            var lblCat = new Label { Text = "Categories", Dock = DockStyle.Top, Height = 22 };
            lstCategories = new ListBox { Name = "lstCategories", Dock = DockStyle.Fill };

            // Right panel: expense grid + input area.
            var pnlRight = new Panel { Dock = DockStyle.Fill };
            var lblExp = new Label { Text = "Expenses", Dock = DockStyle.Top, Height = 22 };
            dgvExpenses = new DataGridView
            {
                Dock = DockStyle.Fill,
                Name = "dgvExpenses",
                ReadOnly = true,
                AllowUserToAddRows = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                MultiSelect = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.None,
                ScrollBars = ScrollBars.Both,
                RowHeadersVisible = false,
                ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing,
                AllowUserToResizeColumns = true,
                AllowUserToResizeRows = false
            };
            dgvExpenses.SizeChanged += (s, e) => SetExpenseColumnLayout();
            dgvExpenses.CellFormatting += (s, e) =>
            {
                if (e.RowIndex >= 0 && e.ColumnIndex >= 0 &&
                    dgvExpenses.Columns[e.ColumnIndex].Name == "Amount")
                {
                    if (e.Value != null && e.Value != DBNull.Value)
                    {
                        e.Value = ExpenseSummary.FormatAmount(Convert.ToDecimal(e.Value));
                        e.FormattingApplied = true;
                    }
                }
            };

            // Shared footer row keeps category controls aligned with the expense input area.
            const int footerHeight = 120;
            var footer = new TableLayoutPanel { ColumnCount = 2, RowCount = 1 };
            footer.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, categoryPanelWidth));
            footer.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            footer.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

            // Category controls on the left footer.
            var footerLeft = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.LeftToRight, Padding = new Padding(6), WrapContents = false };
            var categoryButtonWidth = categoryPanelWidth < 280 ? 60 : 75;
            btnLoadCategories = new Button { Name = "btnLoadCategories", Text = "Load", AutoSize = false, Width = categoryButtonWidth, Height = 44, Padding = new Padding(6), Margin = new Padding(2), TextAlign = ContentAlignment.MiddleCenter };
            txtNewCategory = new TextBox { Name = "txtNewCategory", Width = Math.Max(80, categoryPanelWidth - (categoryButtonWidth * 2) - 40), Anchor = AnchorStyles.Left | AnchorStyles.Right, Margin = new Padding(2, 6, 2, 6) };
            btnAddCategory = new Button { Name = "btnAddCategory", Text = "Add", AutoSize = false, Width = categoryButtonWidth, Height = 44, Padding = new Padding(6), Margin = new Padding(2), TextAlign = ContentAlignment.MiddleCenter };

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
            inputTable.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 130));
            inputTable.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            inputTable.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 140));
            inputTable.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            inputTable.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

            var lblAmount = new Label { Text = "Amount", AutoSize = true, TextAlign = ContentAlignment.MiddleRight, Anchor = AnchorStyles.Right, Margin = new Padding(3, 8, 6, 3) };
            txtAmount = new TextBox { Name = "txtAmount", Width = 120, Anchor = AnchorStyles.Left | AnchorStyles.Right, Margin = new Padding(3, 6, 6, 6) };

            var lblDate = new Label { Text = "Date", AutoSize = true, TextAlign = ContentAlignment.MiddleRight, Anchor = AnchorStyles.Right, Margin = new Padding(12, 8, 6, 3) };
            dtpDate = new DateTimePicker { Name = "dtpDate", Width = 130, Format = DateTimePickerFormat.Custom, CustomFormat = "yyyy-MM-dd HH:mm", Margin = new Padding(3, 6, 6, 6) };

            var lblNote = new Label { Text = "Note", AutoSize = true, TextAlign = ContentAlignment.MiddleRight, Anchor = AnchorStyles.Right, Margin = new Padding(12, 8, 6, 3) };
            txtNote = new TextBox { Name = "txtNote", Anchor = AnchorStyles.Left | AnchorStyles.Right, Margin = new Padding(3, 6, 6, 6), Width = 200 };

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
            var actionColumnWidth = Math.Min(180, Math.Max(120, (ClientSize.Width - categoryPanelWidth - 32) / 3));
            footerRightTable.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, actionColumnWidth));
            footerRightTable.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, actionColumnWidth));
            footerRightTable.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, actionColumnWidth));
            footerRightTable.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            footerRightTable.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

            footerRightTable.Controls.Add(inputTable, 0, 0);
            footerRightTable.SetColumnSpan(inputTable, 6);

            var actionButtonWidth = actionColumnWidth - 20;
            btnDeleteExpense = new Button { Name = "btnDeleteExpense", Text = "Delete Expense", AutoSize = false, Width = actionButtonWidth, Height = 44, Padding = new Padding(4), Margin = new Padding(8), Anchor = AnchorStyles.Left, TextAlign = ContentAlignment.MiddleCenter, UseCompatibleTextRendering = true };
            var btnLoadExpenses = new Button { Name = "btnLoadExpenses", Text = "Load Expenses", AutoSize = false, Width = actionButtonWidth, Height = 44, Padding = new Padding(4), Margin = new Padding(8), Anchor = AnchorStyles.Left, TextAlign = ContentAlignment.MiddleCenter, UseCompatibleTextRendering = true };
            btnAddExpense = new Button { Name = "btnAddExpense", Text = "Add Expense", AutoSize = false, Width = actionButtonWidth, Height = 44, Padding = new Padding(4), Margin = new Padding(8), Anchor = AnchorStyles.Left, TextAlign = ContentAlignment.MiddleCenter, UseCompatibleTextRendering = true };

            footerRightTable.Controls.Add(btnDeleteExpense, 1, 1);
            footerRightTable.Controls.Add(btnLoadExpenses, 2, 1);
            footerRightTable.Controls.Add(btnAddExpense, 3, 1);

            btnAddExpense.Click += (s, e) => AddExpense();
            btnLoadExpenses.Click += (s, e) => LoadExpenses();
            btnDeleteExpense.Click += (s, e) => DeleteSelectedExpense();
            dgvExpenses.CellDoubleClick += (s, e) =>
            {
                if (e.RowIndex >= 0)
                {
                    BeginEditSelectedExpense();
                }
            };

            footerRight.Controls.Add(footerRightTable);

            pnlLeft.Controls.Add(lstCategories);
            pnlLeft.Controls.Add(lblCat);

            pnlRight.Controls.Add(dgvExpenses);
            pnlRight.Controls.Add(lblExp);

            root.Controls.Add(pnlLeft, 0, 0);
            root.Controls.Add(pnlRight, 1, 0);
            footer.Controls.Add(footerLeft, 0, 0);
            footer.Controls.Add(footerRight, 1, 0);
            Controls.Add(footer);

            void LayoutMainContent()
            {
                var contentHeight = Math.Max(0, ClientSize.Height - footerHeight);
                root.Bounds = new Rectangle(0, 0, ClientSize.Width, contentHeight);
                footer.Bounds = new Rectangle(0, contentHeight, ClientSize.Width, ClientSize.Height - contentHeight);
            }

            Resize += (s, e) => LayoutMainContent();
            LayoutMainContent();
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
                const int maxRetries = 2;
                for (int attempt = 0; attempt < maxRetries; attempt++)
                {
                    if (TryOpenConnection(envConn))
                    {
                        _conn = envConn;
                        return;
                    }

                    if (attempt < maxRetries - 1)
                    {
                        System.Threading.Thread.Sleep(500);
                    }
                }

                _conn = envConn;
                MessageBox.Show(
                    "The configured database is unavailable. Check the connection and try again.",
                    "Database unavailable",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
                return;
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
            if (TryOpenConnection(LocalDbConnectionString))
            {
                _conn = LocalDbConnectionString;
                return;
            }

            var fullMdf = FindDataMdf();
            if (!string.IsNullOrEmpty(fullMdf))
            {
                var attachFull = string.Format(LocalDbAttachConnectionTemplate, fullMdf);
                if (TryOpenConnection(attachFull))
                {
                    _conn = attachFull;
                    return;
                }
            }

            _conn = LocalDbConnectionString;
            MessageBox.Show(
                "No database connection is available. Start Docker SQL Server or configure LocalDB, then restart the app.",
                "Database unavailable",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        }

        private string GetConnectionStringFromEnvironment()
        {
            // Prefer the process-level environment variable, because this is what setup scripts set.
            var envConn = Environment.GetEnvironmentVariable("SQL_CONN");
            if (string.IsNullOrWhiteSpace(envConn))
            {
                return null;
            }

            return NormalizeConnectionStringForLocalSql(envConn);
        }

        private static string NormalizeConnectionStringForLocalSql(string connectionString)
        {
            if (string.IsNullOrWhiteSpace(connectionString))
            {
                return connectionString;
            }

            var builder = new SqlConnectionStringBuilder(connectionString)
            {
                Encrypt = false,
                TrustServerCertificate = true,
                ConnectTimeout = 3
            };
            return builder.ConnectionString;
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
                    var errorType = ex.GetType().Name;
                    var sqlError = ex is SqlException sqlException
                        ? $"; SqlError={sqlException.Number}"
                        : string.Empty;
                    var line = $"[{DateTime.UtcNow:O}] TryOpenConnection failed. ErrorType={errorType}{sqlError}{Environment.NewLine}";
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
                lstCategories.DisplayMember = "Name";
                lstCategories.ValueMember = "Id";
                lstCategories.DataSource = _repository.GetCategories();
            }
            catch (Exception ex)
            {
                ShowDatabaseError("Loading categories", ex);
            }
        }

        private void AddCategory()
        {
            var name = txtNewCategory.Text.Trim();
            if (!ExpenseValidation.IsValidCategoryName(name))
            {
                MessageBox.Show("Enter a category name with at most 200 characters.");
                return;
            }

            try
            {
                _repository.AddCategory(name);
                txtNewCategory.Text = "";
                LoadCategories();
            }
            catch (Exception ex)
            {
                ShowDatabaseError("Adding category", ex);
            }
        }

        /// <summary>
        /// Loads the expense grid with amount, date, note, and category name.
        /// </summary>
        private void LoadExpenses()
        {
            try
            {
                var dt = _repository.GetExpenses();

                var totalAmount = ExpenseSummary.CalculateTotal(
                    dt.AsEnumerable()
                        .Where(row => row["Amount"] != DBNull.Value)
                        .Select(row => Convert.ToDecimal(row["Amount"])));
                var totalRow = dt.NewRow();
                totalRow["Id"] = DBNull.Value;
                totalRow["Amount"] = totalAmount;
                totalRow["Date"] = DBNull.Value;
                totalRow["Note"] = DBNull.Value;
                totalRow["CategoryId"] = DBNull.Value;
                totalRow["CategoryName"] = "TOTAL";
                dt.Rows.Add(totalRow);

                dgvExpenses.DataSource = dt;

                if (dt.Rows.Count > 0 &&
                    dt.Rows[dt.Rows.Count - 1]["CategoryName"]?.ToString() == "TOTAL")
                {
                    dgvExpenses.Rows[dgvExpenses.Rows.Count - 1].DefaultCellStyle.Font =
                        new Font(dgvExpenses.Font, FontStyle.Bold);
                }

                if (dgvExpenses.Columns.Contains("Id")) dgvExpenses.Columns["Id"].Visible = false;
                if (dgvExpenses.Columns.Contains("CategoryId")) dgvExpenses.Columns["CategoryId"].Visible = false;
                if (dgvExpenses.Columns.Contains("CategoryName"))
                {
                    dgvExpenses.Columns["CategoryName"].HeaderText = "Category";
                }
                SetExpenseColumnLayout();
            }
            catch (Exception ex)
            {
                ShowDatabaseError("Loading expenses", ex);
            }
        }

        private void SetExpenseColumnLayout()
        {
            var columns = new[]
            {
                (Name: "Amount", Weight: 18, MinimumWidth: 90),
                (Name: "Date", Weight: 25, MinimumWidth: 120),
                (Name: "Note", Weight: 32, MinimumWidth: 140),
                (Name: "CategoryName", Weight: 25, MinimumWidth: 130)
            };
            var physicalWidth = Math.Max(
                0,
                dgvExpenses.ClientSize.Width - SystemInformation.VerticalScrollBarWidth - 2);
            var availableWidth = physicalWidth;
            var minimumWidthTotal = columns.Sum(column => column.MinimumWidth);
            var remainingWidth = Math.Max(0, availableWidth - minimumWidthTotal);
            var remainingWeight = columns.Sum(column => column.Weight);

            foreach (var columnInfo in columns)
            {
                if (!dgvExpenses.Columns.Contains(columnInfo.Name))
                {
                    continue;
                }

                var column = dgvExpenses.Columns[columnInfo.Name];
                column.MinimumWidth = columnInfo.MinimumWidth;
                var proportionalWidth = remainingWeight == 0
                    ? 0
                    : remainingWidth * columnInfo.Weight / remainingWeight;
                column.Width = columnInfo.MinimumWidth + proportionalWidth;
                remainingWidth -= proportionalWidth;
                remainingWeight -= columnInfo.Weight;
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

            if (!ExpenseValidation.TryParseAmount(txtAmount.Text, out var amount))
            {
                MessageBox.Show($"Enter an amount greater than zero and no more than {ExpenseValidation.MaxAmount:N2}.");
                return;
            }

            var date = dtpDate.Value;
            var note = txtNote.Text.Trim();
            var row = (DataRowView)lstCategories.SelectedItem;
            var categoryId = Convert.ToInt32(row["Id"]);

            try
            {
                if (_isEditingExpense)
                {
                    _repository.UpdateExpense(_editingExpenseId, amount, date, note, categoryId);
                }
                else
                {
                    _repository.AddExpense(amount, date, note, categoryId);
                }

                ResetExpenseEditor();
                LoadExpenses();
            }
            catch (Exception ex)
            {
                ShowDatabaseError(_isEditingExpense ? "Updating expense" : "Adding expense", ex);
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
                if (idObj == null || idObj == DBNull.Value)
                {
                    MessageBox.Show("The total row cannot be deleted. Select an expense row.");
                    return;
                }

                var id = Convert.ToInt32(idObj);
                _repository.DeleteExpense(id);
                if (_isEditingExpense && id == _editingExpenseId)
                {
                    ResetExpenseEditor();
                }
                LoadExpenses();
            }
            catch (Exception ex)
            {
                ShowDatabaseError("Deleting expense", ex);
            }
        }

        private static void ShowDatabaseError(string operation, Exception exception)
        {
            var detail = exception is SqlException sqlException
                ? $"SQL error {sqlException.Number}"
                : exception.GetType().Name;
            MessageBox.Show(
                $"{operation} failed ({detail}). Check that the database is available and try again.",
                "Database error",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        }

        private void BeginEditSelectedExpense()
        {
            var currentRow = dgvExpenses.CurrentRow;
            if (currentRow == null || currentRow.Cells["Id"].Value == null ||
                currentRow.Cells["Id"].Value == DBNull.Value)
            {
                return;
            }

            var row = (DataRowView)currentRow.DataBoundItem;
            _editingExpenseId = Convert.ToInt32(row["Id"]);
            txtAmount.Text = Convert.ToDecimal(row["Amount"]).ToString(CultureInfo.CurrentCulture);
            dtpDate.Value = Convert.ToDateTime(row["Date"]);
            txtNote.Text = row["Note"] == DBNull.Value
                ? string.Empty
                : Convert.ToString(row["Note"]);
            lstCategories.SelectedValue = Convert.ToInt32(row["CategoryId"]);
            _isEditingExpense = true;
            btnAddExpense.Text = "Save Changes";
        }

        private void ResetExpenseEditor()
        {
            _isEditingExpense = false;
            _editingExpenseId = 0;
            btnAddExpense.Text = "Add Expense";
            txtAmount.Clear();
            txtNote.Clear();
        }
    }
}

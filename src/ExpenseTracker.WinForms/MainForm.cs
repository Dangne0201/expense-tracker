using System;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using ExpenseTracker.Core;
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
        private ExpenseOverviewService _expenseOverviewService;

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
        private DateTimePicker dtpFilterFrom;
        private DateTimePicker dtpFilterTo;
        private DateTimePicker dtpSummaryMonth;
        private ComboBox cboFilterCategory;
        private Button btnApplyFilters;
        private Button btnClearFilters;
        private Button btnExportExpenses;
        private Label lblMonthTotal;
        private Label lblExpenseStatus;
        private ErrorProvider _inputErrors;
        private bool _isEditingExpense;
        private int _editingExpenseId;
        private int _expenseLoadVersion;
        private CancellationTokenSource _expenseLoadCancellation;
        private IReadOnlyList<Expense> _visibleExpenses = Array.Empty<Expense>();

        private sealed record ExpenseGridRow(
            int? Id,
            decimal? Amount,
            DateTime? Date,
            string Note,
            int? CategoryId,
            string CategoryName,
            bool IsTotal);

        public MainForm()
        {
            // Resolve DB connectivity before building the form; the rest of the UI depends on it.
            EnsureDatabaseAvailable();
            _repository = new ExpenseRepository(_conn);
            _expenseOverviewService = new ExpenseOverviewService(_repository);
            InitializeComponents();
            Shown += async (s, e) =>
            {
                await LoadCategoriesAsync();
                await LoadExpensesAsync();
            };
            FormClosing += (s, e) => _expenseLoadCancellation?.Cancel();
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
            dgvExpenses.AccessibleName = "Expenses";
            dgvExpenses.AccessibleDescription = "Expense records and their total. Double-click an expense to edit it.";

            _inputErrors = new ErrorProvider
            {
                ContainerControl = this,
                BlinkStyle = ErrorBlinkStyle.NeverBlink
            };
            var expenseFilterPanel = CreateExpenseFilterPanel();

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
            txtNewCategory = new TextBox { Name = "txtNewCategory", Width = Math.Max(80, categoryPanelWidth - (categoryButtonWidth * 2) - 40), Anchor = AnchorStyles.Left | AnchorStyles.Right, Margin = new Padding(2, 6, 2, 6), AccessibleName = "New category name", AccessibleDescription = "Enter a category name up to 200 characters." };
            txtNewCategory.TextChanged += (s, e) => _inputErrors.SetError(txtNewCategory, string.Empty);
            btnAddCategory = new Button { Name = "btnAddCategory", Text = "Add", AutoSize = false, Width = categoryButtonWidth, Height = 44, Padding = new Padding(6), Margin = new Padding(2), TextAlign = ContentAlignment.MiddleCenter };

            footerLeft.Controls.Add(btnLoadCategories);
            footerLeft.Controls.Add(txtNewCategory);
            footerLeft.Controls.Add(btnAddCategory);
            btnLoadCategories.Click += async (s, e) => await LoadCategoriesAsync();
            btnAddCategory.Click += async (s, e) => await AddCategoryAsync();

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

            var lblAmount = new Label { Text = "&Amount", AutoSize = true, TextAlign = ContentAlignment.MiddleRight, Anchor = AnchorStyles.Right, Margin = new Padding(3, 8, 6, 3), AccessibleName = "Amount input label" };
            txtAmount = new TextBox
            {
                Name = "txtAmount",
                Width = 120,
                Anchor = AnchorStyles.Left | AnchorStyles.Right,
                Margin = new Padding(3, 6, 6, 6),
                AccessibleName = "Expense amount",
                AccessibleDescription =
                    $"Enter a positive amount using '{CultureInfo.CurrentCulture.NumberFormat.NumberDecimalSeparator}' as the decimal separator, with at most two decimal places."
            };
            txtAmount.TextChanged += (s, e) => _inputErrors.SetError(txtAmount, string.Empty);

            var lblDate = new Label { Text = "&Date", AutoSize = true, TextAlign = ContentAlignment.MiddleRight, Anchor = AnchorStyles.Right, Margin = new Padding(12, 8, 6, 3), AccessibleName = "Expense date label" };
            dtpDate = new DateTimePicker { Name = "dtpDate", Width = 130, Format = DateTimePickerFormat.Custom, CustomFormat = "yyyy-MM-dd HH:mm", Margin = new Padding(3, 6, 6, 6), AccessibleName = "Expense date and time" };

            var lblNote = new Label { Text = "&Note", AutoSize = true, TextAlign = ContentAlignment.MiddleRight, Anchor = AnchorStyles.Right, Margin = new Padding(12, 8, 6, 3), AccessibleName = "Expense note label" };
            txtNote = new TextBox { Name = "txtNote", Anchor = AnchorStyles.Left | AnchorStyles.Right, Margin = new Padding(3, 6, 6, 6), Width = 200, AccessibleName = "Expense note" };

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

            btnAddExpense.Click += async (s, e) => await AddExpenseAsync();
            btnLoadExpenses.Click += async (s, e) => await LoadExpensesAsync();
            btnDeleteExpense.Click += async (s, e) => await DeleteSelectedExpenseAsync();
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
            pnlRight.Controls.Add(expenseFilterPanel);
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

        private Panel CreateExpenseFilterPanel()
        {
            const int filterControlsHeight = 136;
            const int summaryRowSpacing = 5;
            var filterPanel = new Panel
            {
                Name = "expenseFilterPanel",
                Dock = DockStyle.Top,
                AccessibleName = "Expense filters"
            };
            var filters = new FlowLayoutPanel
            {
                Name = "expenseFilters",
                Location = Point.Empty,
                Height = filterControlsHeight,
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right,
                AutoScroll = false,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = true,
                Padding = new Padding(4)
            };
            var summaryRow = new Panel
            {
                Location = new Point(0, filterControlsHeight + summaryRowSpacing),
                Height = 32,
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
            };

            dtpFilterFrom = new DateTimePicker
            {
                Name = "dtpFilterFrom",
                Width = 142,
                Format = DateTimePickerFormat.Custom,
                CustomFormat = "'From' yyyy-MM-dd",
                ShowCheckBox = true,
                Checked = false,
                Margin = new Padding(3, 6, 3, 3),
                AccessibleName = "Filter expenses from date",
                AccessibleDescription = "Optional inclusive start date for expense filtering."
            };
            dtpFilterTo = new DateTimePicker
            {
                Name = "dtpFilterTo",
                Width = 142,
                Format = DateTimePickerFormat.Custom,
                CustomFormat = "'To' yyyy-MM-dd",
                ShowCheckBox = true,
                Checked = false,
                Margin = new Padding(3, 6, 3, 3),
                AccessibleName = "Filter expenses through date",
                AccessibleDescription = "Optional inclusive end date for expense filtering."
            };
            cboFilterCategory = new ComboBox
            {
                Name = "cboFilterCategory",
                Width = 145,
                DropDownStyle = ComboBoxStyle.DropDownList,
                Margin = new Padding(3, 6, 3, 3),
                AccessibleName = "Filter expenses by category"
            };
            btnApplyFilters = new Button
            {
                Name = "btnApplyFilters",
                Text = "Apply",
                Width = 76,
                Height = 32,
                Margin = new Padding(3, 5, 3, 3),
                AccessibleName = "Apply expense filters"
            };
            btnClearFilters = new Button
            {
                Name = "btnClearFilters",
                Text = "Clear",
                Width = 76,
                Height = 32,
                Margin = new Padding(3, 5, 3, 3),
                AccessibleName = "Clear expense filters"
            };
            btnExportExpenses = new Button
            {
                Name = "btnExportExpenses",
                Text = "Export CSV",
                Width = 96,
                Height = 32,
                Margin = new Padding(3, 5, 3, 3),
                AccessibleName = "Export filtered expenses to CSV"
            };
            filters.Controls.AddRange(new Control[]
            {
                dtpFilterFrom,
                dtpFilterTo,
                cboFilterCategory,
                btnApplyFilters,
                btnClearFilters,
                btnExportExpenses
            });

            dtpSummaryMonth = new DateTimePicker
            {
                Name = "dtpSummaryMonth",
                Location = new Point(8, 3),
                Width = 130,
                Format = DateTimePickerFormat.Custom,
                CustomFormat = "MMMM yyyy",
                ShowUpDown = true,
                Value = DateTime.Today,
                AccessibleName = "Month for monthly total"
            };
            lblMonthTotal = new Label
            {
                Name = "lblMonthTotal",
                AutoSize = true,
                Location = new Point(148, 8),
                Text = "Month total: --",
                AccessibleDescription = "Total expense amount for the selected month and category."
            };
            lblExpenseStatus = new Label
            {
                Name = "lblExpenseStatus",
                Dock = DockStyle.Bottom,
                Height = 23,
                Padding = new Padding(8, 3, 3, 3),
                Text = "Ready",
                AccessibleDescription = "Shows the current expense loading, empty, or result status."
            };
            summaryRow.Controls.Add(dtpSummaryMonth);
            summaryRow.Controls.Add(lblMonthTotal);
            filterPanel.Controls.Add(lblExpenseStatus);
            filterPanel.Controls.Add(summaryRow);
            filterPanel.Controls.Add(filters);
            var updatingFilterLayout = false;
            void UpdateFilterLayout()
            {
                if (updatingFilterLayout)
                {
                    return;
                }

                updatingFilterLayout = true;
                try
                {
                    filters.Width = filterPanel.ClientSize.Width;
                    filters.PerformLayout();
                    var controlsBottom = filters.Controls
                        .Cast<Control>()
                        .Max(control => control.Bottom + control.Margin.Bottom);
                    filters.Height = controlsBottom + filters.Padding.Bottom + 6;
                    summaryRow.Location = new Point(0, filters.Bottom + summaryRowSpacing);
                    summaryRow.Width = filterPanel.ClientSize.Width;
                    filterPanel.Height = summaryRow.Bottom + lblExpenseStatus.Height;
                }
                finally
                {
                    updatingFilterLayout = false;
                }
            }

            filterPanel.Resize += (s, e) => UpdateFilterLayout();
            UpdateFilterLayout();

            btnApplyFilters.Click += async (s, e) => await LoadExpensesAsync();
            btnClearFilters.Click += async (s, e) =>
            {
                dtpFilterFrom.Checked = false;
                dtpFilterTo.Checked = false;
                dtpSummaryMonth.Value = DateTime.Today;
                if (cboFilterCategory.Items.Count > 0)
                {
                    cboFilterCategory.SelectedValue = 0;
                }

                await LoadExpensesAsync();
            };
            btnExportExpenses.Click += (s, e) => ExportExpenses();

            return filterPanel;
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

                StartLocalDbInstance();

                if (TryOpenConnection(attachConn))
                {
                    _conn = attachConn;
                    return;
                }
            }

            StartLocalDbInstance();
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

        private bool TryOpenConnection(string connStr)
        {
            try
            {
                using var c = new SqlConnection(connStr);
                c.Open();
                c.Close();
                return true;
            }
            catch (Exception ex) when (ex is SqlException or InvalidOperationException or ArgumentException)
            {
                ApplicationDiagnostics.LogFailure("Database connection probe", ex);
                return false;
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
                if (p == null)
                {
                    ApplicationDiagnostics.LogFailure(
                        "Starting LocalDB",
                        new InvalidOperationException("The LocalDB startup process could not be created."));
                    return;
                }

                if (!p.WaitForExit(5000))
                {
                    ApplicationDiagnostics.LogFailure(
                        "Starting LocalDB",
                        new TimeoutException("The LocalDB startup process did not finish in time."));
                }
                else if (p.ExitCode != 0)
                {
                    ApplicationDiagnostics.LogFailure(
                        "Starting LocalDB",
                        new InvalidOperationException("The LocalDB startup process returned a failure code."));
                }
            }
            catch (System.ComponentModel.Win32Exception ex)
            {
                ApplicationDiagnostics.LogFailure("Starting LocalDB", ex);
            }
            catch (InvalidOperationException ex)
            {
                ApplicationDiagnostics.LogFailure("Starting LocalDB", ex);
            }
            catch (IOException ex)
            {
                ApplicationDiagnostics.LogFailure("Starting LocalDB", ex);
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
        private async Task LoadCategoriesAsync()
        {
            try
            {
                var previousFilterCategoryId = cboFilterCategory.SelectedValue is int selectedId
                    ? selectedId
                    : 0;
                var categories = await _repository.GetCategoriesAsync();
                lstCategories.DisplayMember = nameof(Category.Name);
                lstCategories.ValueMember = nameof(Category.Id);
                lstCategories.DataSource = categories;

                var filterCategories = new[] { new Category(0, "All categories") }
                    .Concat(categories)
                    .ToList();
                cboFilterCategory.DisplayMember = nameof(Category.Name);
                cboFilterCategory.ValueMember = nameof(Category.Id);
                cboFilterCategory.DataSource = filterCategories;
                cboFilterCategory.SelectedValue = filterCategories
                    .Any(category => category.Id == previousFilterCategoryId)
                    ? previousFilterCategoryId
                    : 0;
            }
            catch (OperationCanceledException)
            {
            }
            catch (Exception ex)
            {
                ShowDatabaseError("Loading categories", ex);
            }
        }

        private async Task AddCategoryAsync()
        {
            var name = txtNewCategory.Text.Trim();
            if (!ExpenseValidation.IsValidCategoryName(name))
            {
                _inputErrors.SetError(txtNewCategory, "Enter a category name with 1–200 characters.");
                txtNewCategory.Focus();
                return;
            }

            try
            {
                await _repository.AddCategoryAsync(name);
                txtNewCategory.Text = "";
                await LoadCategoriesAsync();
            }
            catch (SqlException ex) when (ex.Number is 2601 or 2627)
            {
                _inputErrors.SetError(txtNewCategory, "A category with this name already exists.");
                txtNewCategory.Focus();
            }
            catch (Exception ex)
            {
                ShowDatabaseError("Adding category", ex);
            }
        }

        /// <summary>
        /// Loads the expense grid with amount, date, note, and category name.
        /// </summary>
        private async Task LoadExpensesAsync()
        {
            var startDate = dtpFilterFrom.Checked ? dtpFilterFrom.Value.Date : (DateTime?)null;
            var endDate = dtpFilterTo.Checked ? dtpFilterTo.Value.Date : (DateTime?)null;
            var categoryId = cboFilterCategory.SelectedValue is int selectedCategoryId &&
                             selectedCategoryId > 0
                ? selectedCategoryId
                : (int?)null;
            var summaryMonth = dtpSummaryMonth.Value;

            if (startDate.HasValue && endDate.HasValue && startDate.Value > endDate.Value)
            {
                lblExpenseStatus.Text = "Start date must be on or before the end date.";
                dtpFilterFrom.Focus();
                return;
            }

            var requestVersion = System.Threading.Interlocked.Increment(ref _expenseLoadVersion);
            _expenseLoadCancellation?.Cancel();
            var loadCancellation = new CancellationTokenSource();
            _expenseLoadCancellation = loadCancellation;
            btnApplyFilters.Enabled = false;
            btnClearFilters.Enabled = false;
            UseWaitCursor = true;
            lblExpenseStatus.Text = "Loading expenses...";
            try
            {
                var filter = new ExpenseFilter(startDate, endDate, categoryId);
                var overview = await _expenseOverviewService.LoadAsync(
                    filter,
                    summaryMonth,
                    loadCancellation.Token);

                if (IsDisposed || requestVersion != Volatile.Read(ref _expenseLoadVersion))
                {
                    return;
                }

                var expenses = overview.Expenses;
                var monthlyTotal = overview.MonthlyTotal;
                _visibleExpenses = expenses;
                var totalAmount = ExpenseSummary.CalculateTotal(expenses.Select(expense => expense.Amount));
                var gridRows = expenses.Select(expense => new ExpenseGridRow(
                    expense.Id,
                    expense.Amount,
                    expense.Date,
                    expense.Note,
                    expense.CategoryId,
                    expense.CategoryName,
                    false)).ToList();
                gridRows.Add(new ExpenseGridRow(null, totalAmount, null, null, null, "TOTAL", true));
                dgvExpenses.DataSource = gridRows;
                dgvExpenses.Rows[dgvExpenses.Rows.Count - 1].DefaultCellStyle.Font =
                    new Font(dgvExpenses.Font, FontStyle.Bold);

                if (dgvExpenses.Columns.Contains("Id")) dgvExpenses.Columns["Id"].Visible = false;
                if (dgvExpenses.Columns.Contains("CategoryId")) dgvExpenses.Columns["CategoryId"].Visible = false;
                if (dgvExpenses.Columns.Contains("IsTotal")) dgvExpenses.Columns["IsTotal"].Visible = false;
                if (dgvExpenses.Columns.Contains("CategoryName"))
                {
                    dgvExpenses.Columns["CategoryName"].HeaderText = "Category";
                }
                SetExpenseColumnLayout();
                lblMonthTotal.Text =
                    $"Month total ({summaryMonth:MMMM yyyy}): {ExpenseSummary.FormatAmount(monthlyTotal)}";
                lblExpenseStatus.Text = expenses.Count == 0
                    ? "No expenses match these filters. Clear filters or add an expense."
                    : $"Showing {expenses.Count} expense{(expenses.Count == 1 ? string.Empty : "s")}.";
            }
            catch (OperationCanceledException) when (loadCancellation.IsCancellationRequested)
            {
            }
            catch (Exception ex)
            {
                if (requestVersion == Volatile.Read(ref _expenseLoadVersion))
                {
                    lblExpenseStatus.Text = "Unable to load expenses. Select Load Expenses to retry.";
                }
                ShowDatabaseError("Loading expenses", ex);
            }
            finally
            {
                if (!IsDisposed && requestVersion == Volatile.Read(ref _expenseLoadVersion))
                {
                    btnApplyFilters.Enabled = true;
                    btnClearFilters.Enabled = true;
                    UseWaitCursor = false;
                }

                if (ReferenceEquals(_expenseLoadCancellation, loadCancellation))
                {
                    _expenseLoadCancellation = null;
                }

                loadCancellation.Dispose();
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
            var dpiScale = dgvExpenses.DeviceDpi / 96d;
            var scaledColumns = columns.Select(column => (
                column.Name,
                column.Weight,
                MinimumWidth: Math.Max(1, (int)Math.Ceiling(column.MinimumWidth / dpiScale))))
                .ToArray();
            var minimumContentWidth = scaledColumns.Sum(column => column.MinimumWidth);
            var requiredClientWidth = minimumContentWidth + 2;
            var requiredScrollBars = dgvExpenses.ClientSize.Width < requiredClientWidth
                ? ScrollBars.Both
                : ScrollBars.Vertical;
            if (dgvExpenses.ScrollBars != requiredScrollBars)
            {
                dgvExpenses.ScrollBars = requiredScrollBars;
            }

            var availableWidth = Math.Max(0, dgvExpenses.ClientSize.Width - 2);
            var remainingWidth = Math.Max(0, availableWidth - minimumContentWidth);
            var remainingWeight = scaledColumns.Sum(column => column.Weight);
            foreach (var columnInfo in scaledColumns)
            {
                if (!dgvExpenses.Columns.Contains(columnInfo.Name))
                {
                    continue;
                }

                var column = dgvExpenses.Columns[columnInfo.Name];
                column.MinimumWidth = columnInfo.MinimumWidth;
                column.AutoSizeMode = DataGridViewAutoSizeColumnMode.None;
                var proportionalWidth = remainingWeight == 0
                    ? 0
                    : remainingWidth * columnInfo.Weight / remainingWeight;
                column.Width = columnInfo.MinimumWidth + proportionalWidth;
                remainingWidth -= proportionalWidth;
                remainingWeight -= columnInfo.Weight;
            }

            foreach (DataGridViewColumn column in dgvExpenses.Columns)
            {
                if (!columns.Any(columnInfo => columnInfo.Name == column.Name))
                {
                    column.AutoSizeMode = DataGridViewAutoSizeColumnMode.None;
                }
            }
        }

        private void ExportExpenses()
        {
            using var dialog = new SaveFileDialog
            {
                AddExtension = true,
                DefaultExt = "csv",
                FileName = $"expenses-{DateTime.Today:yyyy-MM-dd}.csv",
                Filter = "CSV files (*.csv)|*.csv",
                OverwritePrompt = true
            };
            if (dialog.ShowDialog(this) != DialogResult.OK)
            {
                return;
            }

            try
            {
                using var writer = new StreamWriter(dialog.FileName, false, new System.Text.UTF8Encoding(true));
                ExpenseCsvExporter.Write(writer, _visibleExpenses);
                lblExpenseStatus.Text = $"Exported {_visibleExpenses.Count} filtered expenses.";
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or ArgumentException)
            {
                ApplicationDiagnostics.LogFailure("CSV export", ex);
                MessageBox.Show(
                    "Could not export the CSV file. Check the destination and your write permissions.",
                    "Export failed",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }

        private async Task AddExpenseAsync()
        {
            if (lstCategories.Items.Count == 0)
            {
                MessageBox.Show("No category selected. Load categories and select one.");
                return;
            }

            if (lstCategories.SelectedItem is not Category selectedCategory)
            {
                MessageBox.Show("Select a category from the list.");
                return;
            }

            if (!ExpenseValidation.TryParseAmount(txtAmount.Text, out var amount))
            {
                _inputErrors.SetError(
                    txtAmount,
                    $"Enter a positive amount with at most two decimals, up to {ExpenseValidation.MaxAmount:N2}.");
                txtAmount.Focus();
                return;
            }

            var date = dtpDate.Value;
            var note = txtNote.Text.Trim();
            var categoryId = selectedCategory.Id;

            try
            {
                if (_isEditingExpense)
                {
                    var updated = await _repository.UpdateExpenseAsync(
                        _editingExpenseId, amount, date, note, categoryId);
                    if (!updated)
                    {
                        lblExpenseStatus.Text = "This expense no longer exists. The list has been refreshed.";
                    }
                }
                else
                {
                    await _repository.AddExpenseAsync(amount, date, note, categoryId);
                }

                ResetExpenseEditor();
                await LoadExpensesAsync();
            }
            catch (Exception ex)
            {
                ShowDatabaseError(_isEditingExpense ? "Updating expense" : "Adding expense", ex);
            }
        }

        private async Task DeleteSelectedExpenseAsync()
        {
            if (dgvExpenses.CurrentRow == null)
            {
                MessageBox.Show("Select an expense to delete.");
                return;
            }

            try
            {
                if (dgvExpenses.CurrentRow.DataBoundItem is not ExpenseGridRow { Id: int id, IsTotal: false })
                {
                    MessageBox.Show("The total row cannot be deleted. Select an expense row.");
                    return;
                }

                var confirmation = MessageBox.Show(
                    this,
                    "Delete the selected expense? This action cannot be undone.",
                    "Confirm deletion",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Warning,
                    MessageBoxDefaultButton.Button2);
                if (confirmation != DialogResult.Yes)
                {
                    return;
                }

                var deleted = await _repository.DeleteExpenseAsync(id);
                if (_isEditingExpense && id == _editingExpenseId)
                {
                    ResetExpenseEditor();
                }
                if (!deleted)
                {
                    lblExpenseStatus.Text = "This expense no longer exists. The list has been refreshed.";
                }

                await LoadExpensesAsync();
            }
            catch (Exception ex)
            {
                ShowDatabaseError("Deleting expense", ex);
            }
        }

        private static void ShowDatabaseError(string operation, Exception exception)
        {
            ApplicationDiagnostics.LogFailure(operation, exception);
            var sqlErrorNumber = exception is SqlException sqlException
                ? sqlException.Number
                : (int?)null;
            MessageBox.Show(
                DatabaseFailureMessages.ForOperation(operation, sqlErrorNumber),
                "Database error",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        }

        private void BeginEditSelectedExpense()
        {
            if (dgvExpenses.CurrentRow?.DataBoundItem is not ExpenseGridRow
                {
                    Id: int id,
                    Amount: decimal amount,
                    Date: DateTime date,
                    CategoryId: int categoryId,
                    IsTotal: false
                } row)
            {
                return;
            }

            _editingExpenseId = id;
            txtAmount.Text = amount.ToString(CultureInfo.CurrentCulture);
            dtpDate.Value = date;
            txtNote.Text = row.Note ?? string.Empty;
            lstCategories.SelectedValue = categoryId;
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

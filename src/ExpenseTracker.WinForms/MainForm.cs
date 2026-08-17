using System;
using System.Data;
using System.Windows.Forms;
using System.Drawing;
using Microsoft.Data.SqlClient;

namespace ExpenseTracker.WinForms
{
    public class MainForm : Form
    {
        private readonly string _conn = @"Server=(localdb)\\MSSQLLocalDB;Database=ExpenseDb;Trusted_Connection=True;";
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
            InitializeComponents();
        }

        private void InitializeComponents()
                {
                    Text = "Expense Tracker (WinForms)";
                    // Make form scale properly on high-DPI screens
                    this.AutoScaleMode = AutoScaleMode.Dpi;
                    this.Font = SystemFonts.MessageBoxFont;
                    Width = 1300;
                    Height = 640;
                                        this.MinimumSize = new Size(1300, 640);
                    this.MaximumSize = this.MinimumSize; // lock size
                    this.FormBorderStyle = FormBorderStyle.FixedSingle;
                    this.MaximizeBox = false;

                    // Root layout: two columns (left: categories, right: expenses)
                    var root = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 1 };
                    root.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 560));
                    root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
                    Controls.Add(root);

                    // Left panel - categories (uses Dock/Filling layout)
                    var pnlLeft = new Panel { Dock = DockStyle.Fill };
                    var lblCat = new Label { Text = "Categories", Dock = DockStyle.Top, Height = 22 };
                    lstCategories = new ListBox { Dock = DockStyle.Fill };

                    // Right panel - expenses (vertical layout)
                    var pnlRight = new Panel { Dock = DockStyle.Fill };
                    var lblExp = new Label { Text = "Expenses", Dock = DockStyle.Top, Height = 22 };

                    // DataGridView fills the available area between label and footer
                    dgvExpenses = new DataGridView { Dock = DockStyle.Fill, ReadOnly = true, AllowUserToAddRows = false, AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill };

                    // Footer: shared row so left/right footers align heights
                    var footer = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 1 };
                    footer.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 300));
                    footer.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

                    // Footer left: category controls
                    var footerLeft = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.LeftToRight, Padding = new Padding(6), WrapContents = false };
                    btnLoadCategories = new Button { Text = "Load", AutoSize = false, Width = 160, Height = 44, Padding = new Padding(6), Margin = new Padding(6), TextAlign = ContentAlignment.MiddleCenter };
                                        txtNewCategory = new TextBox { Width = 240, Margin = new Padding(6), Height = 30 };
                                        btnAddCategory = new Button { Text = "Add", AutoSize = false, Width = 120, Height = 44, Padding = new Padding(6), Margin = new Padding(6), TextAlign = ContentAlignment.MiddleCenter };
                    footerLeft.Controls.Add(btnLoadCategories);
                    footerLeft.Controls.Add(txtNewCategory);
                    footerLeft.Controls.Add(btnAddCategory);
                    btnLoadCategories.Click += (s, e) => LoadCategories();
                    btnAddCategory.Click += (s, e) => AddCategory();

                    // Footer right: inputs and expense buttons
                    var footerRight = new Panel { Dock = DockStyle.Fill };

                    var inputTable = new TableLayoutPanel { Dock = DockStyle.Top, Height = 40, AutoSize = false, ColumnCount = 6, RowCount = 1, Padding = new Padding(6) };
                    inputTable.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize)); // Amount label
                    inputTable.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 160)); // Amount textbox
                    inputTable.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize)); // Date label
                    inputTable.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 160)); // Date picker (smaller)
                    inputTable.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize)); // Note label
                    inputTable.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100)); // Note textbox (fill remaining)

                    var lblAmount = new Label { Text = "Amount", AutoSize = true, TextAlign = ContentAlignment.MiddleRight, Anchor = AnchorStyles.Right, Margin = new Padding(3, 8, 6, 3) };
                    txtAmount = new TextBox { Width = 120, Anchor = AnchorStyles.Left | AnchorStyles.Right, Margin = new Padding(3, 6, 6, 6) };

                    var lblDate = new Label { Text = "Date", AutoSize = true, TextAlign = ContentAlignment.MiddleRight, Anchor = AnchorStyles.Right, Margin = new Padding(12, 8, 6, 3) };
                    dtpDate = new DateTimePicker { Width = 160, Format = DateTimePickerFormat.Custom, CustomFormat = "yyyy-MM-dd HH:mm", Margin = new Padding(3, 6, 6, 6) };

                    var lblNote = new Label { Text = "Note", AutoSize = true, TextAlign = ContentAlignment.MiddleRight, Anchor = AnchorStyles.Right, Margin = new Padding(12, 8, 6, 3) };
                    txtNote = new TextBox { Anchor = AnchorStyles.Left | AnchorStyles.Right, Margin = new Padding(3, 6, 6, 6), Width = 320 };

                    inputTable.Controls.Add(lblAmount, 0, 0);
                    inputTable.Controls.Add(txtAmount, 1, 0);
                    inputTable.Controls.Add(lblDate, 2, 0);
                    inputTable.Controls.Add(dtpDate, 3, 0);
                    inputTable.Controls.Add(lblNote, 4, 0);
                    inputTable.Controls.Add(txtNote, 5, 0);

                    // Create a footerRightTable so the button row can be positioned under the Amount column
                    var footerRightTable = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 2, ColumnCount = 6 };
                    footerRightTable.RowStyles.Add(new RowStyle(SizeType.AutoSize));
                    footerRightTable.RowStyles.Add(new RowStyle(SizeType.AutoSize));
                    footerRightTable.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
                    footerRightTable.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 160));
                    footerRightTable.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
                    footerRightTable.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 160));
                    footerRightTable.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
                    footerRightTable.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

                    footerRightTable.Controls.Add(inputTable, 0, 0);
                    footerRightTable.SetColumnSpan(inputTable, 6);

                    // Create fixed-size buttons and put each into its own footer column so all are visible
                    btnDeleteExpense = new Button { Text = "Delete Expense", AutoSize = false, Width = 160, Height = 44, Padding = new Padding(6), Margin = new Padding(6), Anchor = AnchorStyles.Left, TextAlign = ContentAlignment.MiddleCenter };
                                        var btnLoadExpenses = new Button { Text = "Load", AutoSize = false, Width = 140, Height = 44, Padding = new Padding(6), Margin = new Padding(6), Anchor = AnchorStyles.Left, TextAlign = ContentAlignment.MiddleCenter };
                                        btnAddExpense = new Button { Text = "Add", AutoSize = false, Width = 140, Height = 44, Padding = new Padding(6), Margin = new Padding(6), Anchor = AnchorStyles.Left, TextAlign = ContentAlignment.MiddleCenter };

                    footerRightTable.Controls.Add(btnDeleteExpense, 1, 1);
                    footerRightTable.Controls.Add(btnLoadExpenses, 2, 1);
                    footerRightTable.Controls.Add(btnAddExpense, 3, 1);

                    btnAddExpense.Click += (s, e) => AddExpense();
                    btnLoadExpenses.Click += (s, e) => LoadExpenses();
                    btnDeleteExpense.Click += (s, e) => DeleteSelectedExpense();

                    footerRight.Controls.Add(footerRightTable);

                    // Assemble panels into root with footer row
                    // root has 2 rows: 0 = main area, 1 = footer fixed height
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
            if (string.IsNullOrEmpty(name)) { MessageBox.Show("Enter category name"); return; }
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

        private void LoadExpenses()
        {
            try
            {
                using var conn = new SqlConnection(_conn);
                using var cmd = new SqlCommand(@"SELECT e.Id, e.Amount, e.Date, e.Note, e.CategoryId, c.Name AS CategoryName
FROM Expenses e
JOIN Categories c ON e.CategoryId = c.Id
ORDER BY e.Date DESC", conn);
                var dt = new DataTable();
                using var da = new SqlDataAdapter(cmd);
                da.Fill(dt);
                dgvExpenses.DataSource = dt;
            }
            catch (Exception ex)
            {
                MessageBox.Show("LoadExpenses error: " + ex.Message);
            }
        }

        private void AddExpense()
        {
            if (lstCategories.Items.Count == 0) { MessageBox.Show("No category selected. Load categories and select one."); return; }
            if (lstCategories.SelectedItem == null) { MessageBox.Show("Select a category from the list."); return; }
            if (!decimal.TryParse(txtAmount.Text.Trim(), out var amount)) { MessageBox.Show("Invalid amount"); return; }
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
                if (idObj == null) { MessageBox.Show("Selected row has no Id."); return; }
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

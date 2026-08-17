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
                    Width = 900;
                    Height = 600;

                    // Root layout: two columns (left: categories, right: expenses)
                    var root = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 1 };
                    root.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 300));
                    root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
                    Controls.Add(root);

                    // Left panel - categories (uses Dock/Filling layout)
                    var pnlLeft = new Panel { Dock = DockStyle.Fill };
                    var lblCat = new Label { Text = "Categories", Dock = DockStyle.Top, Height = 22 };
                    lstCategories = new ListBox { Dock = DockStyle.Fill }; // fills available space between label and bottom panel

                    var leftBottom = new FlowLayoutPanel { Dock = DockStyle.Bottom, Height = 80, FlowDirection = FlowDirection.LeftToRight, Padding = new Padding(6), WrapContents = false };
                    btnLoadCategories = new Button { Text = "Load", AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink, Padding = new Padding(6) };
                    txtNewCategory = new TextBox { Width = 150 };
                    btnAddCategory = new Button { Text = "Add Category", AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink, Padding = new Padding(6) };
                    leftBottom.Controls.Add(btnLoadCategories);
                    leftBottom.Controls.Add(txtNewCategory);
                    leftBottom.Controls.Add(btnAddCategory);

                    btnLoadCategories.Click += (s, e) => LoadCategories();
                    btnAddCategory.Click += (s, e) => AddCategory();

                    pnlLeft.Controls.Add(lstCategories);
                    pnlLeft.Controls.Add(lblCat);
                    pnlLeft.Controls.Add(leftBottom);

                    // Right panel - expenses (vertical layout)
                    var pnlRight = new Panel { Dock = DockStyle.Fill };
                    var lblExp = new Label { Text = "Expenses", Dock = DockStyle.Top, Height = 22 };

                    // DataGridView fills the available area between label and input panel
                    dgvExpenses = new DataGridView { Dock = DockStyle.Fill, ReadOnly = true, AllowUserToAddRows = false, AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill };

                    // Input area: use a fixed-height Panel so left and right bottom areas reserve the same space
                    var inputPanel = new Panel { Dock = DockStyle.Bottom, Height = 80 };

                    // Inputs row (top of inputPanel)
                    var inputTable = new TableLayoutPanel { Dock = DockStyle.Top, Height = 40, AutoSize = false, ColumnCount = 6, RowCount = 1, Padding = new Padding(6) };
                    inputTable.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize)); // Amount label
                    inputTable.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 160)); // Amount textbox
                    inputTable.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize)); // Date label
                    inputTable.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 220)); // Date picker
                    inputTable.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize)); // Note label
                    inputTable.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100)); // Note textbox (fill remaining)

                    var lblAmount = new Label { Text = "Amount", AutoSize = true, TextAlign = ContentAlignment.MiddleRight, Anchor = AnchorStyles.Right, Margin = new Padding(3, 8, 6, 3) };
                    txtAmount = new TextBox { Width = 140, Anchor = AnchorStyles.Left | AnchorStyles.Right, Margin = new Padding(3, 6, 6, 6) };

                    var lblDate = new Label { Text = "Date", AutoSize = true, TextAlign = ContentAlignment.MiddleRight, Anchor = AnchorStyles.Right, Margin = new Padding(12, 8, 6, 3) };
                    dtpDate = new DateTimePicker { Width = 220, Format = DateTimePickerFormat.Custom, CustomFormat = "yyyy-MM-dd HH:mm", Margin = new Padding(3, 6, 6, 6) };

                    var lblNote = new Label { Text = "Note", AutoSize = true, TextAlign = ContentAlignment.MiddleRight, Anchor = AnchorStyles.Right, Margin = new Padding(12, 8, 6, 3) };
                    txtNote = new TextBox { Anchor = AnchorStyles.Left | AnchorStyles.Right, Margin = new Padding(3, 6, 6, 6) };

                    inputTable.Controls.Add(lblAmount, 0, 0);
                    inputTable.Controls.Add(txtAmount, 1, 0);
                    inputTable.Controls.Add(lblDate, 2, 0);
                    inputTable.Controls.Add(dtpDate, 3, 0);
                    inputTable.Controls.Add(lblNote, 4, 0);
                    inputTable.Controls.Add(txtNote, 5, 0);

                    // Button row below inputs — right-aligned by using RightToLeft flow
                    var btnRow = new FlowLayoutPanel { Dock = DockStyle.Bottom, Height = 36, FlowDirection = FlowDirection.RightToLeft, Padding = new Padding(6), WrapContents = false }; 
                    btnAddExpense = new Button { Text = "Add Expense", AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink, Padding = new Padding(6), Margin = new Padding(6) };
                    var btnLoadExpenses = new Button { Text = "Load Expenses", AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink, Padding = new Padding(6), Margin = new Padding(6) };
                    btnRow.Controls.Add(btnAddExpense);
                    btnRow.Controls.Add(btnLoadExpenses);

                    btnAddExpense.Click += (s, e) => AddExpense();
                    btnLoadExpenses.Click += (s, e) => LoadExpenses();

                    // Add controls to the inputPanel in correct order (inputs on top, buttons below)
                    inputPanel.Controls.Add(inputTable);
                    inputPanel.Controls.Add(btnRow);

                    // Add to pnlRight in proper order: label (top), dgv (fill), inputPanel (bottom)
                    pnlRight.Controls.Add(dgvExpenses);
                    pnlRight.Controls.Add(inputPanel);
                    pnlRight.Controls.Add(lblExp);

                    // Add panels to root layout
                    root.Controls.Add(pnlLeft, 0, 0);
                    root.Controls.Add(pnlRight, 1, 0);
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
    }
}

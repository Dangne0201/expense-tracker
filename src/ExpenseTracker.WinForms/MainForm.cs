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

            // Left panel - categories
            var pnlLeft = new Panel { Left = 10, Top = 10, Width = 280, Height = 540 };
            var lblCat = new Label { Text = "Categories", Left = 10, Top = 10, Width = 200 };
            lstCategories = new ListBox { Left = 10, Top = 35, Width = 260, Height = 300 };
            btnLoadCategories = new Button { Text = "Load", Left = 10, Top = 345 };
                        btnLoadCategories.AutoSize = true;
                        btnLoadCategories.AutoSizeMode = AutoSizeMode.GrowAndShrink;
                        btnLoadCategories.Padding = new Padding(6);
                        btnLoadCategories.MinimumSize = new Size(90, 30);
                        var szLoad = TextRenderer.MeasureText(btnLoadCategories.Text, this.Font);
                        btnLoadCategories.Size = new Size(Math.Max(btnLoadCategories.MinimumSize.Width, szLoad.Width + 20), Math.Max(btnLoadCategories.MinimumSize.Height, szLoad.Height + 10));
                        txtNewCategory = new TextBox { Left = 10, Top = 380, Width = 130 };
                        btnAddCategory = new Button { Text = "Add Category", Left = 150, Top = 378 };
                        btnAddCategory.AutoSize = true;
                        btnAddCategory.AutoSizeMode = AutoSizeMode.GrowAndShrink;
                        btnAddCategory.Padding = new Padding(6);
                        btnAddCategory.MinimumSize = new Size(110, 30);
                        var szAddCat = TextRenderer.MeasureText(btnAddCategory.Text, this.Font);
                        btnAddCategory.Size = new Size(Math.Max(btnAddCategory.MinimumSize.Width, szAddCat.Width + 20), Math.Max(btnAddCategory.MinimumSize.Height, szAddCat.Height + 10));

            btnLoadCategories.Click += (s, e) => LoadCategories();
            btnAddCategory.Click += (s, e) => AddCategory();

            pnlLeft.Controls.Add(lblCat);
            pnlLeft.Controls.Add(lstCategories);
            pnlLeft.Controls.Add(btnLoadCategories);
            pnlLeft.Controls.Add(txtNewCategory);
            pnlLeft.Controls.Add(btnAddCategory);
            Controls.Add(pnlLeft);

            // Right panel - expenses
            var pnlRight = new Panel { Left = 300, Top = 10, Width = 580, Height = 540 };
            var lblExp = new Label { Text = "Expenses", Left = 10, Top = 10, Width = 200 };
            dgvExpenses = new DataGridView { Left = 10, Top = 35, Width = 560, Height = 350, ReadOnly = true, AllowUserToAddRows = false };

            var lblAmount = new Label { Text = "Amount", Left = 10, Top = 400 };
            txtAmount = new TextBox { Left = 80, Top = 396, Width = 100 };
            var lblDate = new Label { Text = "Date", Left = 200, Top = 400 };
            dtpDate = new DateTimePicker { Left = 240, Top = 396, Width = 180, Format = DateTimePickerFormat.Custom, CustomFormat = "yyyy-MM-dd HH:mm" };
            var lblNote = new Label { Text = "Note", Left = 10, Top = 430 };
            txtNote = new TextBox { Left = 80, Top = 426, Width = 340 };
            btnAddExpense = new Button { Text = "Add Expense", Left = 440, Top = 426 };
                        btnAddExpense.AutoSize = true;
                        btnAddExpense.AutoSizeMode = AutoSizeMode.GrowAndShrink;
                        btnAddExpense.Padding = new Padding(6);
                        btnAddExpense.MinimumSize = new Size(110, 30);
                        var szAddExp = TextRenderer.MeasureText(btnAddExpense.Text, this.Font);
                        btnAddExpense.Size = new Size(Math.Max(btnAddExpense.MinimumSize.Width, szAddExp.Width + 20), Math.Max(btnAddExpense.MinimumSize.Height, szAddExp.Height + 10));
                        var btnLoadExpenses = new Button { Text = "Load Expenses", Left = 10, Top = 470 };
                        btnLoadExpenses.AutoSize = true;
                        btnLoadExpenses.AutoSizeMode = AutoSizeMode.GrowAndShrink;
                        btnLoadExpenses.Padding = new Padding(6);
                        btnLoadExpenses.MinimumSize = new Size(110, 30);
                        var szLoadExp = TextRenderer.MeasureText(btnLoadExpenses.Text, this.Font);
                        btnLoadExpenses.Size = new Size(Math.Max(btnLoadExpenses.MinimumSize.Width, szLoadExp.Width + 20), Math.Max(btnLoadExpenses.MinimumSize.Height, szLoadExp.Height + 10));

            btnAddExpense.Click += (s, e) => AddExpense();
            btnLoadExpenses.Click += (s, e) => LoadExpenses();

            pnlRight.Controls.Add(lblExp);
            pnlRight.Controls.Add(dgvExpenses);
            pnlRight.Controls.Add(lblAmount);
            pnlRight.Controls.Add(txtAmount);
            pnlRight.Controls.Add(lblDate);
            pnlRight.Controls.Add(dtpDate);
            pnlRight.Controls.Add(lblNote);
            pnlRight.Controls.Add(txtNote);
            pnlRight.Controls.Add(btnAddExpense);
            pnlRight.Controls.Add(btnLoadExpenses);
            Controls.Add(pnlRight);
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

using System.Data;
using Microsoft.Data.SqlClient;

namespace ExpenseTracker.Core;

public sealed class ExpenseRepository
{
    private readonly string _connectionString;

    public ExpenseRepository(string connectionString)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionString);
        _connectionString = connectionString;
    }

    public DataTable GetCategories()
    {
        using var connection = new SqlConnection(_connectionString);
        using var command = new SqlCommand(
            "SELECT Id, Name FROM Categories ORDER BY Name",
            connection);
        var table = new DataTable();
        using var adapter = new SqlDataAdapter(command);
        adapter.Fill(table);
        return table;
    }

    public void AddCategory(string name)
    {
        using var connection = new SqlConnection(_connectionString);
        using var command = new SqlCommand(
            "INSERT INTO Categories (Name) VALUES (@name)",
            connection);
        command.Parameters.Add("@name", SqlDbType.NVarChar, 200).Value = name;
        connection.Open();
        command.ExecuteNonQuery();
    }

    public DataTable GetExpenses(DateTime? startDate = null, DateTime? endDate = null, int? categoryId = null)
    {
        using var connection = new SqlConnection(_connectionString);
        using var command = new SqlCommand(
            @"SELECT e.Id, e.Amount, e.Date, e.Note, e.CategoryId, c.Name AS CategoryName
FROM Expenses e
JOIN Categories c ON e.CategoryId = c.Id
WHERE (@startDate IS NULL OR e.Date >= @startDate)
  AND (@endExclusive IS NULL OR e.Date < @endExclusive)
  AND (@categoryId IS NULL OR e.CategoryId = @categoryId)
ORDER BY e.Date DESC, e.Id DESC",
            connection);
        command.Parameters.Add("@startDate", SqlDbType.DateTime2).Value =
            startDate?.Date ?? (object)DBNull.Value;
        command.Parameters.Add("@endExclusive", SqlDbType.DateTime2).Value =
            endDate?.Date.AddDays(1) ?? (object)DBNull.Value;
        command.Parameters.Add("@categoryId", SqlDbType.Int).Value =
            categoryId ?? (object)DBNull.Value;
        var table = new DataTable();
        using var adapter = new SqlDataAdapter(command);
        adapter.Fill(table);
        return table;
    }

    public decimal GetMonthlyTotal(DateTime month, int? categoryId = null)
    {
        var monthStart = new DateTime(month.Year, month.Month, 1);
        using var connection = new SqlConnection(_connectionString);
        using var command = new SqlCommand(
            @"SELECT COALESCE(SUM(Amount), 0)
FROM Expenses
WHERE Date >= @monthStart AND Date < @nextMonth
  AND (@categoryId IS NULL OR CategoryId = @categoryId)",
            connection);
        command.Parameters.Add("@monthStart", SqlDbType.DateTime2).Value = monthStart;
        command.Parameters.Add("@nextMonth", SqlDbType.DateTime2).Value = monthStart.AddMonths(1);
        command.Parameters.Add("@categoryId", SqlDbType.Int).Value =
            categoryId ?? (object)DBNull.Value;
        connection.Open();
        return Convert.ToDecimal(command.ExecuteScalar());
    }

    public void AddExpense(decimal amount, DateTime date, string note, int categoryId)
    {
        using var connection = new SqlConnection(_connectionString);
        using var command = new SqlCommand(
            "INSERT INTO Expenses (Amount, Date, Note, CategoryId) VALUES (@amount, @date, @note, @categoryId)",
            connection);
        AddExpenseParameters(command, amount, date, note, categoryId);
        connection.Open();
        command.ExecuteNonQuery();
    }

    public void UpdateExpense(int id, decimal amount, DateTime date, string note, int categoryId)
    {
        using var connection = new SqlConnection(_connectionString);
        using var command = new SqlCommand(
            @"UPDATE Expenses
SET Amount = @amount, Date = @date, Note = @note, CategoryId = @categoryId
WHERE Id = @id",
            connection);
        AddExpenseParameters(command, amount, date, note, categoryId);
        command.Parameters.Add("@id", SqlDbType.Int).Value = id;
        connection.Open();
        command.ExecuteNonQuery();
    }

    public int DeleteExpense(int id)
    {
        using var connection = new SqlConnection(_connectionString);
        using var command = new SqlCommand(
            "DELETE FROM Expenses WHERE Id = @id",
            connection);
        command.Parameters.Add("@id", SqlDbType.Int).Value = id;
        connection.Open();
        return command.ExecuteNonQuery();
    }

    private static void AddExpenseParameters(
        SqlCommand command,
        decimal amount,
        DateTime date,
        string note,
        int categoryId)
    {
        var amountParameter = command.Parameters.Add("@amount", SqlDbType.Decimal);
        amountParameter.Precision = 18;
        amountParameter.Scale = 2;
        amountParameter.Value = amount;
        command.Parameters.Add("@date", SqlDbType.DateTime2).Value = date;
        command.Parameters.Add("@note", SqlDbType.NVarChar, -1).Value =
            string.IsNullOrEmpty(note) ? DBNull.Value : note;
        command.Parameters.Add("@categoryId", SqlDbType.Int).Value = categoryId;
    }
}

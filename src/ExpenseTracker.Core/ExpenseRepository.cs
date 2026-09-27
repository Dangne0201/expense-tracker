using System.Data;
using Microsoft.Data.SqlClient;

namespace ExpenseTracker.Core;

public sealed class ExpenseRepository : IExpenseRepository
{
    private readonly string _connectionString;

    public ExpenseRepository(string connectionString)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionString);
        _connectionString = connectionString;
    }

    public async Task<IReadOnlyList<Category>> GetCategoriesAsync(CancellationToken cancellationToken = default)
    {
        var categories = new List<Category>();
        await using var connection = new SqlConnection(_connectionString);
        await using var command = new SqlCommand(
            "SELECT Id, Name FROM Categories ORDER BY Name",
            connection);
        await connection.OpenAsync(cancellationToken);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            categories.Add(new Category(reader.GetInt32(0), reader.GetString(1)));
        }

        return categories;
    }

    public async Task AddCategoryAsync(string name, CancellationToken cancellationToken = default)
    {
        await using var connection = new SqlConnection(_connectionString);
        await using var command = new SqlCommand(
            "INSERT INTO Categories (Name) VALUES (@name)",
            connection);
        command.Parameters.Add("@name", SqlDbType.NVarChar, 200).Value = name;
        await connection.OpenAsync(cancellationToken);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Expense>> GetExpensesAsync(
        ExpenseFilter filter,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(filter);
        filter.Validate();

        var expenses = new List<Expense>();
        await using var connection = new SqlConnection(_connectionString);
        await using var command = new SqlCommand(
            @"SELECT e.Id, e.Amount, e.Date, e.Note, e.CategoryId, c.Name AS CategoryName
FROM Expenses e
JOIN Categories c ON e.CategoryId = c.Id
WHERE (@startDate IS NULL OR e.Date >= @startDate)
  AND (@endExclusive IS NULL OR e.Date < @endExclusive)
  AND (@categoryId IS NULL OR e.CategoryId = @categoryId)
ORDER BY e.Date DESC, e.Id DESC",
            connection);
        command.Parameters.Add("@startDate", SqlDbType.DateTime2).Value =
            filter.StartDate?.Date ?? (object)DBNull.Value;
        command.Parameters.Add("@endExclusive", SqlDbType.DateTime2).Value =
            filter.EndDate?.Date.AddDays(1) ?? (object)DBNull.Value;
        command.Parameters.Add("@categoryId", SqlDbType.Int).Value =
            filter.CategoryId ?? (object)DBNull.Value;

        await connection.OpenAsync(cancellationToken);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            expenses.Add(new Expense(
                reader.GetInt32(0),
                reader.GetDecimal(1),
                reader.GetDateTime(2),
                reader.IsDBNull(3) ? null : reader.GetString(3),
                reader.GetInt32(4),
                reader.GetString(5)));
        }

        return expenses;
    }

    public async Task<decimal> GetMonthlyTotalAsync(
        DateTime month,
        int? categoryId = null,
        CancellationToken cancellationToken = default)
    {
        if (categoryId is <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(categoryId), "A category ID must be positive.");
        }

        var monthStart = new DateTime(month.Year, month.Month, 1);
        await using var connection = new SqlConnection(_connectionString);
        await using var command = new SqlCommand(
            @"SELECT COALESCE(SUM(Amount), 0)
FROM Expenses
WHERE Date >= @monthStart AND Date < @nextMonth
  AND (@categoryId IS NULL OR CategoryId = @categoryId)",
            connection);
        command.Parameters.Add("@monthStart", SqlDbType.DateTime2).Value = monthStart;
        command.Parameters.Add("@nextMonth", SqlDbType.DateTime2).Value = monthStart.AddMonths(1);
        command.Parameters.Add("@categoryId", SqlDbType.Int).Value =
            categoryId ?? (object)DBNull.Value;
        await connection.OpenAsync(cancellationToken);
        var result = await command.ExecuteScalarAsync(cancellationToken);
        return Convert.ToDecimal(result);
    }

    public async Task AddExpenseAsync(
        decimal amount,
        DateTime date,
        string? note,
        int categoryId,
        CancellationToken cancellationToken = default)
    {
        await using var connection = new SqlConnection(_connectionString);
        await using var command = new SqlCommand(
            "INSERT INTO Expenses (Amount, Date, Note, CategoryId) VALUES (@amount, @date, @note, @categoryId)",
            connection);
        AddExpenseParameters(command, amount, date, note, categoryId);
        await connection.OpenAsync(cancellationToken);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task<bool> UpdateExpenseAsync(
        int id,
        decimal amount,
        DateTime date,
        string? note,
        int categoryId,
        CancellationToken cancellationToken = default)
    {
        await using var connection = new SqlConnection(_connectionString);
        await using var command = new SqlCommand(
            @"UPDATE Expenses
SET Amount = @amount, Date = @date, Note = @note, CategoryId = @categoryId
WHERE Id = @id",
            connection);
        AddExpenseParameters(command, amount, date, note, categoryId);
        command.Parameters.Add("@id", SqlDbType.Int).Value = id;
        await connection.OpenAsync(cancellationToken);
        return await command.ExecuteNonQueryAsync(cancellationToken) == 1;
    }

    public async Task<bool> DeleteExpenseAsync(int id, CancellationToken cancellationToken = default)
    {
        await using var connection = new SqlConnection(_connectionString);
        await using var command = new SqlCommand(
            "DELETE FROM Expenses WHERE Id = @id",
            connection);
        command.Parameters.Add("@id", SqlDbType.Int).Value = id;
        await connection.OpenAsync(cancellationToken);
        return await command.ExecuteNonQueryAsync(cancellationToken) == 1;
    }

    private static void AddExpenseParameters(
        SqlCommand command,
        decimal amount,
        DateTime date,
        string? note,
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

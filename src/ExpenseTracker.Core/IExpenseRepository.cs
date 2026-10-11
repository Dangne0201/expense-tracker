namespace ExpenseTracker.Core;

// Defines the asynchronous category and expense operations used by services and repository fakes.
public interface IExpenseRepository
{
    // Returns categories for display and selection.
    Task<IReadOnlyList<Category>> GetCategoriesAsync(CancellationToken cancellationToken = default);

    // Persists a category; database constraints enforce uniqueness and valid names.
    Task AddCategoryAsync(string name, CancellationToken cancellationToken = default);

    // Returns expenses matching the optional date and category filters.
    Task<IReadOnlyList<Expense>> GetExpensesAsync(
        ExpenseFilter filter,
        CancellationToken cancellationToken = default);

    // Calculates a calendar-month total independently of any expense-list date range.
    Task<decimal> GetMonthlyTotalAsync(
        DateTime month,
        int? categoryId = null,
        CancellationToken cancellationToken = default);

    // Creates an expense with its amount, date, optional note, and category.
    Task AddExpenseAsync(
        decimal amount,
        DateTime date,
        string? note,
        int categoryId,
        CancellationToken cancellationToken = default);

    // Updates an existing expense and reports whether exactly one row was changed.
    Task<bool> UpdateExpenseAsync(
        int id,
        decimal amount,
        DateTime date,
        string? note,
        int categoryId,
        CancellationToken cancellationToken = default);

    // Deletes an expense and reports whether exactly one row was removed.
    Task<bool> DeleteExpenseAsync(int id, CancellationToken cancellationToken = default);
}

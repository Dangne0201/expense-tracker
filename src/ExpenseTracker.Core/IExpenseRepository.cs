namespace ExpenseTracker.Core;

public interface IExpenseRepository
{
    Task<IReadOnlyList<Category>> GetCategoriesAsync(CancellationToken cancellationToken = default);

    Task AddCategoryAsync(string name, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Expense>> GetExpensesAsync(
        ExpenseFilter filter,
        CancellationToken cancellationToken = default);

    Task<decimal> GetMonthlyTotalAsync(
        DateTime month,
        int? categoryId = null,
        CancellationToken cancellationToken = default);

    Task AddExpenseAsync(
        decimal amount,
        DateTime date,
        string? note,
        int categoryId,
        CancellationToken cancellationToken = default);

    Task<bool> UpdateExpenseAsync(
        int id,
        decimal amount,
        DateTime date,
        string? note,
        int categoryId,
        CancellationToken cancellationToken = default);

    Task<bool> DeleteExpenseAsync(int id, CancellationToken cancellationToken = default);
}

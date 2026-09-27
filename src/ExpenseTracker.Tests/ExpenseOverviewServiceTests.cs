using ExpenseTracker.Core;

namespace ExpenseTracker.Tests;

public class ExpenseOverviewServiceTests
{
    [Fact]
    public async Task LoadAsync_returns_filtered_expenses_and_independent_monthly_total()
    {
        var repository = new FakeExpenseRepository();
        var service = new ExpenseOverviewService(repository);
        var filter = new ExpenseFilter(
            new DateTime(2026, 9, 10),
            new DateTime(2026, 9, 20),
            CategoryId: 7);
        var month = new DateTime(2026, 9, 27);

        var overview = await service.LoadAsync(filter, month);

        Assert.Same(repository.Expenses, overview.Expenses);
        Assert.Equal(42.50m, overview.MonthlyTotal);
        Assert.Equal(filter, repository.RequestedFilter);
        Assert.Equal(month, repository.RequestedMonth);
        Assert.Equal(7, repository.RequestedCategoryId);
    }

    private sealed class FakeExpenseRepository : IExpenseRepository
    {
        public IReadOnlyList<Expense> Expenses { get; } = new[]
        {
            new Expense(1, 12.50m, new DateTime(2026, 9, 14), "Lunch", 7, "Food")
        };

        public ExpenseFilter? RequestedFilter { get; private set; }

        public DateTime? RequestedMonth { get; private set; }

        public int? RequestedCategoryId { get; private set; }

        public Task<IReadOnlyList<Category>> GetCategoriesAsync(CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task AddCategoryAsync(string name, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<IReadOnlyList<Expense>> GetExpensesAsync(
            ExpenseFilter filter,
            CancellationToken cancellationToken = default)
        {
            RequestedFilter = filter;
            return Task.FromResult(Expenses);
        }

        public Task<decimal> GetMonthlyTotalAsync(
            DateTime month,
            int? categoryId = null,
            CancellationToken cancellationToken = default)
        {
            RequestedMonth = month;
            RequestedCategoryId = categoryId;
            return Task.FromResult(42.50m);
        }

        public Task AddExpenseAsync(
            decimal amount,
            DateTime date,
            string? note,
            int categoryId,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<bool> UpdateExpenseAsync(
            int id,
            decimal amount,
            DateTime date,
            string? note,
            int categoryId,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<bool> DeleteExpenseAsync(int id, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();
    }
}

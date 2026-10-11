namespace ExpenseTracker.Core;

// Combines the filtered expense rows with the selected month's independent total.
public sealed record ExpenseOverview(
    IReadOnlyList<Expense> Expenses,
    decimal MonthlyTotal);

public sealed class ExpenseOverviewService
{
    private readonly IExpenseRepository _repository;

    // Injects the repository so overview orchestration can be tested without SQL Server.
    public ExpenseOverviewService(IExpenseRepository repository)
    {
        ArgumentNullException.ThrowIfNull(repository);
        _repository = repository;
    }

    // Loads the grid result and monthly summary concurrently, applying the category to both.
    public async Task<ExpenseOverview> LoadAsync(
        ExpenseFilter filter,
        DateTime summaryMonth,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(filter);
        filter.Validate();

        var expensesTask = _repository.GetExpensesAsync(filter, cancellationToken);
        var monthlyTotalTask = _repository.GetMonthlyTotalAsync(
            summaryMonth,
            filter.CategoryId,
            cancellationToken);
        await Task.WhenAll(expensesTask, monthlyTotalTask);

        return new ExpenseOverview(await expensesTask, await monthlyTotalTask);
    }
}

namespace ExpenseTracker.Core;

public sealed record Expense(
    int Id,
    decimal Amount,
    DateTime Date,
    string? Note,
    int CategoryId,
    string CategoryName);

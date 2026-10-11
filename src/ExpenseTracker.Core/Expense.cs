namespace ExpenseTracker.Core;

// Represents an expense together with the category name needed for display.
public sealed record Expense(
    int Id,
    decimal Amount,
    DateTime Date,
    string? Note,
    int CategoryId,
    string CategoryName);

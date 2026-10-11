namespace ExpenseTracker.Core;

// Represents a category row shared by the UI and data-access layers.
public sealed record Category(int Id, string Name);

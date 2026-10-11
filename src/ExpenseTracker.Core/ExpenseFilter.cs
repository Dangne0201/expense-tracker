namespace ExpenseTracker.Core;

// Holds optional expense-list filters; date bounds are calendar dates and category IDs must be positive.
public sealed record ExpenseFilter(
    DateTime? StartDate = null,
    DateTime? EndDate = null,
    int? CategoryId = null)
{
    // Rejects invalid ranges and non-positive category IDs before repository queries run.
    public void Validate()
    {
        if (StartDate.HasValue && EndDate.HasValue && StartDate.Value.Date > EndDate.Value.Date)
        {
            throw new ArgumentException("The start date must be on or before the end date.");
        }

        if (CategoryId is <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(CategoryId), "A category ID must be positive.");
        }
    }
}

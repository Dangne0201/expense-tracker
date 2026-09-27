namespace ExpenseTracker.Core;

public sealed record ExpenseFilter(
    DateTime? StartDate = null,
    DateTime? EndDate = null,
    int? CategoryId = null)
{
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

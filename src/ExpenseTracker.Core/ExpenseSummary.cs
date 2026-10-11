using System.Globalization;

namespace ExpenseTracker.Core;

// Provides shared expense-total and current-culture display formatting.
public static class ExpenseSummary
{
    // Sums the supplied amounts; an empty collection produces zero.
    public static decimal CalculateTotal(IEnumerable<decimal> amounts)
    {
        ArgumentNullException.ThrowIfNull(amounts);
        return amounts.Sum();
    }

    // Formats an amount as currency using the user's current culture.
    public static string FormatAmount(decimal amount)
    {
        return amount.ToString("C2", CultureInfo.CurrentCulture);
    }
}

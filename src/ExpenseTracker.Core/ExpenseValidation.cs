using System.Globalization;

namespace ExpenseTracker.Core;

// Applies UI-level rules for expense amounts and category names before persistence.
public static class ExpenseValidation
{
    public const decimal MaxAmount = 9999999999999999.99m;

    // Parses using the current culture and accepts only positive DECIMAL(18,2)-compatible values.
    public static bool TryParseAmount(string input, out decimal amount)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            amount = 0;
            return false;
        }

        if (!decimal.TryParse(
                input.Trim(),
                NumberStyles.AllowDecimalPoint | NumberStyles.AllowLeadingSign | NumberStyles.AllowThousands,
                CultureInfo.CurrentCulture,
                out amount))
        {
            return false;
        }

        return amount > 0 &&
               amount <= MaxAmount &&
               decimal.Round(amount, 2) == amount;
    }

    // Category names must contain non-whitespace text and fit the database's 200-character limit.
    public static bool IsValidCategoryName(string name)
    {
        return !string.IsNullOrWhiteSpace(name) && name.Trim().Length <= 200;
    }
}

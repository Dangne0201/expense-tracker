using System.Globalization;

namespace ExpenseTracker.Core;

public static class ExpenseCsvExporter
{
    public static void Write(TextWriter writer, IEnumerable<Expense> expenses)
    {
        ArgumentNullException.ThrowIfNull(writer);
        ArgumentNullException.ThrowIfNull(expenses);

        writer.WriteLine("Amount,Date,Note,Category");
        foreach (var expense in expenses)
        {
            writer.WriteLine(string.Join(",",
                Escape(expense.Amount.ToString("F2", CultureInfo.InvariantCulture)),
                Escape(expense.Date.ToString("yyyy-MM-ddTHH:mm:ss.fffffff", CultureInfo.InvariantCulture)),
                Escape(expense.Note ?? string.Empty),
                Escape(expense.CategoryName)));
        }
    }

    private static string Escape(string value)
    {
        var firstNonWhitespace = value.FirstOrDefault(character => !char.IsWhiteSpace(character));
        if (firstNonWhitespace is '=' or '+' or '-' or '@')
        {
            value = "'" + value;
        }

        return $"\"{value.Replace("\"", "\"\"")}\"";
    }
}

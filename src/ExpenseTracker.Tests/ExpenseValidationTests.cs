using System.Globalization;
using System.IO;
using ExpenseTracker.Core;
using Xunit;

namespace ExpenseTracker.Tests;

public class ExpenseValidationTests
{
    [Fact]
    public void ExpenseTotal_sums_amounts_and_returns_zero_for_no_expenses()
    {
        Assert.Equal(30.75m, ExpenseSummary.CalculateTotal(new[] { 12.50m, 18.25m }));
        Assert.Equal(0m, ExpenseSummary.CalculateTotal(Array.Empty<decimal>()));
    }

    [Fact]
    public void ExpenseAmount_format_uses_the_current_culture_currency()
    {
        var originalCulture = CultureInfo.CurrentCulture;
        try
        {
            var culture = CultureInfo.GetCultureInfo("en-US");
            CultureInfo.CurrentCulture = culture;

            var formatted = ExpenseSummary.FormatAmount(1234.5m);

            Assert.Contains(culture.NumberFormat.CurrencySymbol, formatted);
            Assert.Contains("1,234.50", formatted);
        }
        finally
        {
            CultureInfo.CurrentCulture = originalCulture;
        }
    }

    [Fact]
    public void TryParseAmount_accepts_positive_values_in_current_culture()
    {
        var originalCulture = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("en-US");
            Assert.True(ExpenseValidation.TryParseAmount("12.50", out var amount));
            Assert.Equal(12.50m, amount);
            Assert.True(ExpenseValidation.TryParseAmount("1,234.56", out amount));
            Assert.Equal(1234.56m, amount);
        }
        finally
        {
            CultureInfo.CurrentCulture = originalCulture;
        }
    }

    [Theory]
    [InlineData("en-US", "1,234.56", "1234.56")]
    [InlineData("vi-VN", "1.234,56", "1234.56")]
    public void TryParseAmount_supports_group_and_decimal_separators_for_culture(
        string cultureName,
        string input,
        string expected)
    {
        var originalCulture = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo(cultureName);
            Assert.True(ExpenseValidation.TryParseAmount(input, out var amount));
            Assert.Equal(decimal.Parse(expected, CultureInfo.InvariantCulture), amount);
        }
        finally
        {
            CultureInfo.CurrentCulture = originalCulture;
        }
    }

    [Theory]
    [InlineData("9999999999999999.99", true)]
    [InlineData("10000000000000000.00", false)]
    [InlineData("0.01", true)]
    [InlineData("0.001", false)]
    public void TryParseAmount_checks_decimal_18_2_boundaries(string input, bool expected)
    {
        var originalCulture = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("en-US");
            Assert.Equal(expected, ExpenseValidation.TryParseAmount(input, out _));
        }
        finally
        {
            CultureInfo.CurrentCulture = originalCulture;
        }
    }

    [Theory]
    [InlineData("")]
    [InlineData("0")]
    [InlineData("-1")]
    [InlineData("not-a-number")]
    [InlineData("1.239")]
    [InlineData("10000000000000000.00")]
    public void TryParseAmount_rejects_invalid_or_non_positive_values(string input)
    {
        Assert.False(ExpenseValidation.TryParseAmount(input, out _));
    }

    [Fact]
    public void CategoryName_rejects_names_longer_than_schema()
    {
        Assert.False(ExpenseValidation.IsValidCategoryName(new string('x', 201)));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void CategoryName_rejects_empty_names(string? name)
    {
        Assert.False(ExpenseValidation.IsValidCategoryName(name!));
    }

    [Theory]
    [InlineData("Food")]
    [InlineData("  Transport  ")]
    public void CategoryName_accepts_non_empty_names(string name)
    {
        Assert.True(ExpenseValidation.IsValidCategoryName(name));
    }

    [Fact]
    public void ExpenseFilter_rejects_a_start_date_after_the_end_date()
    {
        var filter = new ExpenseFilter(new DateTime(2026, 5, 2), new DateTime(2026, 5, 1));

        Assert.Throws<ArgumentException>(filter.Validate);
    }

    [Fact]
    public void ExpenseFilter_rejects_non_positive_category_ids()
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () => new ExpenseFilter(CategoryId: 0).Validate());
    }

    [Fact]
    public void CsvExport_uses_invariant_values_quotes_special_fields_and_neutralizes_formulas()
    {
        var expense = new Expense(
            1,
            12.5m,
            new DateTime(2026, 9, 27, 14, 5, 6),
            "=HYPERLINK(\"bad\"),\nLunch with \"quotes\"",
            2,
            "Food");
        using var writer = new StringWriter(CultureInfo.GetCultureInfo("fr-FR"));

        ExpenseCsvExporter.Write(writer, new[] { expense });

        var csv = writer.ToString();
        Assert.Contains("12.50", csv);
        Assert.Contains("\"Food\"", csv);
        Assert.Contains("\"'=HYPERLINK(\"\"bad\"\"),\nLunch with \"\"quotes\"\"\"", csv);
        Assert.Contains("'=HYPERLINK", csv);
        Assert.DoesNotContain("12,50", csv);
    }

    [Fact]
    public void CsvExport_preserves_unicode_text()
    {
        var expense = new Expense(
            2,
            8m,
            new DateTime(2026, 9, 27),
            "Cà phê ☕",
            3,
            "Ăn uống");
        using var writer = new StringWriter(CultureInfo.InvariantCulture);

        ExpenseCsvExporter.Write(writer, new[] { expense });

        Assert.Contains("Cà phê ☕", writer.ToString());
        Assert.Contains("Ăn uống", writer.ToString());
    }

    [Theory]
    [InlineData(53, "unavailable")]
    [InlineData(10061, "unavailable")]
    [InlineData(2601, "unique")]
    [InlineData(2627, "unique")]
    [InlineData(547, "database rule")]
    public void Database_error_messages_explain_expected_failure_categories(
        int sqlErrorNumber,
        string expectedText)
    {
        var message = DatabaseFailureMessages.ForOperation("Saving expense", sqlErrorNumber);

        Assert.Contains(expectedText, message, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("connection string", message, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("password", message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Repository_reports_unavailable_loopback_sql_server_without_credentials()
    {
        var repository = new ExpenseRepository(
            "Server=127.0.0.1,1;Database=ExpenseDb;Connect Timeout=1;Encrypt=False;TrustServerCertificate=True");

        var exception = await Assert.ThrowsAsync<Microsoft.Data.SqlClient.SqlException>(
            () => repository.GetCategoriesAsync());

        Assert.Contains(
            "database is unavailable",
            DatabaseFailureMessages.ForOperation("Loading categories", exception.Number),
            StringComparison.OrdinalIgnoreCase);
    }
}

using System.Data;
using ExpenseTracker.Core;
using Microsoft.Data.SqlClient;

namespace ExpenseTracker.Tests;

public class ExpenseRepositoryIntegrationTests
{
    [Fact]
    [Trait("Category", "Integration")]
    public void Repository_supports_crud_date_category_filters_and_monthly_totals()
    {
        var connectionString = DbIntegrationTests.GetLocalTestConnectionString();
        var repository = new ExpenseRepository(connectionString);
        var categoryName = "RepositoryTest-" + Guid.NewGuid().ToString("N")[..10];
        var categoryId = 0;

        try
        {
            repository.AddCategory(categoryName);
            categoryId = FindCategoryId(repository.GetCategories(), categoryName);
            Assert.Empty(repository.GetExpenses(categoryId: categoryId).AsEnumerable());

            var month = new DateTime(2026, 4, 1);
            Assert.Equal(0m, repository.GetMonthlyTotal(month, categoryId));
            repository.AddExpense(10.25m, month.AddDays(5), string.Empty, categoryId);
            repository.AddExpense(5.25m, month.AddMonths(1), "next-month", categoryId);

            var monthEnd = month.AddMonths(1).AddDays(-1);
            var aprilExpenses = repository.GetExpenses(month, monthEnd, categoryId);
            Assert.Single(aprilExpenses.AsEnumerable());
            Assert.Equal(DBNull.Value, aprilExpenses.Rows[0]["Note"]);
            Assert.Equal(10.25m, repository.GetMonthlyTotal(month, categoryId));
            Assert.Equal(10.25m, repository.GetMonthlyTotal(month));
            Assert.Equal(5.25m, repository.GetMonthlyTotal(month.AddMonths(1), categoryId));
            Assert.Equal("next-month",
                repository.GetExpenses(month.AddMonths(1), month.AddMonths(1), categoryId).Rows[0]["Note"]);

            var expenseId = Convert.ToInt32(aprilExpenses.Rows[0]["Id"]);
            repository.UpdateExpense(expenseId, 22.75m, month.AddMonths(1).AddTicks(-1), "updated", categoryId);
            Assert.Equal(22.75m, repository.GetMonthlyTotal(month, categoryId));

            Assert.Equal(1, repository.DeleteExpense(expenseId));
            Assert.Equal(0, repository.DeleteExpense(expenseId));
            Assert.Empty(repository.GetExpenses(month, monthEnd, categoryId).AsEnumerable());
        }
        finally
        {
            using var connection = new SqlConnection(connectionString);
            connection.Open();
            using var transaction = connection.BeginTransaction();
            using var command = new SqlCommand(
                @"DELETE FROM Expenses
WHERE CategoryId IN (SELECT Id FROM Categories WHERE Name = @name);
DELETE FROM Categories WHERE Name = @name;",
                connection,
                transaction);
            command.Parameters.Add("@name", SqlDbType.NVarChar, 200).Value = categoryName;
            command.ExecuteNonQuery();
            transaction.Commit();
        }
    }

    [Fact]
    [Trait("Category", "Integration")]
    public void Repository_rejects_an_expense_with_a_missing_category()
    {
        var repository = new ExpenseRepository(DbIntegrationTests.GetLocalTestConnectionString());
        var exception = Assert.Throws<SqlException>(
            () => repository.AddExpense(1m, DateTime.UtcNow, "invalid-category", int.MaxValue));

        Assert.Equal(547, exception.Number);
    }

    private static int FindCategoryId(DataTable categories, string name)
    {
        foreach (DataRow category in categories.Rows)
        {
            if (string.Equals(category["Name"]?.ToString(), name, StringComparison.Ordinal))
            {
                return Convert.ToInt32(category["Id"]);
            }
        }

        throw new InvalidOperationException("The newly created test category was not returned by the repository.");
    }
}

using ExpenseTracker.Core;
using Microsoft.Data.SqlClient;

namespace ExpenseTracker.Tests;

public class ExpenseRepositoryIntegrationTests
{
    [Fact]
    [Trait("Category", "Integration")]
    public async Task Repository_supports_crud_date_category_filters_and_monthly_totals()
    {
        var connectionString = DbIntegrationTests.GetLocalTestConnectionString();
        var repository = new ExpenseRepository(connectionString);
        var categoryName = "RepositoryTest-" + Guid.NewGuid().ToString("N")[..10];
        var categoryId = 0;

        try
        {
            await repository.AddCategoryAsync(categoryName);
            categoryId = (await repository.GetCategoriesAsync())
                .Single(category => category.Name == categoryName).Id;
            Assert.Empty(await repository.GetExpensesAsync(new ExpenseFilter(CategoryId: categoryId)));

            var month = new DateTime(2024, 2, 1);
            var leapDay = new DateTime(2024, 2, 29, 23, 59, 0);
            Assert.Equal(0m, await repository.GetMonthlyTotalAsync(month, categoryId));
            await repository.AddExpenseAsync(10.25m, leapDay, string.Empty, categoryId);
            await repository.AddExpenseAsync(5.25m, month.AddMonths(1), "next-month", categoryId);

            var monthEnd = month.AddMonths(1).AddDays(-1);
            var februaryExpenses = await repository.GetExpensesAsync(
                new ExpenseFilter(month, monthEnd, categoryId));
            Assert.Single(februaryExpenses);
            Assert.Single(await repository.GetExpensesAsync(
                new ExpenseFilter(leapDay.Date, leapDay.Date, categoryId)));
            Assert.Null(februaryExpenses[0].Note);
            Assert.Equal(10.25m, await repository.GetMonthlyTotalAsync(month, categoryId));
            Assert.Equal(10.25m, await repository.GetMonthlyTotalAsync(month));
            Assert.Equal(5.25m, await repository.GetMonthlyTotalAsync(month.AddMonths(1), categoryId));
            Assert.Equal("next-month",
                (await repository.GetExpensesAsync(new ExpenseFilter(
                    month.AddMonths(1), month.AddMonths(1), categoryId))).Single().Note);

            var expenseId = februaryExpenses[0].Id;
            Assert.True(await repository.UpdateExpenseAsync(
                expenseId, 22.75m, month.AddMonths(1).AddTicks(-1), "updated", categoryId));
            Assert.Equal(22.75m, await repository.GetMonthlyTotalAsync(month, categoryId));

            Assert.True(await repository.DeleteExpenseAsync(expenseId));
            Assert.False(await repository.DeleteExpenseAsync(expenseId));
            Assert.Empty(await repository.GetExpensesAsync(new ExpenseFilter(month, monthEnd, categoryId)));
        }
        finally
        {
            await using var connection = new SqlConnection(connectionString);
            await connection.OpenAsync();
            await using var transaction = (SqlTransaction)await connection.BeginTransactionAsync();
            await using var command = new SqlCommand(
                @"DELETE FROM Expenses
WHERE CategoryId IN (SELECT Id FROM Categories WHERE Name = @name);
DELETE FROM Categories WHERE Name = @name;",
                connection,
                transaction);
            command.Parameters.Add("@name", System.Data.SqlDbType.NVarChar, 200).Value = categoryName;
            await command.ExecuteNonQueryAsync();
            await transaction.CommitAsync();
        }
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task Repository_rejects_an_expense_with_a_missing_category()
    {
        var repository = new ExpenseRepository(DbIntegrationTests.GetLocalTestConnectionString());
        var exception = await Assert.ThrowsAsync<SqlException>(
            () => repository.AddExpenseAsync(1m, DateTime.UtcNow, "invalid-category", int.MaxValue));

        Assert.Equal(547, exception.Number);
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task Repository_round_trips_decimal_boundary_unicode_and_day_end()
    {
        var connectionString = DbIntegrationTests.GetLocalTestConnectionString();
        var repository = new ExpenseRepository(connectionString);
        var categoryName = "BoundaryTest-" + Guid.NewGuid().ToString("N")[..10];
        var categoryId = 0;
        var day = new DateTime(2024, 2, 29);
        const decimal maxAmount = 9999999999999999.99m;

        try
        {
            await repository.AddCategoryAsync(categoryName);
            categoryId = (await repository.GetCategoriesAsync())
                .Single(category => category.Name == categoryName).Id;
            await repository.AddExpenseAsync(
                maxAmount,
                day.AddDays(1).AddTicks(-1),
                "Cà phê ☕",
                categoryId);

            var expenses = await repository.GetExpensesAsync(new ExpenseFilter(day, day, categoryId));

            var expense = Assert.Single(expenses);
            Assert.Equal(maxAmount, expense.Amount);
            Assert.Equal("Cà phê ☕", expense.Note);
            Assert.Equal(day.AddDays(1).AddTicks(-1), expense.Date);
        }
        finally
        {
            await using var connection = new SqlConnection(connectionString);
            await connection.OpenAsync();
            await using var command = new SqlCommand(
                @"DELETE FROM Expenses
WHERE CategoryId IN (SELECT Id FROM Categories WHERE Name = @name);
DELETE FROM Categories WHERE Name = @name;",
                connection);
            command.Parameters.Add("@name", System.Data.SqlDbType.NVarChar, 200).Value = categoryName;
            await command.ExecuteNonQueryAsync();
        }
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task Schema_migration_enforces_case_insensitive_unique_nonblank_category_names()
    {
        var connectionString = DbIntegrationTests.GetLocalTestConnectionString();
        var repository = new ExpenseRepository(connectionString);
        var categoryName = "UniqueCase-" + Guid.NewGuid().ToString("N")[..10];

        try
        {
            await using var connection = new SqlConnection(connectionString);
            await connection.OpenAsync();
            await using (var migrationCommand = new SqlCommand(
                             "SELECT COUNT(*) FROM dbo.SchemaMigrations WHERE MigrationId = @migrationId",
                             connection))
            {
                migrationCommand.Parameters.Add("@migrationId", System.Data.SqlDbType.NVarChar, 100).Value =
                    "2026-09-data-integrity";
                Assert.Equal(1, (int)(await migrationCommand.ExecuteScalarAsync())!);
            }

            await repository.AddCategoryAsync(categoryName);
            var duplicate = await Assert.ThrowsAsync<SqlException>(
                () => repository.AddCategoryAsync(categoryName.ToUpperInvariant()));
            Assert.Contains(duplicate.Number, new[] { 2601, 2627 });

            var blank = await Assert.ThrowsAsync<SqlException>(
                () => repository.AddCategoryAsync("   "));
            Assert.Equal(547, blank.Number);

            var categoryId = (await repository.GetCategoriesAsync())
                .Single(category => category.Name == categoryName).Id;
            var nonPositiveAmount = await Assert.ThrowsAsync<SqlException>(
                () => repository.AddExpenseAsync(-0.01m, DateTime.UtcNow, "invalid-amount", categoryId));
            Assert.Equal(547, nonPositiveAmount.Number);
        }
        finally
        {
            await using var connection = new SqlConnection(connectionString);
            await connection.OpenAsync();
            await using var command = new SqlCommand(
                "DELETE FROM dbo.Categories WHERE Name = @name",
                connection);
            command.Parameters.Add("@name", System.Data.SqlDbType.NVarChar, 200).Value = categoryName;
            await command.ExecuteNonQueryAsync();
        }
    }
}

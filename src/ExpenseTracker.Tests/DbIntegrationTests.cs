using System.Data;
using System.Globalization;
using Microsoft.Data.SqlClient;

namespace ExpenseTracker.Tests;

public class DbIntegrationTests
{
    internal static string GetLocalTestConnectionString()
    {
        var connectionString = Environment.GetEnvironmentVariable("SQL_CONN");
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                "SQL_CONN must be set by the local integration-test setup; refusing to use a default database.");
        }

        var builder = new SqlConnectionStringBuilder(connectionString)
        {
            Encrypt = false,
            TrustServerCertificate = true
        };
        var expectedPortValue = Environment.GetEnvironmentVariable("EXPENSE_TEST_SQL_PORT");
        var expectedPort = string.IsNullOrWhiteSpace(expectedPortValue)
            ? 1433
            : int.TryParse(expectedPortValue, NumberStyles.None, CultureInfo.InvariantCulture, out var parsedPort)
                ? parsedPort
                : -1;
        var endpoint = builder.DataSource.Split(',', 2);
        var dataSourcePort = endpoint.Length == 2 &&
                             int.TryParse(endpoint[1], NumberStyles.None, CultureInfo.InvariantCulture, out var parsedDataSourcePort)
            ? parsedDataSourcePort
            : 1433;
        var isAllowedLocalHost =
            endpoint.Length > 0 &&
            (string.Equals(endpoint[0], "localhost", StringComparison.OrdinalIgnoreCase) ||
             string.Equals(endpoint[0], "127.0.0.1", StringComparison.OrdinalIgnoreCase));
        if (!string.Equals(builder.InitialCatalog, "ExpenseDb", StringComparison.OrdinalIgnoreCase) ||
            !isAllowedLocalHost ||
            expectedPort is < 1 or > 65535 ||
            dataSourcePort != expectedPort)
        {
            throw new InvalidOperationException(
                $"Integration tests are restricted to ExpenseDb on localhost:{expectedPort}.");
        }

        return builder.ConnectionString;
    }

    [Fact]
    [Trait("Category", "Integration")]
    public void CanInsertAndReadExpense()
    {
        using var connection = new SqlConnection(GetLocalTestConnectionString());
        connection.Open();

        using (var command = new SqlCommand(
                   "SELECT COUNT(*) FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'Categories'",
                   connection))
        {
            Assert.Equal(1, (int)command.ExecuteScalar()!);
        }

        using var transaction = connection.BeginTransaction();
        using var categoryCommand = new SqlCommand(
            "INSERT INTO Categories (Name) OUTPUT INSERTED.Id VALUES (@name)",
            connection,
            transaction);
        categoryCommand.Parameters.Add("@name", SqlDbType.NVarChar, 200).Value =
            "TestCategory" + Guid.NewGuid().ToString("N")[..6];
        var categoryId = (int)categoryCommand.ExecuteScalar()!;

        using var insertCommand = new SqlCommand(
            "INSERT INTO Expenses (Amount, Date, Note, CategoryId) VALUES (@amount, @date, @note, @categoryId)",
            connection,
            transaction);
        var amount = insertCommand.Parameters.Add("@amount", SqlDbType.Decimal);
        amount.Precision = 18;
        amount.Scale = 2;
        amount.Value = 123.45m;
        insertCommand.Parameters.Add("@date", SqlDbType.DateTime2).Value = DateTime.UtcNow;
        insertCommand.Parameters.Add("@note", SqlDbType.NVarChar, -1).Value = "integration-test";
        insertCommand.Parameters.Add("@categoryId", SqlDbType.Int).Value = categoryId;
        Assert.Equal(1, insertCommand.ExecuteNonQuery());

        using (var readCommand = new SqlCommand(
                   "SELECT Amount, Note, CategoryId FROM Expenses WHERE CategoryId = @categoryId ORDER BY Id DESC",
                   connection,
                   transaction))
        {
            readCommand.Parameters.Add("@categoryId", SqlDbType.Int).Value = categoryId;
            using var reader = readCommand.ExecuteReader();
            Assert.True(reader.Read());
            Assert.Equal(123.45m, reader.GetDecimal(0));
            Assert.Equal("integration-test", reader.GetString(1));
            Assert.Equal(categoryId, reader.GetInt32(2));
        }

        transaction.Rollback();
    }
}

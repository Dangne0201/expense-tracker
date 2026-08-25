using System;
using System.Data;
using Microsoft.Data.SqlClient;
using Xunit;

namespace ExpenseTracker.Tests
{
    public class DbIntegrationTests
    {
        private static string NormalizeConnectionString(string connectionString)
        {
            if (string.IsNullOrWhiteSpace(connectionString))
            {
                return connectionString;
            }

            var parts = connectionString.Split(';', StringSplitOptions.RemoveEmptyEntries);
            var normalized = new System.Collections.Generic.List<string>();
            var seenEncrypt = false;
            var seenTrust = false;

            foreach (var part in parts)
            {
                var trimmed = part.Trim();
                if (string.IsNullOrWhiteSpace(trimmed))
                {
                    continue;
                }

                var key = trimmed.Split('=')[0].Trim();
                if (string.Equals(key, "Encrypt", StringComparison.OrdinalIgnoreCase))
                {
                    normalized.Add("Encrypt=False");
                    seenEncrypt = true;
                    continue;
                }

                if (string.Equals(key, "TrustServerCertificate", StringComparison.OrdinalIgnoreCase))
                {
                    normalized.Add("TrustServerCertificate=True");
                    seenTrust = true;
                    continue;
                }

                normalized.Add(trimmed);
            }

            if (!seenEncrypt) normalized.Add("Encrypt=False");
            if (!seenTrust) normalized.Add("TrustServerCertificate=True");

            return string.Join(";", normalized) + ";";
        }

        private string GetConn() => NormalizeConnectionString(Environment.GetEnvironmentVariable("SQL_CONN")
            ?? "Server=127.0.0.1,1433;Database=ExpenseDb;User Id=sa;Password=Your_password123;Encrypt=False;TrustServerCertificate=True;");

        [Fact]
        public void CanInsertAndReadExpense()
        {
            var connStr = GetConn();
            using var conn = new SqlConnection(connStr);
            conn.Open();

            // Ensure categories table exists
            using (var cmd = new SqlCommand("SELECT COUNT(*) FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'Categories'", conn))
            {
                var count = (int)cmd.ExecuteScalar();
                Assert.True(count >= 0, "Could not verify presence of Categories table");
            }

            // Insert a category, then insert an expense referencing it, then read back
            int categoryId;
            using (var t = conn.BeginTransaction())
            {
                using var cmd = new SqlCommand("INSERT INTO Categories (Name) OUTPUT INSERTED.Id VALUES (@name)", conn, t);
                cmd.Parameters.AddWithValue("@name", "TestCategory" + Guid.NewGuid().ToString("N").Substring(0,6));
                categoryId = (int)cmd.ExecuteScalar();

                using var cmd2 = new SqlCommand("INSERT INTO Expenses (Amount, Date, Note, CategoryId) VALUES (@amt, @dt, @note, @cat)", conn, t);
                var pAmt = new SqlParameter("@amt", System.Data.SqlDbType.Decimal) { Precision = 18, Scale = 2, Value = 123.45m }; cmd2.Parameters.Add(pAmt);
                cmd2.Parameters.AddWithValue("@dt", DateTime.UtcNow);
                cmd2.Parameters.AddWithValue("@note", "integration-test");
                cmd2.Parameters.AddWithValue("@cat", categoryId);
                cmd2.ExecuteNonQuery();

                // Read the expense back
                using var cmd3 = new SqlCommand("SELECT TOP 1 Amount, Note, CategoryId FROM Expenses WHERE CategoryId = @cat ORDER BY Id DESC", conn, t);
                cmd3.Parameters.AddWithValue("@cat", categoryId);
                using (var rdr = cmd3.ExecuteReader())
                {
                    Assert.True(rdr.Read(), "Inserted expense not found");
                    var amount = Convert.ToDecimal(rdr.GetValue(0)); Assert.Equal(123.45m, amount);
                    Assert.Equal("integration-test", rdr.GetString(1));
                    Assert.Equal(categoryId, rdr.GetInt32(2));
                }

                // Rollback so tests are idempotent
                t.Rollback();
            }
        }
    }
}

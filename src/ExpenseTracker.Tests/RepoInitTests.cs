using System.IO;
using Xunit;

namespace ExpenseTracker.Tests
{
    // Checks that the repository's canonical SQL initialization script defines the expected tables.
    public class RepoInitTests
    {
        [Fact]
        // Finds init.sql from the current directory and verifies its core table declarations.
        public void InitSqlContainsCategoriesTable()
        {
            // Walk upward so the test works from both the repository and test output directories.
            var dir = Directory.GetCurrentDirectory();
            string? found = null;
            while (!string.IsNullOrEmpty(dir))
            {
                var candidate = Path.Combine(dir, "data", "init.sql");
                if (File.Exists(candidate)) { found = candidate; break; }
                var parent = Directory.GetParent(dir);
                dir = parent?.FullName;
            }
            Assert.False(string.IsNullOrEmpty(found), "init.sql not found in repository (searched upward from current dir)");
            var text = File.ReadAllText(found!);
            Assert.Contains("CREATE TABLE dbo.Categories", text);
            Assert.Contains("CREATE TABLE dbo.Expenses", text);
        }
    }
}

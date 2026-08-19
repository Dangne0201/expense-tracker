using System.IO;
using Xunit;

namespace ExpenseTracker.Tests
{
    public class RepoInitTests
    {
        [Fact]
        public void InitSqlContainsCategoriesTable()
        {
            // Robustly find data/init.sql by walking upward from current directory
            var dir = Directory.GetCurrentDirectory();
            string found = null;
            while (!string.IsNullOrEmpty(dir))
            {
                var candidate = Path.Combine(dir, "data", "init.sql");
                if (File.Exists(candidate)) { found = candidate; break; }
                var parent = Directory.GetParent(dir);
                dir = parent?.FullName;
            }
            Assert.False(string.IsNullOrEmpty(found), "init.sql not found in repository (searched upward from current dir)");
            var text = File.ReadAllText(found);
            Assert.Contains("CREATE TABLE dbo.Categories", text);
            Assert.Contains("CREATE TABLE dbo.Expenses", text);
        }
    }
}

using System.IO;
using Xunit;

namespace ExpenseTracker.Tests
{
    public class RepoInitTests
    {
        [Fact]
        public void InitSqlContainsCategoriesTable()
        {
            var path = Path.Combine("..", "..", "..", "..", "data", "init.sql");
            Assert.True(File.Exists(path), $"init.sql not found at {path}");
            var text = File.ReadAllText(path);
            Assert.Contains("CREATE TABLE dbo.Categories", text);
            Assert.Contains("CREATE TABLE dbo.Expenses", text);
        }
    }
}

using System.Collections.Generic;

namespace ExpenseTracker.Api.Models
{
    public class Category
    {
        public int Id { get; set; }
        public string Name { get; set; } = null!;

        // Navigation
        public ICollection<Expense> Expenses { get; set; } = new List<Expense>();
    }
}

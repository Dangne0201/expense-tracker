using System;

namespace ExpenseTracker.Api.Models
{
    public class Expense
    {
        public int Id { get; set; }
        public decimal Amount { get; set; }
        public DateTimeOffset Date { get; set; }
        public string? Note { get; set; }

        // Foreign key
        public int CategoryId { get; set; }
        public Category? Category { get; set; }
    }
}

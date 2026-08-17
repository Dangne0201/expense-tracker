using System;

namespace ExpenseTracker.Api.Dtos
{
    public class ExpenseDto
    {
        public int Id { get; set; }
        public decimal Amount { get; set; }
        public DateTimeOffset Date { get; set; }
        public string? Note { get; set; }
        public int CategoryId { get; set; }
        public string? CategoryName { get; set; }
    }
}

using System;
using System.ComponentModel.DataAnnotations;

namespace ExpenseTracker.Api.Dtos
{
    public class CreateExpenseDto
    {
        [Required]
        [Range(0.01, double.MaxValue)]
        public decimal Amount { get; set; }

        [Required]
        public DateTimeOffset Date { get; set; }

        [MaxLength(1000)]
        public string? Note { get; set; }

        [Required]
        public int CategoryId { get; set; }
    }
}

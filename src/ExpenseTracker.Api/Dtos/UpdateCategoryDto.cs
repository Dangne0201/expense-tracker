using System.ComponentModel.DataAnnotations;

namespace ExpenseTracker.Api.Dtos
{
    public class UpdateCategoryDto
    {
        [Required]
        [MaxLength(100)]
        public string Name { get; set; } = null!;
    }
}

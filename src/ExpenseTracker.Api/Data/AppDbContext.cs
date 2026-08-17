using Microsoft.EntityFrameworkCore;
using ExpenseTracker.Api.Models;

namespace ExpenseTracker.Api.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<Expense> Expenses { get; set; } = null!;
        public DbSet<Category> Categories { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Category>(b =>
            {
                b.HasKey(c => c.Id);
                b.Property(c => c.Name).IsRequired().HasMaxLength(100);
            });

            modelBuilder.Entity<Expense>(b =>
            {
                b.HasKey(e => e.Id);
                b.Property(e => e.Amount).HasColumnType("decimal(18,2)").IsRequired();
                b.Property(e => e.Date).IsRequired();
                b.Property(e => e.Note).HasMaxLength(1000);

                b.HasOne(e => e.Category)
                 .WithMany(c => c.Expenses)
                 .HasForeignKey(e => e.CategoryId)
                 .OnDelete(DeleteBehavior.Restrict);
            });
        }
    }
}

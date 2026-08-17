using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ExpenseTracker.Api.Data;
using ExpenseTracker.Api.Dtos;
using ExpenseTracker.Api.Models;

namespace ExpenseTracker.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ExpensesController : ControllerBase
    {
        private readonly AppDbContext _db;

        public ExpensesController(AppDbContext db)
        {
            _db = db;
        }

        // GET: api/expenses
        // Supports optional filters: categoryId, from (ISO date), to (ISO date), page, pageSize
        [HttpGet]
        public async Task<ActionResult<IEnumerable<ExpenseDto>>> GetAll([FromQuery] int? categoryId, [FromQuery] DateTimeOffset? from, [FromQuery] DateTimeOffset? to, [FromQuery] int page = 1, [FromQuery] int pageSize = 50)
        {
            if (page <= 0) page = 1;
            if (pageSize <= 0 || pageSize > 100) pageSize = 50;

            var query = _db.Expenses.AsNoTracking().AsQueryable();

            if (categoryId.HasValue) query = query.Where(e => e.CategoryId == categoryId.Value);
            if (from.HasValue) query = query.Where(e => e.Date >= from.Value);
            if (to.HasValue) query = query.Where(e => e.Date <= to.Value);

            var items = await query
                        .OrderByDescending(e => e.Date)
                        .Skip((page - 1) * pageSize)
                        .Take(pageSize)
                        .Select(e => new ExpenseDto {
                            Id = e.Id,
                            Amount = e.Amount,
                            Date = e.Date,
                            Note = e.Note,
                            CategoryId = e.CategoryId,
                            CategoryName = e.Category != null ? e.Category.Name : null
                        })
                        .ToListAsync();

            return Ok(items);
        }

        // GET: api/expenses/5
        [HttpGet("{id:int}")]
        public async Task<ActionResult<ExpenseDto>> Get(int id)
        {
            var e = await _db.Expenses
                .AsNoTracking()
                .Where(x => x.Id == id)
                .Select(x => new ExpenseDto {
                    Id = x.Id,
                    Amount = x.Amount,
                    Date = x.Date,
                    Note = x.Note,
                    CategoryId = x.CategoryId,
                    CategoryName = x.Category != null ? x.Category.Name : null
                })
                .FirstOrDefaultAsync();

            if (e == null) return NotFound();
            return Ok(e);
        }

        // POST: api/expenses
        [HttpPost]
        public async Task<ActionResult<ExpenseDto>> Create([FromBody] CreateExpenseDto dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            // Ensure category exists
            var category = await _db.Categories.FindAsync(dto.CategoryId);
            if (category == null) return BadRequest(new { message = "Category does not exist." });

            var entity = new Expense {
                Amount = dto.Amount,
                Date = dto.Date,
                Note = dto.Note,
                CategoryId = dto.CategoryId
            };

            _db.Expenses.Add(entity);
            await _db.SaveChangesAsync();

            var result = new ExpenseDto {
                Id = entity.Id,
                Amount = entity.Amount,
                Date = entity.Date,
                Note = entity.Note,
                CategoryId = entity.CategoryId,
                CategoryName = category.Name
            };

            return CreatedAtAction(nameof(Get), new { id = result.Id }, result);
        }

        // PUT: api/expenses/5
        [HttpPut("{id:int}")]
        public async Task<IActionResult> Update(int id, [FromBody] UpdateExpenseDto dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var entity = await _db.Expenses.FindAsync(id);
            if (entity == null) return NotFound();

            // ensure category exists
            var category = await _db.Categories.FindAsync(dto.CategoryId);
            if (category == null) return BadRequest(new { message = "Category does not exist." });

            entity.Amount = dto.Amount;
            entity.Date = dto.Date;
            entity.Note = dto.Note;
            entity.CategoryId = dto.CategoryId;

            await _db.SaveChangesAsync();
            return NoContent();
        }

        // DELETE: api/expenses/5
        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id)
        {
            var entity = await _db.Expenses.FindAsync(id);
            if (entity == null) return NotFound();

            _db.Expenses.Remove(entity);
            await _db.SaveChangesAsync();
            return NoContent();
        }
    }
}

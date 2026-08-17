using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ExpenseTracker.Api.Data;
using ExpenseTracker.Api.Dtos;
using ExpenseTracker.Api.Models;

namespace ExpenseTracker.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class CategoriesController : ControllerBase
    {
        private readonly AppDbContext _db;

        public CategoriesController(AppDbContext db)
        {
            _db = db;
        }

        // GET: api/categories
        [HttpGet]
        public async Task<ActionResult<IEnumerable<CategoryDto>>> GetAll()
        {
            var list = await _db.Categories
                        .AsNoTracking()
                        .Select(c => new CategoryDto { Id = c.Id, Name = c.Name })
                        .ToListAsync();
            return Ok(list);
        }

        // GET: api/categories/5
        [HttpGet("{id:int}")]
        public async Task<ActionResult<CategoryDto>> Get(int id)
        {
            var c = await _db.Categories
                        .AsNoTracking()
                        .Where(x => x.Id == id)
                        .Select(x => new CategoryDto { Id = x.Id, Name = x.Name })
                        .FirstOrDefaultAsync();
            if (c == null) return NotFound();
            return Ok(c);
        }

        // POST: api/categories
        [HttpPost]
        public async Task<ActionResult<CategoryDto>> Create([FromBody] CreateCategoryDto dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var entity = new Category { Name = dto.Name };
            _db.Categories.Add(entity);
            await _db.SaveChangesAsync();

            var result = new CategoryDto { Id = entity.Id, Name = entity.Name };
            return CreatedAtAction(nameof(Get), new { id = result.Id }, result);
        }

        // PUT: api/categories/5
        [HttpPut("{id:int}")]
        public async Task<IActionResult> Update(int id, [FromBody] UpdateCategoryDto dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);
            var entity = await _db.Categories.FindAsync(id);
            if (entity == null) return NotFound();

            entity.Name = dto.Name;
            await _db.SaveChangesAsync();

            return NoContent();
        }

        // DELETE: api/categories/5
        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id)
        {
            var entity = await _db.Categories.Include(c => c.Expenses).FirstOrDefaultAsync(c => c.Id == id);
            if (entity == null) return NotFound();

            if (entity.Expenses != null && entity.Expenses.Any())
            {
                // Prevent deletion when expenses exist; caller should reassign or delete expenses first
                return BadRequest(new { message = "Cannot delete category that has expenses. Reassign or remove expenses first." });
            }

            _db.Categories.Remove(entity);
            await _db.SaveChangesAsync();
            return NoContent();
        }
    }
}

namespace Wihngo.Controllers
{
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.EntityFrameworkCore;
    using Wihngo.Data;
    using Wihngo.Models;

    [Route("api/[controller]")]
    [ApiController]
    public class SupportTransactionsController : ControllerBase
    {
        private readonly AppDbContext _db;

        public SupportTransactionsController(AppDbContext db)
        {
            _db = db;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<SupportTransaction>>> Get()
        {
            return await _db.SupportTransactions
                .Include(t => t.Supporter)
                .Include(t => t.Bird)
                .ToListAsync();
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<SupportTransaction>> Get(Guid id)
        {
            var tx = await _db.SupportTransactions
                .Include(t => t.Supporter)
                .Include(t => t.Bird)
                .FirstOrDefaultAsync(t => t.TransactionId == id);

            if (tx == null) return NotFound();
            return tx;
        }

        [HttpPost]
        public async Task<ActionResult<SupportTransaction>> Post([FromBody] SupportTransaction tx)
        {
            tx.TransactionId = Guid.NewGuid();
            tx.CreatedAt = DateTime.UtcNow;
            _db.SupportTransactions.Add(tx);
            await _db.SaveChangesAsync();
            return CreatedAtAction(nameof(Get), new { id = tx.TransactionId }, tx);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(Guid id)
        {
            var tx = await _db.SupportTransactions.FindAsync(id);
            if (tx == null) return NotFound();

            _db.SupportTransactions.Remove(tx);
            await _db.SaveChangesAsync();
            return NoContent();
        }
    }
}

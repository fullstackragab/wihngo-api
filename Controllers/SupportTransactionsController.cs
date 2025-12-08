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
            if (!ModelState.IsValid) return BadRequest(ModelState);

            // Validate supporter exists
            var supporter = await _db.Users.FindAsync(tx.SupporterId);
            if (supporter == null) return BadRequest("Supporter not found");

            // Validate bird exists
            var bird = await _db.Birds.FindAsync(tx.BirdId);
            if (bird == null) return BadRequest("Bird not found");

            tx.TransactionId = Guid.NewGuid();
            tx.CreatedAt = DateTime.UtcNow;

            // Use a transaction and perform atomic DB-side increment to avoid race conditions
            await using (var dbTxn = await _db.Database.BeginTransactionAsync())
            {
                try
                {
                    _db.SupportTransactions.Add(tx);
                    await _db.SaveChangesAsync();

                    // Atomic increment on DB side
                    await _db.Database.ExecuteSqlInterpolatedAsync($"UPDATE birds SET supported_count = supported_count + 1 WHERE bird_id = {tx.BirdId}");

                    await dbTxn.CommitAsync();
                }
                catch
                {
                    await dbTxn.RollbackAsync();
                    throw;
                }
            }

            var created = await _db.SupportTransactions
                .Include(t => t.Supporter)
                .Include(t => t.Bird)
                .FirstOrDefaultAsync(t => t.TransactionId == tx.TransactionId);

            return CreatedAtAction(nameof(Get), new { id = tx.TransactionId }, created);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(Guid id)
        {
            var tx = await _db.SupportTransactions.FindAsync(id);
            if (tx == null) return NotFound();

            // Use transaction and atomic DB-side decrement to avoid race conditions
            await using (var dbTxn = await _db.Database.BeginTransactionAsync())
            {
                try
                {
                    // Atomic decrement but do not go below zero
                    await _db.Database.ExecuteSqlInterpolatedAsync($"UPDATE birds SET supported_count = GREATEST(supported_count - 1, 0) WHERE bird_id = {tx.BirdId}");

                    _db.SupportTransactions.Remove(tx);
                    await _db.SaveChangesAsync();

                    await dbTxn.CommitAsync();
                }
                catch
                {
                    await dbTxn.RollbackAsync();
                    throw;
                }
            }

            return NoContent();
        }
    }
}

using System;
using System.Threading;
using System.Threading.Tasks;
using Ledger.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Ledger.Infrastructure.Repositories
{
    /// <summary>
    /// Repositório para operações de persistência de lançamentos.
    /// </summary>
    public class LedgerRepository
    {
        private readonly LedgerDbContext _context;

        public LedgerRepository(LedgerDbContext context)
        {
            _context = context;
        }

        public async Task<LedgerEntry> AddAsync(LedgerEntry entry, CancellationToken cancellationToken)
        {
            _context.Entries.Add(entry);
            await _context.SaveChangesAsync(cancellationToken);
            return entry;
        }

        public async Task<LedgerEntry?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
        {
            return await _context.Entries.AsNoTracking().FirstOrDefaultAsync(e => e.Id == id, cancellationToken);
        }
    }
}
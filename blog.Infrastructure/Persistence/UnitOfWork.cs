using blog.Domain.Common.Interfaces;
using blog.Domain.Exceptions;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace blog.Infrastructure.Persistence
{
    public class UnitOfWork(AppDbContext context) : IUnitOfWork
    {
        public async Task<int> SaveChangesAsync(CancellationToken ct = default)
        {
            try
            {
                return await context.SaveChangesAsync(ct);
            }
            catch (DbUpdateException ex) when (ex.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation })
            {
                var resource = ex.Entries.FirstOrDefault()?.Metadata.ClrType.Name ?? "Resource";
                throw new AlreadyExistsException(resource);
            }
        }
    }
}
using GK.GlobalKineticAssessment.Domain.Entities;
using GK.GlobalKineticAssessment.Domain.Interfaces;
using GK.GlobalKineticAssessment.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace GK.GlobalKineticAssessment.Infrastructure.Repositories;

public sealed class CustomerRepository : RepositoryBase<Customer, Guid>, ICustomerRepository
{
    public CustomerRepository(AppDbContext context) : base(context) { }

    public async Task<(IEnumerable<Customer> Items, int TotalCount)> GetPagedAsync(
        string? firstNameFilter, int pageNumber, int pageSize, CancellationToken ct = default)
    {
        var all = await _dbSet.AsNoTracking().ToListAsync(ct);

        if (!string.IsNullOrWhiteSpace(firstNameFilter))
            all = all.Where(c => c.FirstName.Contains(firstNameFilter, StringComparison.OrdinalIgnoreCase)).ToList();

        return (all.Skip((pageNumber - 1) * pageSize).Take(pageSize).ToList(), all.Count);
    }

    public async Task<bool> EmailExistsAsync(string email, Guid? excludeId = null, CancellationToken ct = default)
    {
        var all = await _dbSet.AsNoTracking().ToListAsync(ct);
        return all.Any(c =>
            c.Email.Equals(email, StringComparison.OrdinalIgnoreCase) &&
            (excludeId is null || c.Id != excludeId.Value));
    }
}

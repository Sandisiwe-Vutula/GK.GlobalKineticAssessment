using GK.GlobalKineticAssessment.Domain.Entities;

namespace GK.GlobalKineticAssessment.Domain.Interfaces;

public interface ICustomerRepository : IRepository<Customer, Guid>
{
    Task<(IEnumerable<Customer> Items, int TotalCount)> GetPagedAsync(
        string? firstNameFilter, int pageNumber, int pageSize, CancellationToken ct = default);
    Task<bool> EmailExistsAsync(string email, Guid? excludeId = null, CancellationToken ct = default);
}

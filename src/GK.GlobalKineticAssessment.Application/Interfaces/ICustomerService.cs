using GK.GlobalKineticAssessment.Application.DTOs;

namespace GK.GlobalKineticAssessment.Application.Interfaces;

public interface ICustomerService
{
    Task<CustomerResponse> CreateAsync(CreateCustomerRequest request, CancellationToken ct = default);
    Task<CustomerResponse> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<PagedResponse<CustomerResponse>> GetAllAsync(GetCustomersQuery query, CancellationToken ct = default);
    Task<CustomerResponse> UpdateAsync(Guid id, UpdateCustomerRequest request, CancellationToken ct = default);
    Task DeleteAsync(Guid id, CancellationToken ct = default);
}

using FluentValidation;
using GK.GlobalKineticAssessment.Application.DTOs;
using GK.GlobalKineticAssessment.Application.Interfaces;
using GK.GlobalKineticAssessment.Domain.Entities;
using GK.GlobalKineticAssessment.Domain.Exceptions;
using GK.GlobalKineticAssessment.Domain.Interfaces;
using Microsoft.Extensions.Logging;
using ValidationException = GK.GlobalKineticAssessment.Domain.Exceptions.ValidationException;

namespace GK.GlobalKineticAssessment.Application.Services;

public sealed class CustomerService : ICustomerService
{
    private readonly ICustomerRepository _customerRepository;
    private readonly IValidator<CreateCustomerRequest> _createVal;
    private readonly IValidator<UpdateCustomerRequest> _updateVal;
    private readonly IValidator<GetCustomersQuery> _queryVal;
    private readonly ILogger<CustomerService> _logger;

    public CustomerService(
        ICustomerRepository customerRepository,
        IValidator<CreateCustomerRequest> createVal,
        IValidator<UpdateCustomerRequest> updateVal,
        IValidator<GetCustomersQuery> queryVal,
        ILogger<CustomerService> logger)
    {
        _customerRepository = customerRepository; _createVal = createVal;
        _updateVal = updateVal; _queryVal = queryVal; _logger = logger;
    }

    public async Task<CustomerResponse> CreateAsync(CreateCustomerRequest request, CancellationToken ct = default)
    {
        await ValidateAsync(_createVal, request, ct);

        if (await _customerRepository.EmailExistsAsync(request.Email, null, ct))
            throw new DuplicateEmailException(request.Email);

        var customer = new Customer
        {
            Id        = Guid.NewGuid(),
            FirstName = request.FirstName.Trim(),
            LastName  = request.LastName.Trim(),
            Email     = request.Email.Trim().ToLowerInvariant(),
            Age       = request.Age,
            CreatedAt = DateTime.UtcNow
        };

        var result = await _customerRepository.AddAsync(customer, ct);
        _logger.LogInformation("Customer {Id} created", result.Id);
        return ToResponse(result);
    }

    public async Task<CustomerResponse> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var c = await _customerRepository.GetByIdAsync(id, ct) ?? throw new NotFoundException(nameof(Customer), id);
        return ToResponse(c);
    }

    public async Task<PagedResponse<CustomerResponse>> GetAllAsync(GetCustomersQuery query, CancellationToken ct = default)
    {
        await ValidateAsync(_queryVal, query, ct);
        var (items, total) = await _customerRepository.GetPagedAsync(query.FirstName, query.PageNumber, query.PageSize, ct);
        var pages = (int)Math.Ceiling(total / (double)query.PageSize);
        return new PagedResponse<CustomerResponse>(items.Select(ToResponse), query.PageNumber, query.PageSize, total, pages);
    }

    public async Task<CustomerResponse> UpdateAsync(Guid id, UpdateCustomerRequest request, CancellationToken ct = default)
    {
        await ValidateAsync(_updateVal, request, ct);

        var customer = await _customerRepository.GetByIdAsync(id, ct)
            ?? throw new NotFoundException(nameof(Customer), id);

        if (await _customerRepository.EmailExistsAsync(request.Email, id, ct))
            throw new DuplicateEmailException(request.Email);

        customer.FirstName = request.FirstName.Trim();
        customer.LastName  = request.LastName.Trim();
        customer.Email     = request.Email.Trim().ToLowerInvariant();
        customer.Age       = request.Age;
        customer.UpdatedAt = DateTime.UtcNow;

        var result = await _customerRepository.UpdateAsync(customer, ct);
        _logger.LogInformation("Customer {Id} updated", result.Id);
        return ToResponse(result);
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct = default)
    {
        if (!await _customerRepository.DeleteAsync(id, ct))
            throw new NotFoundException(nameof(Customer), id);
        _logger.LogInformation("Customer {Id} deleted", id);
    }

    private static CustomerResponse ToResponse(Customer c) =>
        new(c.Id, c.FirstName, c.LastName, c.Email, c.Age, c.CreatedAt, c.UpdatedAt);

    private static async Task ValidateAsync<T>(IValidator<T> v, T obj, CancellationToken ct)
    {
        var result = await v.ValidateAsync(obj, ct);
        if (!result.IsValid)
        {
            var errors = result.Errors
                .GroupBy(e => e.PropertyName)
                .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray());
            throw new ValidationException(errors);
        }
    }
}

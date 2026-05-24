namespace GK.GlobalKineticAssessment.Application.DTOs;

public record CreateCustomerRequest(string FirstName, string LastName, string Email, int Age);

public record UpdateCustomerRequest(string FirstName, string LastName, string Email, int Age);

public record GetCustomersQuery(string? FirstName = null, int PageNumber = 1, int PageSize = 10);

public record CustomerResponse(Guid Id, string FirstName, string LastName, string Email, int Age, DateTime CreatedAt, DateTime? UpdatedAt);

public record PagedResponse<T>(IEnumerable<T> Items, int PageNumber, int PageSize, int TotalCount, int TotalPages);

public record ErrorResponse(string Message, IReadOnlyDictionary<string, string[]>? Errors = null);

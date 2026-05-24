using Asp.Versioning;
using GK.GlobalKineticAssessment.Application.DTOs;
using GK.GlobalKineticAssessment.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace GK.GlobalKineticAssessment.API.Controllers.V1;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/customers")]
[Produces("application/json")]
public sealed class CustomersController : ControllerBase
{
    private readonly ICustomerService _customerService;
    public CustomersController(ICustomerService customerService) => _customerService = customerService;

    [HttpPost]
    [SwaggerOperation(Summary = "Create a Customer", Tags = ["Customers"])]
    [SwaggerResponse(201, "Customer created.", typeof(CustomerResponse))]
    [SwaggerResponse(409, "Email already exists.", typeof(ErrorResponse))]
    [SwaggerResponse(422, "Validation errors.", typeof(ErrorResponse))]
    public async Task<IActionResult> Create([FromBody] CreateCustomerRequest request, CancellationToken ct)
    {
        var result = await _customerService.CreateAsync(request, ct);
        return CreatedAtAction(nameof(GetById), new { id = result.Id, version = "1" }, result);
    }

    [HttpGet("{id:guid}")]
    [SwaggerOperation(Summary = "Get a Customer by Id", Tags = ["Customers"])]
    [SwaggerResponse(200, "Customer found.", typeof(CustomerResponse))]
    [SwaggerResponse(404, "Customer not found.", typeof(ErrorResponse))]
    public async Task<IActionResult> GetById([FromRoute] Guid id, CancellationToken ct) =>
        Ok(await _customerService.GetByIdAsync(id, ct));

    [HttpGet]
    [SwaggerOperation(Summary = "Get all Customers (paged)", Tags = ["Customers"])]
    [SwaggerResponse(200, "Paged results.", typeof(PagedResponse<CustomerResponse>))]
    [SwaggerResponse(422, "Validation errors.", typeof(ErrorResponse))]
    public async Task<IActionResult> GetAll(
        [FromQuery] string? firstName,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize   = 10,
        CancellationToken ct = default)
    {
        var query = new GetCustomersQuery(firstName, pageNumber, pageSize);
        return Ok(await _customerService.GetAllAsync(query, ct));
    }

    [HttpPut("{id:guid}")]
    [SwaggerOperation(Summary = "Update a Customer", Tags = ["Customers"])]
    [SwaggerResponse(200, "Customer updated.", typeof(CustomerResponse))]
    [SwaggerResponse(404, "Customer not found.", typeof(ErrorResponse))]
    [SwaggerResponse(409, "Email already in use.", typeof(ErrorResponse))]
    [SwaggerResponse(422, "Validation errors.", typeof(ErrorResponse))]
    public async Task<IActionResult> Update(
        [FromRoute] Guid id, [FromBody] UpdateCustomerRequest request, CancellationToken ct) =>
        Ok(await _customerService.UpdateAsync(id, request, ct));

    [HttpDelete("{id:guid}")]
    [SwaggerOperation(Summary = "Delete a Customer", Tags = ["Customers"])]
    [SwaggerResponse(204, "Customer deleted.")]
    [SwaggerResponse(404, "Customer not found.", typeof(ErrorResponse))]
    public async Task<IActionResult> Delete([FromRoute] Guid id, CancellationToken ct)
    {
        await _customerService.DeleteAsync(id, ct);
        return NoContent();
    }
}

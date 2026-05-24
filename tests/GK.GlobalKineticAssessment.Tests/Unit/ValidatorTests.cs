using Xunit;
using FluentValidation.TestHelper;
using GK.GlobalKineticAssessment.Application.DTOs;
using GK.GlobalKineticAssessment.Application.Validators;
using GK.GlobalKineticAssessment.Tests.Helpers;

namespace GK.GlobalKineticAssessment.Tests.Unit;

[Trait("Category", "Unit")]
public sealed class ValidatorTests
{
    private readonly CreateCustomerRequestValidator _createVal = new();
    private readonly GetCustomersQueryValidator _queryVal = new();

    [Fact]
    public async Task Create_Valid_PassesValidation()
    {
        var bulder = CustomerBuilder.Unique();
        var result = await _createVal.TestValidateAsync(
            new CreateCustomerRequest(bulder.FirstName, bulder.LastName, bulder.Email, bulder.Age));
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public async Task Create_InvalidEmail_Fails()
    {
        var bulder = new CustomerBuilder().WithEmail("not-email").Build();
        var result = await _createVal.TestValidateAsync(
            new CreateCustomerRequest(bulder.FirstName, bulder.LastName, bulder.Email, bulder.Age));
        result.ShouldHaveValidationErrorFor(x => x.Email);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(151)]
    public async Task Create_InvalidAge_Fails(int age)
    {
        var bulder = new CustomerBuilder().WithAge(age).WithEmail($"u_{Guid.NewGuid():N}@test.com").Build();
        var result = await _createVal.TestValidateAsync(
            new CreateCustomerRequest(bulder.FirstName, bulder.LastName, bulder.Email, bulder.Age));
        result.ShouldHaveValidationErrorFor(x => x.Age);
    }

    [Fact]
    public async Task Query_PageSizeZero_Fails()
    {
        var result = await _queryVal.TestValidateAsync(new GetCustomersQuery(null, 1, 0));
        result.ShouldHaveValidationErrorFor(x => x.PageSize);
    }

    [Fact]
    public async Task Query_PageNumberZero_Fails()
    {
        var result = await _queryVal.TestValidateAsync(new GetCustomersQuery(null, 0, 10));
        result.ShouldHaveValidationErrorFor(x => x.PageNumber);
    }
}
using FluentAssertions;
using GK.GlobalKineticAssessment.Application.DTOs;
using GK.GlobalKineticAssessment.Application.Interfaces;
using GK.GlobalKineticAssessment.Application.Services;
using GK.GlobalKineticAssessment.Application.Validators;
using GK.GlobalKineticAssessment.Domain.Entities;
using GK.GlobalKineticAssessment.Domain.Exceptions;
using GK.GlobalKineticAssessment.Domain.Interfaces;
using GK.GlobalKineticAssessment.Tests.Helpers;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace GK.GlobalKineticAssessment.Tests.Unit;

[Trait("Category", "Unit")]
public sealed class CustomerServiceTests
{
    private readonly Mock<ICustomerRepository> _repositoryMock = new();
    private readonly CustomerService _customerService;

    public CustomerServiceTests() =>
        _customerService = new CustomerService(
            _repositoryMock.Object,
            new CreateCustomerRequestValidator(),
            new UpdateCustomerRequestValidator(),
            new GetCustomersQueryValidator(),
            NullLogger<CustomerService>.Instance);

    [Fact]
    public async Task Create_ValidRequest_ReturnsResponse()
    {
        var sandisiwe = CustomerBuilder.Unique();
        var req = new CreateCustomerRequest(sandisiwe.FirstName, sandisiwe.LastName, sandisiwe.Email, sandisiwe.Age);
        _repositoryMock.Setup(r => r.EmailExistsAsync(sandisiwe.Email, (Guid?)null, default)).ReturnsAsync(false);
        _repositoryMock.Setup(r => r.AddAsync(It.IsAny<Customer>(), default)).ReturnsAsync(sandisiwe);

        var result = await _customerService.CreateAsync(req);

        result.Should().NotBeNull();
        result.Email.Should().Be(sandisiwe.Email);
    }

    [Fact]
    public async Task Create_DuplicateEmail_ThrowsDuplicateEmailException()
    {
        var sandisiwe = CustomerBuilder.Unique();
        var nosiphiwo = new CustomerBuilder()
            .WithFirstName("Nosiphiwo")
            .WithLastName("Buso")
            .WithAge(30)
            .WithEmail(sandisiwe.Email)
            .Build();

        var req = new CreateCustomerRequest(nosiphiwo.FirstName, nosiphiwo.LastName, nosiphiwo.Email, nosiphiwo.Age);
        _repositoryMock.Setup(r => r.EmailExistsAsync(nosiphiwo.Email, (Guid?)null, default)).ReturnsAsync(true);

        await Assert.ThrowsAsync<DuplicateEmailException>(() => _customerService.CreateAsync(req));
    }

    [Theory]
    [InlineData("", "Vutula", "sandisiwevutula28@gmail.com", 35)]
    [InlineData("Sandisiwe", "", "sandisiwevutula28@gmail.com", 35)]
    [InlineData("Sandisiwe", "Vutula", "not-email", 35)]
    [InlineData("Sandisiwe", "Vutula", "sandisiwevutula28@gmail.com", -1)]
    [InlineData("Sandisiwe", "Vutula", "sandisiwevutula28@gmail.com", 200)]
    public async Task Create_InvalidInput_ThrowsValidationException(string f, string l, string e, int a)
    {
        var req = new CreateCustomerRequest(f, l, e, a);
        await Assert.ThrowsAsync<GK.GlobalKineticAssessment.Domain.Exceptions.ValidationException>(
            () => _customerService.CreateAsync(req));
    }

    [Fact]
    public async Task GetById_Existing_ReturnsResponse()
    {
        var bulder = CustomerBuilder.Default();
        _repositoryMock.Setup(r => r.GetByIdAsync(bulder.Id, default)).ReturnsAsync(bulder);

        var result = await _customerService.GetByIdAsync(bulder.Id);
        result.Id.Should().Be(bulder.Id);
    }

    [Fact]
    public async Task GetById_Missing_ThrowsNotFoundException()
    {
        var id = Guid.NewGuid();
        _repositoryMock.Setup(r => r.GetByIdAsync(id, default)).ReturnsAsync((Customer?)null);
        await Assert.ThrowsAsync<NotFoundException>(() => _customerService.GetByIdAsync(id));
    }

    [Fact]
    public async Task GetAll_ReturnsPagedResponse()
    {
        var items = new List<Customer> { CustomerBuilder.Default() };
        _repositoryMock.Setup(r => r.GetPagedAsync(null, 1, 10, default)).ReturnsAsync((items, 1));

        var result = await _customerService.GetAllAsync(new GetCustomersQuery());
        result.TotalCount.Should().Be(1);
        result.Items.Should().HaveCount(1);
    }

    [Fact]
    public async Task Update_Valid_ReturnsUpdated()
    {
        var sandisiwe = CustomerBuilder.Default();
        var global = new CustomerBuilder()
            .WithFirstName("Global")
            .WithLastName("Kinetic")
            .WithAge(25)
            .WithEmail($"u_{Guid.NewGuid():N}@test.com")
            .Build();

        var req = new UpdateCustomerRequest(global.FirstName, global.LastName, global.Email, global.Age);
        _repositoryMock.Setup(r => r.GetByIdAsync(sandisiwe.Id, default)).ReturnsAsync(sandisiwe);
        _repositoryMock.Setup(r => r.EmailExistsAsync(global.Email, (Guid?)sandisiwe.Id, default)).ReturnsAsync(false);
        _repositoryMock.Setup(r => r.UpdateAsync(It.IsAny<Customer>(), default)).ReturnsAsync((Customer x, CancellationToken _) => x);

        var result = await _customerService.UpdateAsync(sandisiwe.Id, req);
        result.FirstName.Should().Be("Global");
        result.UpdatedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task Update_Missing_ThrowsNotFoundException()
    {
        var kubo = new CustomerBuilder()
            .WithFirstName("Kubo")
            .WithLastName("Vutula")
            .WithAge(20)
            .WithEmail($"u_{Guid.NewGuid():N}@test.com")
            .Build();

        var req = new UpdateCustomerRequest(kubo.FirstName, kubo.LastName, kubo.Email, kubo.Age);
        _repositoryMock.Setup(r => r.GetByIdAsync(kubo.Id, default)).ReturnsAsync((Customer?)null);
        await Assert.ThrowsAsync<NotFoundException>(() => _customerService.UpdateAsync(kubo.Id, req));
    }

    [Fact]
    public async Task Delete_Existing_Completes()
    {
        var sandisiwe = CustomerBuilder.Unique();
        _repositoryMock.Setup(r => r.DeleteAsync(sandisiwe.Id, default)).ReturnsAsync(true);
        await _customerService.Invoking(s => s.DeleteAsync(sandisiwe.Id)).Should().NotThrowAsync();
    }

    [Fact]
    public async Task Delete_Missing_ThrowsNotFoundException()
    {
        var kubo = new CustomerBuilder()
            .WithFirstName("Kubo")
            .WithLastName("Vutula")
            .WithAge(20)
            .WithEmail($"u_{Guid.NewGuid():N}@test.com")
            .Build();

        _repositoryMock.Setup(r => r.DeleteAsync(kubo.Id, default)).ReturnsAsync(false);
        await Assert.ThrowsAsync<NotFoundException>(() => _customerService.DeleteAsync(kubo.Id));
    }
}
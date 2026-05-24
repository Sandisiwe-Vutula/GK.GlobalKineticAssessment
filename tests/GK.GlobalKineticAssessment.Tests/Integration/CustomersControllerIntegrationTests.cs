using FluentAssertions;
using GK.GlobalKineticAssessment.Application.DTOs;
using GK.GlobalKineticAssessment.Domain.Entities;
using GK.GlobalKineticAssessment.Tests.Helpers;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Xunit;

namespace GK.GlobalKineticAssessment.Tests.Integration;

[Trait("Category", "Integration")]
public sealed class CustomersControllerIntegrationTests
    : IClassFixture<GkWebApplicationFactory>
{
    private readonly HttpClient _client;
    private readonly GkWebApplicationFactory _factory;
    private static readonly JsonSerializerOptions Opts =
        new() { PropertyNameCaseInsensitive = true };

    public CustomersControllerIntegrationTests(GkWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateAuthenticatedClient();
    }

    // POST 

    [Fact]
    public async Task Post_ValidRequest_Returns201WithBody()
    {
        var c = CustomerBuilder.Unique();
        var req = new CreateCustomerRequest(c.FirstName, c.LastName, c.Email, c.Age);
        var res = await _client.PostAsJsonAsync("/api/v1/customers", req);

        res.StatusCode.Should().Be(HttpStatusCode.Created);
        res.Headers.Location.Should().NotBeNull();

        var body = await res.Content.ReadFromJsonAsync<CustomerResponse>(Opts);
        body!.Id.Should().NotBeEmpty();
        body.FirstName.Should().Be(c.FirstName);
        body.Email.Should().Be(c.Email.ToLowerInvariant());
    }

    [Fact]
    public async Task Post_InvalidEmail_Returns422()
    {
        var c = new CustomerBuilder().WithEmail("bad-email").Build();
        var res = await _client.PostAsJsonAsync("/api/v1/customers",
            new CreateCustomerRequest(c.FirstName, c.LastName, c.Email, c.Age));
        res.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
    }

    [Fact]
    public async Task Post_EmptyFirstName_Returns422()
    {
        var c = new CustomerBuilder().WithFirstName("").WithEmail($"u_{Guid.NewGuid():N}@test.com").Build();
        var res = await _client.PostAsJsonAsync("/api/v1/customers",
            new CreateCustomerRequest(c.FirstName, c.LastName, c.Email, c.Age));
        res.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
    }

    [Fact]
    public async Task Post_DuplicateEmail_Returns409()
    {
        var sandisiwe = CustomerBuilder.Unique();
        await CreateAsync(sandisiwe);

        var nosiphiwo = new CustomerBuilder()
            .WithFirstName("Nosiphiwo")
            .WithLastName("Buso")
            .WithAge(30)
            .WithEmail(sandisiwe.Email) // same email triggers 409
            .Build();

        var res = await _client.PostAsJsonAsync("/api/v1/customers",
            new CreateCustomerRequest(
                nosiphiwo.FirstName, nosiphiwo.LastName, nosiphiwo.Email, nosiphiwo.Age));

        res.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task Post_AgeOutOfRange_Returns422()
    {
        var c = new CustomerBuilder().WithAge(200).WithEmail($"u_{Guid.NewGuid():N}@test.com").Build();
        var res = await _client.PostAsJsonAsync("/api/v1/customers",
            new CreateCustomerRequest(c.FirstName, c.LastName, c.Email, c.Age));
        res.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
    }

    // GET

    [Fact]
    public async Task Get_ExistingId_Returns200()
    {
        var c = CustomerBuilder.Unique();
        var created = await CreateAsync(c);

        var res = await _client.GetAsync($"/api/v1/customers/{created.Id}");

        res.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await res.Content.ReadFromJsonAsync<CustomerResponse>(Opts);
        body!.Id.Should().Be(created.Id);
        body.FirstName.Should().Be(c.FirstName);
    }

    [Fact]
    public async Task Get_NonExistingId_Returns404()
    {
        var res = await _client.GetAsync($"/api/v1/customers/{Guid.NewGuid()}");
        res.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // GET all

    [Fact]
    public async Task GetAll_Returns200WithPagedResponse()
    {
        await CreateAsync(CustomerBuilder.Unique());
        await CreateAsync(new CustomerBuilder()
            .WithFirstName("Nosiphiwo").WithLastName("Buso").WithAge(30)
            .WithEmail($"u_{Guid.NewGuid():N}@test.com").Build());

        var res = await _client.GetAsync("/api/v1/customers?pageNumber=1&pageSize=5");

        res.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await res.Content.ReadFromJsonAsync<PagedResponse<CustomerResponse>>(Opts);
        body!.PageNumber.Should().Be(1);
        body.PageSize.Should().Be(5);
    }

    [Fact]
    public async Task GetAll_WithFilter_ReturnsMatchingOnly()
    {
        var kubo = new CustomerBuilder()
            .WithFirstName("Kubo")
            .WithLastName("Vutula")
            .WithAge(20)
            .WithEmail($"u_{Guid.NewGuid():N}@test.com")
            .Build();

        await CreateAsync(kubo);

        var res = await _client.GetAsync(
            $"/api/v1/customers?firstName={kubo.FirstName}&pageNumber=1&pageSize=10");

        res.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await res.Content.ReadFromJsonAsync<PagedResponse<CustomerResponse>>(Opts);
        body!.TotalCount.Should().BeGreaterThan(0);
        body.Items.Should().OnlyContain(c =>
            c.FirstName.Contains(kubo.FirstName, StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task GetAll_InvalidPageSize_Returns422()
    {
        var res = await _client.GetAsync("/api/v1/customers?pageSize=0");
        res.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
    }

    // PUT

    [Fact]
    public async Task Put_ValidRequest_Returns200WithUpdatedValues()
    {
        var sandisiwe = CustomerBuilder.Unique();
        var created = await CreateAsync(sandisiwe);

        var updated = new CustomerBuilder()
            .WithFirstName("Global")
            .WithLastName("Kinetic")
            .WithAge(25)
            .WithEmail($"u_{Guid.NewGuid():N}@test.com")
            .Build();

        var res = await _client.PutAsJsonAsync($"/api/v1/customers/{created.Id}",
            new UpdateCustomerRequest(
                updated.FirstName, updated.LastName, updated.Email, updated.Age));

        res.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await res.Content.ReadFromJsonAsync<CustomerResponse>(Opts);
        body!.FirstName.Should().Be(updated.FirstName);
        body.Age.Should().Be(updated.Age);
        body.UpdatedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task Put_NonExistingId_Returns404()
    {
        var c = CustomerBuilder.Unique();
        var res = await _client.PutAsJsonAsync($"/api/v1/customers/{Guid.NewGuid()}",
            new UpdateCustomerRequest(c.FirstName, c.LastName, c.Email, c.Age));
        res.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Put_InvalidEmail_Returns422()
    {
        var c = CustomerBuilder.Unique();
        var created = await CreateAsync(c);
        var res = await _client.PutAsJsonAsync($"/api/v1/customers/{created.Id}",
            new UpdateCustomerRequest(c.FirstName, c.LastName, "not-an-email", c.Age));
        res.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
    }

    // DELETE

    [Fact]
    public async Task Delete_ExistingId_Returns204()
    {
        var created = await CreateAsync(CustomerBuilder.Unique());
        var res = await _client.DeleteAsync($"/api/v1/customers/{created.Id}");
        res.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task Delete_NonExistingId_Returns404()
    {
        var res = await _client.DeleteAsync($"/api/v1/customers/{Guid.NewGuid()}");
        res.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Delete_ThenGet_Returns404()
    {
        var created = await CreateAsync(CustomerBuilder.Unique());
        await _client.DeleteAsync($"/api/v1/customers/{created.Id}");

        var res = await _client.GetAsync($"/api/v1/customers/{created.Id}");
        res.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // AUTH

    [Fact]
    public async Task NoAuth_Returns401()
    {
        using var anon = _factory.CreateClient();
        var res = await anon.GetAsync("/api/v1/customers");
        res.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task WrongCredentials_Returns401()
    {
        using var bad = _factory.CreateClient();
        var creds = Convert.ToBase64String(
            System.Text.Encoding.UTF8.GetBytes("wrong:credentials"));
        bad.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", creds);
        var res = await bad.GetAsync("/api/v1/customers");
        res.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Swagger_IsPublic_Returns200()
    {
        using var anon = _factory.CreateClient();
        var res = await anon.GetAsync("/swagger/index.html");
        res.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Health_IsPublic_Returns200()
    {
        using var anon = _factory.CreateClient();
        var res = await anon.GetAsync("/health");
        res.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    // HELPER

    private async Task<CustomerResponse> CreateAsync(Customer c)
    {
        var res = await _client.PostAsJsonAsync("/api/v1/customers",
            new CreateCustomerRequest(c.FirstName, c.LastName, c.Email, c.Age));
        res.EnsureSuccessStatusCode();
        return (await res.Content.ReadFromJsonAsync<CustomerResponse>(Opts))!;
    }
}
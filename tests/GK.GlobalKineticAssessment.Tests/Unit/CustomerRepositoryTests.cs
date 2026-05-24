using Xunit;
using FluentAssertions;
using GK.GlobalKineticAssessment.Infrastructure.Encryption;
using GK.GlobalKineticAssessment.Infrastructure.Persistence;
using GK.GlobalKineticAssessment.Infrastructure.Repositories;
using GK.GlobalKineticAssessment.Tests.Helpers;
using Microsoft.EntityFrameworkCore;

namespace GK.GlobalKineticAssessment.Tests.Unit;

[Trait("Category", "Unit")]
public sealed class CustomerRepositoryTests : IAsyncLifetime
{
    private AppDbContext _ctx = null!;
    private CustomerRepository _sut = null!;

    public Task InitializeAsync()
    {
        var opts = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase($"RepoTest_{Guid.NewGuid()}")
            .Options;
        _ctx = new AppDbContext(opts, new NoOpEncryptionService());
        _sut = new CustomerRepository(_ctx);
        return Task.CompletedTask;
    }

    public async Task DisposeAsync()
    {
        await _ctx.Database.EnsureDeletedAsync();
        await _ctx.DisposeAsync();
    }

    [Fact]
    public async Task Add_ThenGetById_ReturnsCustomer()
    {
        var c = await _sut.AddAsync(CustomerBuilder.Unique());
        var found = await _sut.GetByIdAsync(c.Id);
        found.Should().NotBeNull();
        found!.Id.Should().Be(c.Id);
    }

    [Fact]
    public async Task GetById_Missing_ReturnsNull()
    {
        var result = await _sut.GetByIdAsync(Guid.NewGuid());
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetAll_ReturnsAllAdded()
    {
        await _sut.AddAsync(CustomerBuilder.Unique());
        await _sut.AddAsync(new CustomerBuilder()
            .WithFirstName("Nosiphiwo")
            .WithLastName("Buso")
            .WithAge(30)
            .WithEmail($"u_{Guid.NewGuid():N}@test.com")
            .Build());

        var all = await _sut.GetAllAsync();
        all.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetPaged_ReturnsCorrectPage()
    {
        await _sut.AddAsync(CustomerBuilder.Unique());
        await _sut.AddAsync(new CustomerBuilder()
            .WithFirstName("Nosiphiwo").WithLastName("Buso").WithAge(30)
            .WithEmail($"u_{Guid.NewGuid():N}@test.com").Build());
        await _sut.AddAsync(new CustomerBuilder()
            .WithFirstName("Global").WithLastName("Kinetic").WithAge(25)
            .WithEmail($"u_{Guid.NewGuid():N}@test.com").Build());
        await _sut.AddAsync(new CustomerBuilder()
            .WithFirstName("Kubo").WithLastName("Vutula").WithAge(20)
            .WithEmail($"u_{Guid.NewGuid():N}@test.com").Build());
        await _sut.AddAsync(CustomerBuilder.Unique());

        var (items, total) = await _sut.GetPagedAsync(null, 2, 2);
        total.Should().Be(5);
        items.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetPaged_WithFilter_ReturnsMatching()
    {
        await _sut.AddAsync(new CustomerBuilder()
            .WithFirstName("Sandisiwe")
            .WithLastName("Vutula")
            .WithAge(35)
            .WithEmail($"u_{Guid.NewGuid():N}@test.com")
            .Build());
        await _sut.AddAsync(new CustomerBuilder()
            .WithFirstName("Nosiphiwo")
            .WithLastName("Buso")
            .WithAge(30)
            .WithEmail($"u_{Guid.NewGuid():N}@test.com")
            .Build());

        var (items, total) = await _sut.GetPagedAsync("Sandisiwe", 1, 10);
        total.Should().Be(1);
        items.First().FirstName.Should().Be("Sandisiwe");
    }

    [Fact]
    public async Task Update_ChangesArePersisted()
    {
        var c = await _sut.AddAsync(new CustomerBuilder()
            .WithFirstName("Sandisiwe")
            .WithLastName("Vutula")
            .WithAge(35)
            .WithEmail($"u_{Guid.NewGuid():N}@test.com")
            .Build());

        c.FirstName = "Global";
        c.UpdatedAt = DateTime.UtcNow;
        await _sut.UpdateAsync(c);
        _ctx.Entry(c).State = Microsoft.EntityFrameworkCore.EntityState.Detached;

        var updated = await _sut.GetByIdAsync(c.Id);
        updated!.FirstName.Should().Be("Global");
    }

    [Fact]
    public async Task Delete_Existing_ReturnsTrue()
    {
        var c = await _sut.AddAsync(CustomerBuilder.Unique());
        var result = await _sut.DeleteAsync(c.Id);
        result.Should().BeTrue();
        (await _sut.GetByIdAsync(c.Id)).Should().BeNull();
    }

    [Fact]
    public async Task Delete_Missing_ReturnsFalse()
    {
        var result = await _sut.DeleteAsync(Guid.NewGuid());
        result.Should().BeFalse();
    }

    [Fact]
    public async Task Exists_ReturnsTrueForExisting()
    {
        var c = await _sut.AddAsync(CustomerBuilder.Unique());
        (await _sut.ExistsAsync(c.Id)).Should().BeTrue();
        (await _sut.ExistsAsync(Guid.NewGuid())).Should().BeFalse();
    }

    [Fact]
    public async Task EmailExists_ReturnsTrueForDuplicate()
    {
        var sandisiwe = new CustomerBuilder()
            .WithEmail("sandisiwevutula28@gmail.com")
            .Build();
        await _sut.AddAsync(sandisiwe);

        (await _sut.EmailExistsAsync("sandisiwevutula28@gmail.com")).Should().BeTrue();
    }

    [Fact]
    public async Task EmailExists_UpdatingOwnRecord_ReturnsFalse()
    {
        var sandisiwe = new CustomerBuilder()
            .WithEmail("sandisiwevutula28@gmail.com")
            .Build();
        var c = await _sut.AddAsync(sandisiwe);

        (await _sut.EmailExistsAsync("sandisiwevutula28@gmail.com", c.Id)).Should().BeFalse();
    }

    private sealed class NoOpEncryptionService : IEncryptionService
    {
        public string Encrypt(string p) => p;
        public string Decrypt(string c) => c;
    }
}
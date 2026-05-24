using GK.GlobalKineticAssessment.Domain.Entities;

namespace GK.GlobalKineticAssessment.Tests.Helpers;

public sealed class CustomerBuilder
{
    private Guid _id = Guid.NewGuid();
    private string _first = "Sandisiwe";
    private string _last  = "Vutula";
    private string _email = "sandisiwevutula28@gmail.com";
    private int _age = 35;

    public CustomerBuilder WithId(Guid id) { _id = id;  return this; }
    public CustomerBuilder WithFirstName(string v) { _first = v; return this; }
    public CustomerBuilder WithLastName(string v) { _last = v; return this; }
    public CustomerBuilder WithEmail(string v) { _email = v; return this; }
    public CustomerBuilder WithAge(int v) { _age = v; return this; }

    public Customer Build() => new()
    { Id = _id, FirstName = _first, LastName = _last, Email = _email, Age = _age, CreatedAt = DateTime.UtcNow };

    public static Customer Default() => new CustomerBuilder().Build();
    public static Customer Unique()  => new CustomerBuilder().WithEmail($"u_{Guid.NewGuid():N}@test.com").Build();
}

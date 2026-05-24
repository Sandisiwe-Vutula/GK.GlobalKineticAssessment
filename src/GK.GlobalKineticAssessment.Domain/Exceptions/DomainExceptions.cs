namespace GK.GlobalKineticAssessment.Domain.Exceptions;

public sealed class NotFoundException : Exception
{
    public NotFoundException(string name, object key)
        : base($"{name} with id '{key}' was not found.") { }
}

public sealed class DuplicateEmailException : Exception
{
    public DuplicateEmailException(string email)
        : base($"A customer with email '{email}' already exists.") { }
}

public sealed class ValidationException : Exception
{
    public IReadOnlyDictionary<string, string[]> Errors { get; }

    public ValidationException(IDictionary<string, string[]> errors)
        : base("One or more validation errors occurred.")
    {
        Errors = new Dictionary<string, string[]>(errors);
    }
}

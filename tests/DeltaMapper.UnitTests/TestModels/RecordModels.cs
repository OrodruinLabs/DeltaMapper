namespace DeltaMapper.UnitTests.TestModels;

public record PersonRecord(string FirstName, string LastName, int Age);
public record PersonRecordDto(string FirstName, string LastName, int Age);

// Record with additional init properties
public record ExtendedPersonRecord(string FirstName, string LastName)
{
    public int Age { get; init; }
    public string Email { get; init; } = string.Empty;
}

public record ExtendedPersonRecordDto(string FirstName, string LastName)
{
    public int Age { get; init; }
    public string Email { get; init; } = string.Empty;
}

// Init-only class (not a record)
public class PersonInitOnly
{
    public string FirstName { get; init; } = string.Empty;
    public string LastName { get; init; } = string.Empty;
    public int Age { get; init; }
}

public class PersonInitOnlyDto
{
    public string FirstName { get; init; } = string.Empty;
    public string LastName { get; init; } = string.Empty;
    public int Age { get; init; }
}

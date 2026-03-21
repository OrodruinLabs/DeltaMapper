namespace DeltaMapper.TestFixtures;

public class FixtureSource
{
    public int Id { get; set; }
    public string Value { get; set; } = "";
}

public class FixtureDest
{
    public int Id { get; set; }
    public string Value { get; set; } = "";
}

public class FixtureProfile : Profile
{
    public FixtureProfile()
    {
        CreateMap<FixtureSource, FixtureDest>();
    }
}

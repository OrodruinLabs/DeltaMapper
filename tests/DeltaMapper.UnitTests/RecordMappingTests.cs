using DeltaMapper;
using DeltaMapper.UnitTests.TestModels;
using FluentAssertions;
using Xunit;

namespace DeltaMapper.UnitTests;

public class RecordMappingTests
{
    // ── REC-01 ────────────────────────────────────────────────────────────────
    private class REC01_PersonRecordProfile : MappingProfile
    {
        public REC01_PersonRecordProfile()
        {
            CreateMap<PersonRecord, PersonRecordDto>();
        }
    }

    [Fact]
    public void Map_RecordType_MapsViaConstructor()
    {
        var config = MapperConfiguration.Create(cfg => cfg.AddProfile<REC01_PersonRecordProfile>());
        var mapper = config.CreateMapper();

        var source = new PersonRecord("Jane", "Smith", 28);

        var result = mapper.Map<PersonRecord, PersonRecordDto>(source);

        result.FirstName.Should().Be("Jane");
        result.LastName.Should().Be("Smith");
        result.Age.Should().Be(28);
    }

    // ── REC-02 ────────────────────────────────────────────────────────────────
    private class REC02_ExtendedPersonRecordProfile : MappingProfile
    {
        public REC02_ExtendedPersonRecordProfile()
        {
            CreateMap<ExtendedPersonRecord, ExtendedPersonRecordDto>();
        }
    }

    [Fact]
    public void Map_RecordWithAdditionalProperties_MapsAll()
    {
        var config = MapperConfiguration.Create(cfg => cfg.AddProfile<REC02_ExtendedPersonRecordProfile>());
        var mapper = config.CreateMapper();

        var source = new ExtendedPersonRecord("Alice", "Jones")
        {
            Age = 35,
            Email = "alice@example.com"
        };

        var result = mapper.Map<ExtendedPersonRecord, ExtendedPersonRecordDto>(source);

        result.FirstName.Should().Be("Alice");
        result.LastName.Should().Be("Jones");
        result.Age.Should().Be(35);
        result.Email.Should().Be("alice@example.com");
    }

    // ── REC-03 ────────────────────────────────────────────────────────────────
    private class REC03_PersonInitOnlyProfile : MappingProfile
    {
        public REC03_PersonInitOnlyProfile()
        {
            CreateMap<PersonInitOnly, PersonInitOnlyDto>();
        }
    }

    [Fact]
    public void Map_InitOnlyProperties_MapsCorrectly()
    {
        var config = MapperConfiguration.Create(cfg => cfg.AddProfile<REC03_PersonInitOnlyProfile>());
        var mapper = config.CreateMapper();

        var source = new PersonInitOnly
        {
            FirstName = "Bob",
            LastName = "Builder",
            Age = 42
        };

        var result = mapper.Map<PersonInitOnly, PersonInitOnlyDto>(source);

        result.FirstName.Should().Be("Bob");
        result.LastName.Should().Be("Builder");
        result.Age.Should().Be(42);
    }

    // ── REC-04 ────────────────────────────────────────────────────────────────
    private class REC04_PersonRecordToRecordProfile : MappingProfile
    {
        public REC04_PersonRecordToRecordProfile()
        {
            CreateMap<PersonRecord, PersonRecordDto>();
        }
    }

    [Fact]
    public void Map_RecordToRecord_MapsCorrectly()
    {
        var config = MapperConfiguration.Create(cfg => cfg.AddProfile<REC04_PersonRecordToRecordProfile>());
        var mapper = config.CreateMapper();

        var source = new PersonRecord("Carlos", "Diaz", 31);

        var result = mapper.Map<PersonRecord, PersonRecordDto>(source);

        result.FirstName.Should().Be("Carlos");
        result.LastName.Should().Be("Diaz");
        result.Age.Should().Be(31);
    }

    // ── REC-05 ────────────────────────────────────────────────────────────────
    private class REC05_PersonRecordForMemberProfile : MappingProfile
    {
        public REC05_PersonRecordForMemberProfile()
        {
            CreateMap<PersonRecord, PersonRecordDto>()
                .ForMember(d => d.FirstName, o => o.MapFrom(s => s.FirstName.ToUpper()));
        }
    }

    [Fact]
    public void Map_RecordWithForMember_AppliesOverride()
    {
        var config = MapperConfiguration.Create(cfg => cfg.AddProfile<REC05_PersonRecordForMemberProfile>());
        var mapper = config.CreateMapper();

        var source = new PersonRecord("Dana", "Lee", 25);

        var result = mapper.Map<PersonRecord, PersonRecordDto>(source);

        result.FirstName.Should().Be("DANA");
        result.LastName.Should().Be("Lee");
        result.Age.Should().Be(25);
    }
}

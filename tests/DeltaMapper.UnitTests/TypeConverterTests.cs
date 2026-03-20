using System.Globalization;
using DeltaMapper;
using FluentAssertions;
using Xunit;

namespace DeltaMapper.UnitTests;

// ── Test models ────────────────────────────────────────────────────

public class TC_Source_StringDate
{
    public string? BirthDate { get; set; }
    public string Name { get; set; } = string.Empty;
}

public class TC_Dest_DateTimeDate
{
    public DateTime BirthDate { get; set; }
    public string Name { get; set; } = string.Empty;
}

public class TC_Source_IntString
{
    public int Code { get; set; }
    public string Label { get; set; } = string.Empty;
}

public class TC_Dest_StringInt
{
    public string Code { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
}

public class TC_Source_Multi
{
    public string? DateValue { get; set; }
    public int IntValue { get; set; }
}

public class TC_Dest_Multi
{
    public DateTime DateValue { get; set; }
    public string IntValue { get; set; } = string.Empty;
}

public class TC_Source_NullString
{
    public string? Value { get; set; }
}

public class TC_Dest_NullableDateTime
{
    public DateTime? Value { get; set; }
}

public sealed class RecordWithDateSource
{
    public string Name { get; set; } = "";
    public string BirthDate { get; set; } = "";
}

public record RecordWithDateDest(string Name, DateTime BirthDate);

public sealed class StringIntSource { public string Value { get; set; } = ""; }
public sealed class StringIntDest { public int Value { get; set; } }

// ── Profiles ───────────────────────────────────────────────────────

file class TC_StringDateProfile : Profile
{
    public TC_StringDateProfile() => CreateMap<TC_Source_StringDate, TC_Dest_DateTimeDate>();
}

file class TC_IntStringProfile : Profile
{
    public TC_IntStringProfile() => CreateMap<TC_Source_IntString, TC_Dest_StringInt>();
}

file class TC_MultiProfile : Profile
{
    public TC_MultiProfile() => CreateMap<TC_Source_Multi, TC_Dest_Multi>();
}

file class TC_NullProfile : Profile
{
    public TC_NullProfile() => CreateMap<TC_Source_NullString, TC_Dest_NullableDateTime>();
}

file class TCInlineProfile<TSource, TDest> : Profile
{
    public TCInlineProfile() => CreateMap<TSource, TDest>();
}

// ── Tests ──────────────────────────────────────────────────────────

public class TypeConverterTests
{
    // ── TC-01: string → DateTime via global converter ─────────────────────────
    [Fact]
    public void TypeConverter_StringToDateTime_ConvertsCorrectly()
    {
        var config = MapperConfiguration.Create(cfg =>
        {
            cfg.CreateTypeConverter<string, DateTime>(s => DateTime.Parse(s, CultureInfo.InvariantCulture));
            cfg.AddProfile<TC_StringDateProfile>();
        });
        var mapper = config.CreateMapper();

        var source = new TC_Source_StringDate
        {
            BirthDate = "2000-06-15",
            Name = "Alice"
        };

        var result = mapper.Map<TC_Source_StringDate, TC_Dest_DateTimeDate>(source);

        result.BirthDate.Should().Be(new DateTime(2000, 6, 15));
        result.Name.Should().Be("Alice");
    }

    // ── TC-02: int → string via global converter ──────────────────────────────
    [Fact]
    public void TypeConverter_IntToString_ConvertsCorrectly()
    {
        var config = MapperConfiguration.Create(cfg =>
        {
            cfg.CreateTypeConverter<int, string>(i => i.ToString("D4"));
            cfg.AddProfile<TC_IntStringProfile>();
        });
        var mapper = config.CreateMapper();

        var source = new TC_Source_IntString { Code = 42, Label = "test" };

        var result = mapper.Map<TC_Source_IntString, TC_Dest_StringInt>(source);

        result.Code.Should().Be("0042");
        result.Label.Should().Be("test");
    }

    // ── TC-03: Convention matching still works for same-type properties ────────
    [Fact]
    public void TypeConverter_DoesNotInterferWithSameTypeConventionMapping()
    {
        var config = MapperConfiguration.Create(cfg =>
        {
            // Register a converter that would only apply when types differ
            cfg.CreateTypeConverter<string, DateTime>(s => DateTime.Parse(s, CultureInfo.InvariantCulture));
            cfg.AddProfile<TC_StringDateProfile>();
        });
        var mapper = config.CreateMapper();

        var source = new TC_Source_StringDate
        {
            BirthDate = "2025-01-01",
            Name = "SameTypeCheck"
        };

        var result = mapper.Map<TC_Source_StringDate, TC_Dest_DateTimeDate>(source);

        // Name is string → string, should use direct convention mapping (not converter)
        result.Name.Should().Be("SameTypeCheck");
    }

    // ── TC-04: Multiple converters registered, both work ─────────────────────
    [Fact]
    public void TypeConverter_MultipleConverters_BothApply()
    {
        var config = MapperConfiguration.Create(cfg =>
        {
            cfg.CreateTypeConverter<string, DateTime>(s => DateTime.Parse(s, CultureInfo.InvariantCulture));
            cfg.CreateTypeConverter<int, string>(i => $"#{i}");
            cfg.AddProfile<TC_MultiProfile>();
        });
        var mapper = config.CreateMapper();

        var source = new TC_Source_Multi
        {
            DateValue = "2024-03-10",
            IntValue = 99
        };

        var result = mapper.Map<TC_Source_Multi, TC_Dest_Multi>(source);

        result.DateValue.Should().Be(new DateTime(2024, 3, 10));
        result.IntValue.Should().Be("#99");
    }

    // ── TC-05: Null source value — should not throw, converter returns null ───
    [Fact]
    public void TypeConverter_NullSourceValue_DoesNotThrow()
    {
        var config = MapperConfiguration.Create(cfg =>
        {
            cfg.CreateTypeConverter<string, DateTime?>(s => string.IsNullOrEmpty(s) ? (DateTime?)null : DateTime.Parse(s, CultureInfo.InvariantCulture));
            cfg.AddProfile<TC_NullProfile>();
        });
        var mapper = config.CreateMapper();

        var source = new TC_Source_NullString { Value = null };

        var act = () => mapper.Map<TC_Source_NullString, TC_Dest_NullableDateTime>(source);

        act.Should().NotThrow();
        var result = mapper.Map<TC_Source_NullString, TC_Dest_NullableDateTime>(source);
        result.Value.Should().BeNull();
    }

    // ── TC-06: Null converter argument throws ArgumentNullException ───────────
    [Fact]
    public void TypeConverter_NullConverterArgument_ThrowsArgumentNullException()
    {
        var act = () => MapperConfiguration.Create(cfg =>
        {
            cfg.CreateTypeConverter<string, DateTime>(null!);
        });

        act.Should().Throw<ArgumentNullException>();
    }

    // ── TC-07: Type converter with record / constructor-injection mapping ─────
    [Fact]
    public void TC07_TypeConverter_WithRecordConstructorInjection()
    {
        var mapper = MapperConfiguration.Create(cfg =>
        {
            cfg.CreateTypeConverter<string, DateTime>(s => DateTime.Parse(s, CultureInfo.InvariantCulture));
            cfg.AddProfile(new TCInlineProfile<RecordWithDateSource, RecordWithDateDest>());
        }).CreateMapper();

        var src = new RecordWithDateSource { Name = "Alice", BirthDate = "1990-05-15" };
        var dst = mapper.Map<RecordWithDateSource, RecordWithDateDest>(src);

        dst.Name.Should().Be("Alice");
        dst.BirthDate.Should().Be(new DateTime(1990, 5, 15));
    }

    // ── TC-08: Duplicate converter registration — last one wins ──────────────
    [Fact]
    public void TC08_DuplicateConverter_LastWins()
    {
        var mapper = MapperConfiguration.Create(cfg =>
        {
            cfg.CreateTypeConverter<string, int>(s => 0);
            cfg.CreateTypeConverter<string, int>(s => int.Parse(s));
            cfg.AddProfile(new TCInlineProfile<StringIntSource, StringIntDest>());
        }).CreateMapper();

        var src = new StringIntSource { Value = "42" };
        var dst = mapper.Map<StringIntSource, StringIntDest>(src);

        dst.Value.Should().Be(42);
    }
}

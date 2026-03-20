using DeltaMapper;
using Xunit;

namespace DeltaMapper.UnitTests;

public class NullableCoercionTests
{
    private class Source
    {
        public Guid? TrackingId { get; set; }
        public int? Count { get; set; }
        public DateTime? CreatedAt { get; set; }
        public bool? IsActive { get; set; }
    }

    private class Dest
    {
        public Guid TrackingId { get; set; }
        public int Count { get; set; }
        public DateTime CreatedAt { get; set; }
        public bool IsActive { get; set; }
    }

    private class NullableCoercionProfile : Profile
    {
        public NullableCoercionProfile()
        {
            CreateMap<Source, Dest>();
        }
    }

    [Fact]
    public void NullableGuid_to_Guid_with_value_maps_value()
    {
        var mapper = MapperConfiguration.Create(cfg => cfg.AddProfile<NullableCoercionProfile>()).CreateMapper();
        var id = Guid.NewGuid();
        var result = mapper.Map<Source, Dest>(new Source { TrackingId = id });
        Assert.Equal(id, result.TrackingId);
    }

    [Fact]
    public void NullableGuid_to_Guid_with_null_maps_default()
    {
        var mapper = MapperConfiguration.Create(cfg => cfg.AddProfile<NullableCoercionProfile>()).CreateMapper();
        var result = mapper.Map<Source, Dest>(new Source { TrackingId = null });
        Assert.Equal(Guid.Empty, result.TrackingId);
    }

    [Fact]
    public void NullableInt_to_int_with_null_maps_zero()
    {
        var mapper = MapperConfiguration.Create(cfg => cfg.AddProfile<NullableCoercionProfile>()).CreateMapper();
        var result = mapper.Map<Source, Dest>(new Source { Count = null });
        Assert.Equal(0, result.Count);
    }

    [Fact]
    public void NullableDateTime_to_DateTime_with_null_maps_default()
    {
        var mapper = MapperConfiguration.Create(cfg => cfg.AddProfile<NullableCoercionProfile>()).CreateMapper();
        var result = mapper.Map<Source, Dest>(new Source { CreatedAt = null });
        Assert.Equal(default(DateTime), result.CreatedAt);
    }

    [Fact]
    public void NullableBool_to_bool_with_null_maps_false()
    {
        var mapper = MapperConfiguration.Create(cfg => cfg.AddProfile<NullableCoercionProfile>()).CreateMapper();
        var result = mapper.Map<Source, Dest>(new Source { IsActive = null });
        Assert.False(result.IsActive);
    }

    [Fact]
    public void NullableInt_to_int_with_value_maps_value()
    {
        var mapper = MapperConfiguration.Create(cfg => cfg.AddProfile<NullableCoercionProfile>()).CreateMapper();
        var result = mapper.Map<Source, Dest>(new Source { Count = 42 });
        Assert.Equal(42, result.Count);
    }
}

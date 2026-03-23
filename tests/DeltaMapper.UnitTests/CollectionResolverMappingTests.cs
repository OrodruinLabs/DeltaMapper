using FluentAssertions;
using Xunit;

namespace DeltaMapper.UnitTests;

public class CollectionResolverMappingTests
{
    // ── Models ──────────────────────────────────────────────────────────────
    private class WishItem
    {
        public string Name { get; set; } = "";
        public bool IsActive { get; set; } = true;
    }

    private record WishItemDto
    {
        public string Name { get; init; } = "";
    }

    private class WishList
    {
        public string Title { get; set; } = "";
        public List<WishItem> Items { get; set; } = new();
    }

    private record WishListDto
    {
        public string Title { get; init; } = "";
        public int ItemCount { get; init; }
        public IReadOnlyList<WishItemDto> Items { get; init; } = [];
    }

    // ── CR-01: ForMember/MapFrom with IReadOnlyList<T> on record ────────
    private class CR01_Profile : Profile
    {
        public CR01_Profile()
        {
            CreateMap<WishItem, WishItemDto>(MemberList.None);
            CreateMap<WishList, WishListDto>(MemberList.None)
                .ForMember(d => d.ItemCount, o => o.MapFrom(s => s.Items.Count(i => i.IsActive)))
                .ForMember(d => d.Items, o => o.MapFrom(s => s.Items));
        }
    }

    [Fact]
    public void CR01_ForMember_MapFrom_IReadOnlyList_on_record_maps_elements()
    {
        var config = MapperConfiguration.Create(cfg => cfg.AddProfile<CR01_Profile>());
        var mapper = config.CreateMapper();

        var source = new WishList
        {
            Title = "Guitars",
            Items = new List<WishItem>
            {
                new() { Name = "Strat", IsActive = true },
                new() { Name = "Tele", IsActive = false }
            }
        };

        var result = mapper.Map<WishList, WishListDto>(source);

        result.Title.Should().Be("Guitars");
        result.ItemCount.Should().Be(1);
        result.Items.Should().HaveCount(2);
        result.Items[0].Name.Should().Be("Strat");
        result.Items[1].Name.Should().Be("Tele");
    }

    // ── CR-02: Convention-matched IReadOnlyList<T> on record (no ForMember) ─
    private class CR02_Parent
    {
        public string Name { get; set; } = "";
        public List<WishItem> Items { get; set; } = new();
    }

    private record CR02_ParentDto
    {
        public string Name { get; init; } = "";
        public IReadOnlyList<WishItemDto> Items { get; init; } = [];
    }

    private class CR02_Profile : Profile
    {
        public CR02_Profile()
        {
            CreateMap<WishItem, WishItemDto>(MemberList.None);
            CreateMap<CR02_Parent, CR02_ParentDto>(MemberList.None);
        }
    }

    [Fact]
    public void CR02_Convention_matched_IReadOnlyList_on_record_maps_elements()
    {
        var config = MapperConfiguration.Create(cfg => cfg.AddProfile<CR02_Profile>());
        var mapper = config.CreateMapper();

        var source = new CR02_Parent
        {
            Name = "Guitars",
            Items = new List<WishItem>
            {
                new() { Name = "Les Paul" },
                new() { Name = "SG" }
            }
        };

        var result = mapper.Map<CR02_Parent, CR02_ParentDto>(source);

        result.Name.Should().Be("Guitars");
        result.Items.Should().HaveCount(2);
        result.Items[0].Name.Should().Be("Les Paul");
        result.Items[1].Name.Should().Be("SG");
    }

    // ── CR-03: ForMember/MapFrom with IReadOnlyList<T> on regular class ─────
    private class CR03_ParentDto
    {
        public string Name { get; set; } = "";
        public IReadOnlyList<WishItemDto> Items { get; set; } = [];
    }

    private class CR03_Profile : Profile
    {
        public CR03_Profile()
        {
            CreateMap<WishItem, WishItemDto>(MemberList.None);
            CreateMap<WishList, CR03_ParentDto>(MemberList.None)
                .ForMember(d => d.Items, o => o.MapFrom(s => s.Items));
        }
    }

    [Fact]
    public void CR03_ForMember_MapFrom_IReadOnlyList_on_class_maps_elements()
    {
        var config = MapperConfiguration.Create(cfg => cfg.AddProfile<CR03_Profile>());
        var mapper = config.CreateMapper();

        var source = new WishList
        {
            Title = "Guitars",
            Items = new List<WishItem> { new() { Name = "Jazz Bass" } }
        };

        var result = mapper.Map<WishList, CR03_ParentDto>(source);

        result.Items.Should().ContainSingle();
        result.Items[0].Name.Should().Be("Jazz Bass");
    }

    // ── CR-04: Null source collection via ForMember/MapFrom ─────────────────
    [Fact]
    public void CR04_ForMember_MapFrom_null_collection_maps_to_null()
    {
        var config = MapperConfiguration.Create(cfg => cfg.AddProfile<CR03_Profile>());
        var mapper = config.CreateMapper();

        var source = new WishList { Title = "Empty", Items = null! };

        var result = mapper.Map<WishList, CR03_ParentDto>(source);

        result.Items.Should().BeNull();
    }

    // ── CR-05: Empty collection via ForMember/MapFrom ───────────────────────
    [Fact]
    public void CR05_ForMember_MapFrom_empty_collection_maps_to_empty()
    {
        var config = MapperConfiguration.Create(cfg => cfg.AddProfile<CR01_Profile>());
        var mapper = config.CreateMapper();

        var source = new WishList { Title = "Empty", Items = new() };

        var result = mapper.Map<WishList, WishListDto>(source);

        result.Items.Should().NotBeNull();
        result.Items.Should().BeEmpty();
    }

    // ── CR-06: ForMember/MapFrom with array destination ─────────────────────
    private class CR06_ParentDto
    {
        public string Name { get; set; } = "";
        public WishItemDto[] Items { get; set; } = [];
    }

    private class CR06_Profile : Profile
    {
        public CR06_Profile()
        {
            CreateMap<WishItem, WishItemDto>(MemberList.None);
            CreateMap<WishList, CR06_ParentDto>(MemberList.None)
                .ForMember(d => d.Items, o => o.MapFrom(s => s.Items));
        }
    }

    [Fact]
    public void CR06_ForMember_MapFrom_array_destination_maps_elements()
    {
        var config = MapperConfiguration.Create(cfg => cfg.AddProfile<CR06_Profile>());
        var mapper = config.CreateMapper();

        var source = new WishList
        {
            Title = "Guitars",
            Items = new List<WishItem> { new() { Name = "PRS" } }
        };

        var result = mapper.Map<WishList, CR06_ParentDto>(source);

        result.Items.Should().ContainSingle();
        result.Items[0].Name.Should().Be("PRS");
    }

    // ── CR-07: Constructor-param collection on record ───────────────────────
    private record CR07_ParentDto(string Title, IReadOnlyList<WishItemDto> Items);

    private class CR07_Profile : Profile
    {
        public CR07_Profile()
        {
            CreateMap<WishItem, WishItemDto>(MemberList.None);
            CreateMap<WishList, CR07_ParentDto>(MemberList.None)
                .ForMember(d => d.Items, o => o.MapFrom(s => s.Items))
                .ForMember(d => d.Title, o => o.MapFrom(s => s.Title));
        }
    }

    [Fact]
    public void CR07_ForMember_MapFrom_constructor_param_IReadOnlyList_maps_elements()
    {
        var config = MapperConfiguration.Create(cfg => cfg.AddProfile<CR07_Profile>());
        var mapper = config.CreateMapper();

        var source = new WishList
        {
            Title = "Amps",
            Items = new List<WishItem> { new() { Name = "Twin Reverb" } }
        };

        var result = mapper.Map<WishList, CR07_ParentDto>(source);

        result.Title.Should().Be("Amps");
        result.Items.Should().ContainSingle();
        result.Items[0].Name.Should().Be("Twin Reverb");
    }

    // ── CR-08: Convention-matched constructor-param collection (no ForMember) ─
    private record CR08_ParentDto(string Title, IReadOnlyList<WishItemDto> Items);

    private class CR08_Profile : Profile
    {
        public CR08_Profile()
        {
            CreateMap<WishItem, WishItemDto>(MemberList.None);
            CreateMap<WishList, CR08_ParentDto>(MemberList.None);
        }
    }

    [Fact]
    public void CR08_Convention_matched_constructor_param_IReadOnlyList_maps_elements()
    {
        var config = MapperConfiguration.Create(cfg => cfg.AddProfile<CR08_Profile>());
        var mapper = config.CreateMapper();

        var source = new WishList
        {
            Title = "Pedals",
            Items = new List<WishItem> { new() { Name = "Tube Screamer" } }
        };

        var result = mapper.Map<WishList, CR08_ParentDto>(source);

        result.Title.Should().Be("Pedals");
        result.Items.Should().ContainSingle();
        result.Items[0].Name.Should().Be("Tube Screamer");
    }
}

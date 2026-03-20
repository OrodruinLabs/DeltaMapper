using DeltaMapper.Configuration;
using FluentAssertions;
using Xunit;

namespace DeltaMapper.UnitTests;

public class MigrationFrictionR2Tests
{
    private class Brand { public string Name { get; set; } = ""; public string Country { get; set; } = ""; }
    private class BrandDto { public string Name { get; set; } = ""; public string Country { get; set; } = ""; }

    private class BrandProfile : Profile
    {
        public BrandProfile()
        {
            CreateMap<Brand, BrandDto>();
        }
    }

    [Fact]
    public void AutoMapper_style_single_generic_collection_mapping_works()
    {
        var mapper = MapperConfiguration.Create(cfg => cfg.AddProfile<BrandProfile>()).CreateMapper();

        var brands = new List<Brand>
        {
            new() { Name = "Fender", Country = "USA" },
            new() { Name = "Gibson", Country = "USA" },
            new() { Name = "Yamaha", Country = "Japan" }
        };

        // AutoMapper-style single-generic call
        var result = mapper.Map<IEnumerable<BrandDto>>(brands);

        result.Should().HaveCount(3);
        result.Select(b => b.Name).Should().ContainInOrder("Fender", "Gibson", "Yamaha");
    }

    [Fact]
    public void Assembly_scanning_with_referenced_assemblies_flag_works()
    {
        var config = MapperConfiguration.Create(cfg =>
            cfg.AddProfilesFromAssemblyContaining<MigrationFrictionR2Tests>(
                includeReferencedAssemblies: true));
        var mapper = config.CreateMapper();

        var result = mapper.Map<Brand, BrandDto>(new Brand { Name = "PRS", Country = "USA" });
        result.Name.Should().Be("PRS");
    }
}

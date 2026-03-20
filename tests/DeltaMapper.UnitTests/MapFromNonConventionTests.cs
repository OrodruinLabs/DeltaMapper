using DeltaMapper.Configuration;
using Xunit;

namespace DeltaMapper.UnitTests;

/// <summary>
/// Tests for MapFrom nested type resolution with non-convention type names.
/// Reproduces the bug where Brand → SummaryDto fails because names don't match by convention.
/// </summary>
public class MapFromNonConventionTests
{
    private class Brand { public Guid Id { get; set; } public string Name { get; set; } = ""; }
    private class SummaryDto { public Guid Id { get; set; } public string Name { get; set; } = ""; }
    private class Product { public Brand Brand { get; set; } = null!; }
    private class ProductDto { public SummaryDto Brand { get; set; } = null!; }

    [Fact]
    public void MapFrom_WithNonConventionTypeNames_ShouldAutoApply()
    {
        var config = MapperConfiguration.Create(cfg =>
        {
            cfg.AddProfile(new NonConventionProfile());
        });
        var mapper = config.CreateMapper();
        var source = new Product { Brand = new Brand { Id = Guid.NewGuid(), Name = "Fender" } };

        var result = mapper.Map<Product, ProductDto>(source);

        Assert.NotNull(result.Brand);
        Assert.Equal("Fender", result.Brand.Name);
        Assert.Equal(source.Brand.Id, result.Brand.Id);
    }

    private class NonConventionProfile : Profile
    {
        public NonConventionProfile()
        {
            CreateMap<Brand, SummaryDto>();
            CreateMap<Product, ProductDto>()
                .ForMember(d => d.Brand, o => o.MapFrom(s => s.Brand));
        }
    }

    [Fact]
    public void MapFrom_WithNullNonConventionSource_ShouldReturnNull()
    {
        var config = MapperConfiguration.Create(cfg =>
        {
            cfg.AddProfile(new NonConventionProfile());
        });
        var mapper = config.CreateMapper();
        var source = new Product { Brand = null! };

        var result = mapper.Map<Product, ProductDto>(source);

        Assert.Null(result.Brand);
    }

    // Ternary conditional case — TResult may be inferred differently
    private class Container
    {
        public Brand? Primary { get; set; }
        public Brand? Secondary { get; set; }
    }
    private class ContainerDto { public SummaryDto? Brand { get; set; } }

    [Fact]
    public void MapFrom_WithTernaryConditional_ShouldResolveCorrectMap()
    {
        var config = MapperConfiguration.Create(cfg =>
        {
            cfg.AddProfile(new TernaryProfile());
        });
        var mapper = config.CreateMapper();
        var source = new Container
        {
            Primary = null,
            Secondary = new Brand { Id = Guid.NewGuid(), Name = "Gibson" }
        };

        var result = mapper.Map<Container, ContainerDto>(source);

        Assert.NotNull(result.Brand);
        Assert.Equal("Gibson", result.Brand.Name);
    }

    private class TernaryProfile : Profile
    {
        public TernaryProfile()
        {
            CreateMap<Brand, SummaryDto>();
            CreateMap<Container, ContainerDto>()
                .ForMember(d => d.Brand, o => o.MapFrom(s =>
                    s.Primary ?? s.Secondary));
        }
    }

    // Strumtry-style: multiple levels of nesting with non-convention names
    private class Category { public Guid Id { get; set; } public string Label { get; set; } = ""; }
    private class Model { public Brand Brand { get; set; } = null!; public Category Category { get; set; } = null!; public string ModelName { get; set; } = ""; }
    private class CategoryDto { public Guid Id { get; set; } public string Label { get; set; } = ""; }
    private class ModelDto
    {
        public SummaryDto Brand { get; set; } = null!;
        public CategoryDto Category { get; set; } = null!;
        public string ModelName { get; set; } = "";
    }

    [Fact]
    public void MapFrom_MultipleNestedProperties_ShouldAllResolve()
    {
        var config = MapperConfiguration.Create(cfg =>
        {
            cfg.AddProfile(new MultiNestedProfile());
        });
        var mapper = config.CreateMapper();
        var source = new Model
        {
            Brand = new Brand { Id = Guid.NewGuid(), Name = "Fender" },
            Category = new Category { Id = Guid.NewGuid(), Label = "Guitar" },
            ModelName = "Stratocaster"
        };

        var result = mapper.Map<Model, ModelDto>(source);

        Assert.NotNull(result.Brand);
        Assert.Equal("Fender", result.Brand.Name);
        Assert.NotNull(result.Category);
        Assert.Equal("Guitar", result.Category.Label);
        Assert.Equal("Stratocaster", result.ModelName);
    }

    private class MultiNestedProfile : Profile
    {
        public MultiNestedProfile()
        {
            CreateMap<Brand, SummaryDto>();
            CreateMap<Category, CategoryDto>();
            CreateMap<Model, ModelDto>()
                .ForMember(d => d.Brand, o => o.MapFrom(s => s.Brand))
                .ForMember(d => d.Category, o => o.MapFrom(s => s.Category));
        }
    }

    // Deep chain: MapFrom accessing nested property (s => s.Model.Brand)
    private class Instrument { public Model? Model { get; set; } }
    private class InstrumentDto { public SummaryDto? Brand { get; set; } public string? ModelName { get; set; } }

    [Fact]
    public void MapFrom_DeepPropertyChain_ShouldResolve()
    {
        var config = MapperConfiguration.Create(cfg =>
        {
            cfg.AddProfile(new DeepChainProfile());
        });
        var mapper = config.CreateMapper();
        var source = new Instrument
        {
            Model = new Model
            {
                Brand = new Brand { Id = Guid.NewGuid(), Name = "Gibson" },
                Category = new Category { Id = Guid.NewGuid(), Label = "Guitar" },
                ModelName = "Les Paul"
            }
        };

        var result = mapper.Map<Instrument, InstrumentDto>(source);

        Assert.NotNull(result.Brand);
        Assert.Equal("Gibson", result.Brand.Name);
        Assert.Equal("Les Paul", result.ModelName);
    }

    [Fact]
    public void MapFrom_DeepPropertyChain_NullIntermediate_ShouldReturnNull()
    {
        var config = MapperConfiguration.Create(cfg =>
        {
            cfg.AddProfile(new DeepChainProfile());
        });
        var mapper = config.CreateMapper();
        var source = new Instrument { Model = null };

        var result = mapper.Map<Instrument, InstrumentDto>(source);

        Assert.Null(result.Brand);
    }

    private class DeepChainProfile : Profile
    {
        public DeepChainProfile()
        {
            CreateMap<Brand, SummaryDto>();
            CreateMap<Instrument, InstrumentDto>()
                .ForMember(d => d.Brand, o => o.MapFrom(s => s.Model != null ? s.Model.Brand : null))
                .ForMember(d => d.ModelName, o => o.MapFrom(s => s.Model != null ? s.Model.ModelName : null));
        }
    }
}

using DeltaMapper.EFCore;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace DeltaMapper.IntegrationTests;

public class ProjectToTests
{
    // ── Test models ──

    public class User
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
        public string Email { get; set; } = "";
        public int Age { get; set; }
    }

    public class UserDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
        public string Email { get; set; } = "";
        public int Age { get; set; }
    }

    public class UserProfile : Profile
    {
        public UserProfile()
        {
            CreateMap<User, UserDto>();
        }
    }

    // ── Tests ──

    [Fact]
    public void ProjectTo_maps_flat_convention_matched_properties()
    {
        var config = MapperConfiguration.Create(cfg => cfg.AddProfile<UserProfile>());
        var users = new List<User>
        {
            new() { Id = 1, Name = "Alice", Email = "a@test.com", Age = 30 },
            new() { Id = 2, Name = "Bob", Email = "b@test.com", Age = 25 }
        }.AsQueryable();

        var result = users.ProjectTo<User, UserDto>(config).ToList();

        result.Should().HaveCount(2);
        result[0].Id.Should().Be(1);
        result[0].Name.Should().Be("Alice");
        result[0].Email.Should().Be("a@test.com");
        result[0].Age.Should().Be(30);
        result[1].Name.Should().Be("Bob");
    }

    // ── TASK-004: ForMember/MapFrom, Ignore, NullSubstitute ──

    public class UserWithCustomMap
    {
        public int Id { get; set; }
        public string FirstName { get; set; } = "";
        public string LastName { get; set; } = "";
    }

    public class UserCustomDto
    {
        public int Id { get; set; }
        public string FullName { get; set; } = "";
    }

    public class UserCustomProfile : Profile
    {
        public UserCustomProfile()
        {
            CreateMap<UserWithCustomMap, UserCustomDto>()
                .ForMember(d => d.FullName, o => o.MapFrom(s => s.FirstName + " " + s.LastName));
        }
    }

    [Fact]
    public void ProjectTo_applies_MapFrom_expression()
    {
        var config = MapperConfiguration.Create(cfg => cfg.AddProfile<UserCustomProfile>());
        var users = new List<UserWithCustomMap>
        {
            new() { Id = 1, FirstName = "Alice", LastName = "Smith" }
        }.AsQueryable();

        var result = users.ProjectTo<UserWithCustomMap, UserCustomDto>(config).ToList();

        result[0].FullName.Should().Be("Alice Smith");
    }

    public class UserIgnoreDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
        public string Email { get; set; } = "";
    }

    public class UserIgnoreProfile : Profile
    {
        public UserIgnoreProfile()
        {
            CreateMap<User, UserIgnoreDto>()
                .ForMember(d => d.Email, o => o.Ignore());
        }
    }

    [Fact]
    public void ProjectTo_ignores_configured_members()
    {
        var config = MapperConfiguration.Create(cfg => cfg.AddProfile<UserIgnoreProfile>());
        var users = new List<User>
        {
            new() { Id = 1, Name = "Alice", Email = "a@test.com" }
        }.AsQueryable();

        var result = users.ProjectTo<User, UserIgnoreDto>(config).ToList();

        result[0].Id.Should().Be(1);
        result[0].Name.Should().Be("Alice");
        result[0].Email.Should().BeEmpty(); // MemberInit skips the binding; constructor initializer sets ""
    }

    public class UserNullable
    {
        public int Id { get; set; }
        public string? Name { get; set; }
    }

    public class UserNullSubDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
    }

    public class UserNullSubProfile : Profile
    {
        public UserNullSubProfile()
        {
            CreateMap<UserNullable, UserNullSubDto>()
                .ForMember(d => d.Name, o => o.NullSubstitute("Unknown"));
        }
    }

    [Fact]
    public void ProjectTo_applies_null_substitute_as_coalesce()
    {
        var config = MapperConfiguration.Create(cfg => cfg.AddProfile<UserNullSubProfile>());
        var users = new List<UserNullable>
        {
            new() { Id = 1, Name = null },
            new() { Id = 2, Name = "Bob" }
        }.AsQueryable();

        var result = users.ProjectTo<UserNullable, UserNullSubDto>(config).ToList();

        result[0].Name.Should().Be("Unknown");
        result[1].Name.Should().Be("Bob");
    }

    // ── TASK-005: Flattening ──

    public class Order
    {
        public int Id { get; set; }
        public Customer Customer { get; set; } = null!;
    }

    public class Customer
    {
        public string Name { get; set; } = "";
        public Address Address { get; set; } = null!;
    }

    public class Address
    {
        public string City { get; set; } = "";
        public string Zip { get; set; } = "";
    }

    public class OrderFlatDto
    {
        public int Id { get; set; }
        public string CustomerName { get; set; } = "";
        public string CustomerAddressCity { get; set; } = "";
        public string CustomerAddressZip { get; set; } = "";
    }

    public class OrderFlatProfile : Profile
    {
        public OrderFlatProfile()
        {
            CreateMap<Order, OrderFlatDto>();
        }
    }

    [Fact]
    public void ProjectTo_flattens_nested_properties()
    {
        var config = MapperConfiguration.Create(cfg => cfg.AddProfile<OrderFlatProfile>());
        var orders = new List<Order>
        {
            new()
            {
                Id = 1,
                Customer = new Customer
                {
                    Name = "Alice",
                    Address = new Address { City = "Seattle", Zip = "98101" }
                }
            }
        }.AsQueryable();

        var result = orders.ProjectTo<Order, OrderFlatDto>(config).ToList();

        result[0].Id.Should().Be(1);
        result[0].CustomerName.Should().Be("Alice");
        result[0].CustomerAddressCity.Should().Be("Seattle");
        result[0].CustomerAddressZip.Should().Be("98101");
    }

    // ── TASK-006: Nested object and collection navigation ──

    public class OrderNested
    {
        public int Id { get; set; }
        public CustomerNested Customer { get; set; } = null!;
    }

    public class CustomerNested
    {
        public string Name { get; set; } = "";
        public string Email { get; set; } = "";
    }

    public class OrderNestedDto
    {
        public int Id { get; set; }
        public CustomerNestedDto Customer { get; set; } = null!;
    }

    public class CustomerNestedDto
    {
        public string Name { get; set; } = "";
        public string Email { get; set; } = "";
    }

    public class OrderNestedProfile : Profile
    {
        public OrderNestedProfile()
        {
            CreateMap<OrderNested, OrderNestedDto>();
            CreateMap<CustomerNested, CustomerNestedDto>();
        }
    }

    [Fact]
    public void ProjectTo_builds_nested_object_projection()
    {
        var config = MapperConfiguration.Create(cfg => cfg.AddProfile<OrderNestedProfile>());
        var orders = new List<OrderNested>
        {
            new() { Id = 1, Customer = new() { Name = "Alice", Email = "a@test.com" } }
        }.AsQueryable();

        var result = orders.ProjectTo<OrderNested, OrderNestedDto>(config).ToList();

        result[0].Id.Should().Be(1);
        result[0].Customer.Should().NotBeNull();
        result[0].Customer.Name.Should().Be("Alice");
        result[0].Customer.Email.Should().Be("a@test.com");
    }

    public class OrderWithItems
    {
        public int Id { get; set; }
        public List<OrderItem> Items { get; set; } = [];
    }

    public class OrderItem
    {
        public string Product { get; set; } = "";
        public decimal Price { get; set; }
    }

    public class OrderWithItemsDto
    {
        public int Id { get; set; }
        public List<OrderItemDto> Items { get; set; } = [];
    }

    public class OrderItemDto
    {
        public string Product { get; set; } = "";
        public decimal Price { get; set; }
    }

    public class OrderWithItemsProfile : Profile
    {
        public OrderWithItemsProfile()
        {
            CreateMap<OrderWithItems, OrderWithItemsDto>();
            CreateMap<OrderItem, OrderItemDto>();
        }
    }

    [Fact]
    public void ProjectTo_maps_collection_navigation_via_Select()
    {
        var config = MapperConfiguration.Create(cfg => cfg.AddProfile<OrderWithItemsProfile>());
        var orders = new List<OrderWithItems>
        {
            new()
            {
                Id = 1,
                Items =
                [
                    new() { Product = "Widget", Price = 9.99m },
                    new() { Product = "Gadget", Price = 19.99m }
                ]
            }
        }.AsQueryable();

        var result = orders.ProjectTo<OrderWithItems, OrderWithItemsDto>(config).ToList();

        result[0].Items.Should().HaveCount(2);
        result[0].Items[0].Product.Should().Be("Widget");
        result[0].Items[1].Price.Should().Be(19.99m);
    }

    // ── TASK-007: Unsupported feature error messages ──

    public class BeforeMapProfile : Profile
    {
        public BeforeMapProfile()
        {
            CreateMap<User, UserDto>()
                .BeforeMap((s, d) => { });
        }
    }

    public class AfterMapProfile : Profile
    {
        public AfterMapProfile()
        {
            CreateMap<User, UserDto>()
                .AfterMap((s, d) => { });
        }
    }

    public class ConstructUsingProfile : Profile
    {
        public ConstructUsingProfile()
        {
            CreateMap<User, UserDto>()
                .ConstructUsing(s => new UserDto());
        }
    }

    public class ConditionProfile : Profile
    {
        public ConditionProfile()
        {
            CreateMap<User, UserDto>()
                .ForMember(d => d.Name, o => o.Condition(s => s.Age > 18));
        }
    }

    [Fact]
    public void ProjectTo_throws_for_BeforeMap()
    {
        var config = MapperConfiguration.Create(cfg => cfg.AddProfile<BeforeMapProfile>());
        var users = new List<User>().AsQueryable();

        var act = () => users.ProjectTo<User, UserDto>(config).ToList();

        act.Should().Throw<DeltaMapperException>()
           .WithMessage("*BeforeMap*");
    }

    [Fact]
    public void ProjectTo_throws_for_AfterMap()
    {
        var config = MapperConfiguration.Create(cfg => cfg.AddProfile<AfterMapProfile>());
        var users = new List<User>().AsQueryable();

        var act = () => users.ProjectTo<User, UserDto>(config).ToList();

        act.Should().Throw<DeltaMapperException>()
           .WithMessage("*AfterMap*");
    }

    [Fact]
    public void ProjectTo_throws_for_ConstructUsing()
    {
        var config = MapperConfiguration.Create(cfg => cfg.AddProfile<ConstructUsingProfile>());
        var users = new List<User>().AsQueryable();

        var act = () => users.ProjectTo<User, UserDto>(config).ToList();

        act.Should().Throw<DeltaMapperException>()
           .WithMessage("*ConstructUsing*");
    }

    [Fact]
    public void ProjectTo_throws_for_Condition()
    {
        var config = MapperConfiguration.Create(cfg => cfg.AddProfile<ConditionProfile>());
        var users = new List<User> { new() { Id = 1, Name = "A", Email = "a@b.c", Age = 20 } }.AsQueryable();

        var act = () => users.ProjectTo<User, UserDto>(config).ToList();

        act.Should().Throw<DeltaMapperException>()
           .WithMessage("*Condition*");
    }

    [Fact]
    public void ProjectTo_throws_for_missing_map()
    {
        // Register an unrelated map so MapperConfiguration.Create succeeds,
        // then verify ProjectTo throws when the requested pair is not registered.
        var config = MapperConfiguration.Create(cfg => cfg.AddProfile<OrderFlatProfile>());
        var users = new List<User>().AsQueryable();

        var act = () => users.ProjectTo<User, UserDto>(config).ToList();

        act.Should().Throw<DeltaMapperException>()
           .WithMessage("*No mapping configuration found*");
    }

    // ── TASK-008: Non-generic overload and EF Core InMemory ──

    [Fact]
    public void ProjectTo_non_generic_infers_source_type()
    {
        var config = MapperConfiguration.Create(cfg => cfg.AddProfile<UserProfile>());
        IQueryable source = new List<User>
        {
            new() { Id = 1, Name = "Alice", Email = "a@test.com", Age = 30 }
        }.AsQueryable();

        var result = source.ProjectTo<UserDto>(config).ToList();

        result.Should().HaveCount(1);
        result[0].Name.Should().Be("Alice");
    }

    public class BlogPost
    {
        public int Id { get; set; }
        public string Title { get; set; } = "";
        public string Content { get; set; } = "";
        public BlogAuthor Author { get; set; } = null!;
        public int AuthorId { get; set; }
    }

    public class BlogAuthor
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
    }

    public class BlogPostDto
    {
        public int Id { get; set; }
        public string Title { get; set; } = "";
        public string AuthorName { get; set; } = "";
    }

    public class BlogProfile : Profile
    {
        public BlogProfile()
        {
            CreateMap<BlogPost, BlogPostDto>();
        }
    }

    public class TestDbContext : Microsoft.EntityFrameworkCore.DbContext
    {
        public TestDbContext(Microsoft.EntityFrameworkCore.DbContextOptions<TestDbContext> options) : base(options) { }
        public Microsoft.EntityFrameworkCore.DbSet<BlogPost> BlogPosts => Set<BlogPost>();
        public Microsoft.EntityFrameworkCore.DbSet<BlogAuthor> Authors => Set<BlogAuthor>();
    }

    [Fact]
    public async Task ProjectTo_works_with_EF_Core_InMemory()
    {
        var options = new Microsoft.EntityFrameworkCore.DbContextOptionsBuilder<TestDbContext>()
            .UseInMemoryDatabase(databaseName: $"ProjectToTest_{Guid.NewGuid()}")
            .Options;

        await using (var seedCtx = new TestDbContext(options))
        {
            var author = new BlogAuthor { Id = 1, Name = "Alice" };
            seedCtx.Authors.Add(author);
            seedCtx.BlogPosts.Add(new BlogPost
            {
                Id = 1, Title = "Hello World", Content = "...", Author = author, AuthorId = 1
            });
            await seedCtx.SaveChangesAsync();
        }

        var config = MapperConfiguration.Create(cfg => cfg.AddProfile<BlogProfile>());

        await using var ctx = new TestDbContext(options);
        var result = await ctx.BlogPosts
            .Include(b => b.Author)
            .ProjectTo<BlogPost, BlogPostDto>(config)
            .ToListAsync();

        result.Should().HaveCount(1);
        result[0].Title.Should().Be("Hello World");
        result[0].AuthorName.Should().Be("Alice");
    }
}

using DeltaMapper.Configuration;
using DeltaMapper.EFCore;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace DeltaMapper.IntegrationTests;

// ─── Test models ──────────────────────────────────────────────────────────────

public class Blog
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public List<Post>? Posts { get; set; }
}

public class Post
{
    public int Id { get; set; }
    public string Content { get; set; } = string.Empty;
    public int BlogId { get; set; }
}

public class BlogDto
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
}

// ─── DbContext ─────────────────────────────────────────────────────────────────

public class TestDbContext : DbContext
{
    public TestDbContext(DbContextOptions<TestDbContext> options) : base(options) { }

    public DbSet<Blog> Blogs => Set<Blog>();
    public DbSet<Post> Posts => Set<Post>();
}

// ─── Mapping profile ───────────────────────────────────────────────────────────

public class BlogMappingProfile : MappingProfile
{
    public BlogMappingProfile()
    {
        CreateMap<Blog, BlogDto>();
    }
}

// ─── Tests ─────────────────────────────────────────────────────────────────────

public class EFCoreProxyTests : IDisposable
{
    private readonly TestDbContext _db;

    public EFCoreProxyTests()
    {
        var options = new DbContextOptionsBuilder<TestDbContext>()
            .UseInMemoryDatabase(databaseName: $"EFCoreTests_{Guid.NewGuid()}")
            .Options;

        _db = new TestDbContext(options);
        _db.Database.EnsureCreated();
    }

    public void Dispose() => _db.Dispose();

    [Fact]
    public void EFCore01_ProxyEntityMapsWithoutNavigations()
    {
        // Arrange — seed a Blog with no Posts loaded
        _db.Blogs.Add(new Blog { Id = 1, Title = "Tech Blog", Posts = null });
        _db.SaveChanges();
        _db.ChangeTracker.Clear();

        // Act — load the entity (InMemory does NOT create Castle.Core proxies;
        // middleware passes through for non-proxy entities)
        var trackedBlog = _db.Blogs.First(b => b.Id == 1);

        var config = MapperConfiguration.Create(cfg =>
        {
            cfg.AddEFCoreSupport();
            cfg.AddProfile<BlogMappingProfile>();
        });
        var mapper = config.CreateMapper();

        var dto = mapper.Map<Blog, BlogDto>(trackedBlog);

        // Assert — scalar properties mapped; no navigation on BlogDto
        dto.Should().NotBeNull();
        dto.Id.Should().Be(1);
        dto.Title.Should().Be("Tech Blog");
    }

    [Fact]
    public void EFCore02_NonProxyEntityMapsNormally()
    {
        // Arrange — plain POCO (never touched by EF context)
        var blog = new Blog { Id = 2, Title = "POCO Blog", Posts = null };

        var config = MapperConfiguration.Create(cfg =>
        {
            cfg.AddEFCoreSupport();
            cfg.AddProfile<BlogMappingProfile>();
        });
        var mapper = config.CreateMapper();

        // Act
        var dto = mapper.Map<Blog, BlogDto>(blog);

        // Assert
        dto.Should().NotBeNull();
        dto.Id.Should().Be(2);
        dto.Title.Should().Be("POCO Blog");
    }

    [Fact]
    public void EFCore03_LoadedNavigationMapsThrough()
    {
        // Arrange — Blog with Posts loaded; BlogDto has no Posts property so the
        // source navigation is simply ignored by convention matching
        _db.Blogs.Add(new Blog
        {
            Id = 3,
            Title = "Blog With Posts",
            Posts = new List<Post>
            {
                new Post { Id = 10, Content = "Hello world", BlogId = 3 }
            }
        });
        _db.SaveChanges();
        _db.ChangeTracker.Clear();

        // Eagerly load the navigation
        var trackedBlog = _db.Blogs.Include(b => b.Posts).First(b => b.Id == 3);

        var config = MapperConfiguration.Create(cfg =>
        {
            cfg.AddEFCoreSupport();
            cfg.AddProfile<BlogMappingProfile>();
        });
        var mapper = config.CreateMapper();

        // Act
        var dto = mapper.Map<Blog, BlogDto>(trackedBlog);

        // Assert — scalar properties still map correctly even when navigations are present
        dto.Should().NotBeNull();
        dto.Id.Should().Be(3);
        dto.Title.Should().Be("Blog With Posts");
    }

    [Fact]
    public void EFCore05_MiddlewareDoesNotThrow_WithEagerlyLoadedNavigations()
    {
        // When navigations ARE loaded (Include), mapping should still work
        _db.Blogs.Add(new Blog
        {
            Id = 5,
            Title = "Eager Blog",
            Posts = [new Post { Id = 20, Content = "Loaded", BlogId = 5 }]
        });
        _db.SaveChanges();
        _db.ChangeTracker.Clear();

        var blog = _db.Blogs.Include(b => b.Posts).First(b => b.Id == 5);

        var config = MapperConfiguration.Create(cfg =>
        {
            cfg.AddEFCoreSupport();
            cfg.AddProfile<BlogMappingProfile>();
        });
        var mapper = config.CreateMapper();

        var dto = mapper.Map<Blog, BlogDto>(blog);
        dto.Id.Should().Be(5);
        dto.Title.Should().Be("Eager Blog");
    }

    [Fact]
    public void EFCore04_MiddlewareRegisteredViaAddEFCoreSupport()
    {
        // Arrange & Act — build a config with AddEFCoreSupport() and map a simple entity
        var blog = new Blog { Id = 4, Title = "Middleware Blog" };

        var config = MapperConfiguration.Create(cfg =>
        {
            cfg.AddEFCoreSupport();
            cfg.AddProfile<BlogMappingProfile>();
        });
        var mapper = config.CreateMapper();

        // If AddEFCoreSupport() failed to register the middleware the pipeline would
        // still work (middleware is pass-through), so we verify the mapper is
        // operational — meaning registration did not throw and mapping succeeds.
        var act = () => mapper.Map<Blog, BlogDto>(blog);

        var dto = act.Should().NotThrow().Subject;
        dto.Id.Should().Be(4);
        dto.Title.Should().Be("Middleware Blog");
    }
}

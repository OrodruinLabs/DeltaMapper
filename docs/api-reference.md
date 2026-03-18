# API Reference

## `MapperConfiguration`

```csharp
MapperConfiguration config = MapperConfiguration.Create(cfg =>
{
    cfg.AddProfile<UserProfile>();          // add by generic type
    cfg.AddProfile(new OrderProfile());     // add existing instance
    cfg.Use<MyLoggingMiddleware>();         // add pipeline middleware
});

IMapper mapper = config.CreateMapper();
```

All compilation happens inside `Create()`. The internal registry is a `FrozenDictionary` — read access after construction has no locking overhead.

## `MappingProfile`

Subclass `MappingProfile` and configure maps in the constructor.

```csharp
public class OrderProfile : MappingProfile
{
    public OrderProfile()
    {
        CreateMap<Order, OrderDto>()
            .ForMember(d => d.Total,    o => o.MapFrom(s => s.Lines.Sum(l => l.Amount)))
            .ForMember(d => d.Secret,   o => o.Ignore())
            .ForMember(d => d.Customer, o => o.NullSubstitute("Anonymous"))
            .BeforeMap((src, dst) => dst.MappedAt = DateTimeOffset.UtcNow)
            .AfterMap((src, dst)  => dst.Validate())
            .ReverseMap();
    }
}
```

| Fluent method | Effect |
|---|---|
| `ForMember(dst => dst.Prop, o => o.MapFrom(src => ...))` | Custom value resolver |
| `ForMember(dst => dst.Prop, o => o.Ignore())` | Skip this destination member |
| `ForMember(dst => dst.Prop, o => o.NullSubstitute(value))` | Use `value` when source is null |
| `BeforeMap((src, dst) => ...)` | Hook runs before property assignment |
| `AfterMap((src, dst) => ...)` | Hook runs after property assignment |
| `ReverseMap()` | Registers convention-matched reverse map (`TDst -> TSrc`) |

## `IMapper`

```csharp
TDestination Map<TDestination>(object source);
TDestination Map<TSource, TDestination>(TSource source);
TDestination Map<TSource, TDestination>(TSource source, TDestination destination);
IReadOnlyList<TDestination> MapList<TSource, TDestination>(IEnumerable<TSource> source);
object Map(object source, Type sourceType, Type destinationType);
MappingDiff<TDestination> Patch<TSource, TDestination>(TSource source, TDestination destination);
```

## Convention Matching

Properties are matched by name (case-insensitive) without any configuration:

1. Same name + same type (or assignable) — direct assign
2. Same name + safe numeric widening (e.g., `int` to `long`) — `Convert.ChangeType`
3. Same name + `IEnumerable<T>` on both sides — map each element, produce `List<T>` or `T[]`
4. Same name + complex object type — recursive map lookup

## Records and Init-Only Properties

DeltaMapper detects `init`-only setters via `IsExternalInit` modreq and routes those types through constructor injection automatically.

```csharp
public record UserDto(int Id, string Email, string FullName);

// No special configuration required — constructor injection is automatic
var dto = mapper.Map<User, UserDto>(user);
```

## Middleware Pipeline

Implement `IMappingMiddleware` to intercept every mapping call.

```csharp
public sealed class LoggingMiddleware : IMappingMiddleware
{
    public object Map(object source, Type destType, MapperContext ctx, Func<object> next)
    {
        Console.WriteLine($"Mapping {source.GetType().Name} -> {destType.Name}");
        var result = next();
        Console.WriteLine("Done");
        return result;
    }
}

cfg.Use<LoggingMiddleware>();
```

When no middleware is registered the core delegate is invoked directly with zero overhead.

## DI Integration (ASP.NET Core / Generic Host)

```csharp
builder.Services.AddDeltaMapper(cfg =>
{
    cfg.AddProfile<UserProfile>();
    cfg.AddProfile<OrderProfile>();
});

public class UserService(IMapper mapper)
{
    public UserDto GetUser(User user) => mapper.Map<User, UserDto>(user);
}
```

`AddDeltaMapper` registers both `MapperConfiguration` and `IMapper` as singletons.

## Error Handling

When no mapping is registered `DeltaMapperException` is thrown with a clear, actionable message:

```text
No mapping registered from 'User' to 'UserDto'.
Register a mapping in a MappingProfile using CreateMap<User, UserDto>().
```

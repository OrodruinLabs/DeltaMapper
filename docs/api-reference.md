# API Reference

## `MapperConfiguration`

```csharp
MapperConfiguration config = MapperConfiguration.Create(cfg =>
{
    cfg.AddProfile<UserProfile>();          // add by generic type
    cfg.AddProfile(new OrderProfile());     // add existing instance
    cfg.AddProfilesFromAssembly(assembly);  // scan assembly for profiles
    cfg.AddProfilesFromAssemblyContaining<UserProfile>(); // scan assembly of T
    cfg.CreateTypeConverter<string, DateTime>(s => DateTime.Parse(s)); // global converter
    cfg.Use<MyLoggingMiddleware>();         // add pipeline middleware
});

IMapper mapper = config.CreateMapper();
```

All compilation happens inside `Create()`. The internal registry is a `FrozenDictionary` — read access after construction has no locking overhead.

## `Profile`

Subclass `Profile` and configure maps in the constructor.

```csharp
public class OrderProfile : Profile
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

### Nested Type Resolution in MapFrom

When `MapFrom` returns a type that differs from the destination property type and a registered type map exists, DeltaMapper auto-resolves it:

```csharp
CreateMap<CustomerEntity, CustomerDto>();
CreateMap<OrderEntity, OrderDto>()
    .ForMember(d => d.Customer, o => o.MapFrom(s => s.Customer));
    // CustomerEntity → CustomerDto is resolved automatically
```

If `MapFrom` returns the destination type directly (inline construction), no double-mapping occurs.

## ConstructUsing

Specify a custom factory to construct the destination object. Useful for DDD entities with private constructors and static factory methods:

```csharp
CreateMap<OrderSource, Money>()
    .ConstructUsing(src => Money.Create(src.Total, src.Currency));
```

The factory runs first, then convention matching and `ForMember` overrides apply on top. Read-only properties set by the factory are preserved.

## `IMapper`

```csharp
TDestination Map<TDestination>(object source);
TDestination Map<TSource, TDestination>(TSource source);
TDestination Map<TSource, TDestination>(TSource source, TDestination destination);
List<TDestination> Map<TSource, TDestination>(IEnumerable<TSource> source);
object Map(object source, Type sourceType, Type destinationType);
MappingDiff<TDestination> Patch<TSource, TDestination>(TSource source, TDestination destination);
```

The single-generic overload also supports collections. When `TDestination` is `IEnumerable<X>`, `List<X>`, `X[]`, or `IReadOnlyList<X>` and the source implements `IEnumerable<Y>` with a registered `Y → X` map, elements are mapped automatically:

```csharp
var dtos = mapper.Map<IEnumerable<StudentDto>>(students);  // works!
var list = mapper.Map<List<StudentDto>>(students);          // works!
var array = mapper.Map<StudentDto[]>(students);             // works!
```

## Convention Matching

Properties are matched by name (case-insensitive) without any configuration:

1. Same name + same type (or assignable) — direct assign
2. Same name + safe numeric widening (e.g., `int` to `long`) — `Convert.ChangeType`
3. Same name + `IEnumerable<T>` on both sides — map each element, produce `List<T>` or `T[]`
4. Same name + complex object type — recursive map lookup
5. No direct match — attempt flattening (source) or unflattening (destination), then skip

### Nullable Value Type Coercion

When a source property is `Nullable<T>` and the destination is `T`, DeltaMapper assigns `default(T)` when the source is null:

- `Guid?` → `Guid`: assigns `Guid.Empty`
- `int?` → `int`: assigns `0`
- `DateTime?` → `DateTime`: assigns `DateTime.MinValue`

## Flattening

When no direct source property matches a destination property name, DeltaMapper attempts to resolve it by walking the source's object graph. The destination property name is parsed as a concatenation of property names along the path.

```csharp
public class Order
{
    public int Id { get; set; }
    public Customer? Customer { get; set; }
}
public class Customer { public string? Name { get; set; } }

// Destination property CustomerName is resolved from Order.Customer.Name
public class OrderDto
{
    public int Id { get; set; }
    public string? CustomerName { get; set; }
}

// No configuration required
CreateMap<Order, OrderDto>();
```

Multi-level chains are resolved recursively (`Order.Customer.Address.Zip` → `CustomerAddressZip`). If any intermediate object in the chain is null, the destination property receives null and no exception is thrown. The flattened getter is compiled into an expression delegate at build time — there is no reflection overhead during mapping.

## Unflattening

When a destination property is a complex type and no direct source property matches its name, DeltaMapper attempts to unflatten by treating the destination property name as a prefix. Source properties whose names start with that prefix (case-insensitive) are assigned to the corresponding sub-properties of a new destination object.

```csharp
// Flat source
public class OrderFlatDto
{
    public int Id { get; set; }
    public string? CustomerName  { get; set; }
    public string? CustomerEmail { get; set; }
}

// Nested destination — Customer is reconstructed from CustomerName and CustomerEmail
public class Order
{
    public int Id { get; set; }
    public Customer? Customer { get; set; }
}
public class Customer
{
    public string? Name  { get; set; }
    public string? Email { get; set; }
}

// No configuration required
CreateMap<OrderFlatDto, Order>();
```

Flattening and unflattening can be combined in the same map when some properties are flat and others are complex.

## Assembly Scanning

Instead of listing profiles one by one, scan an entire assembly:

```csharp
// By Assembly reference
cfg.AddProfilesFromAssembly(typeof(UserProfile).Assembly);

// By marker type (resolves assembly from typeof(T))
cfg.AddProfilesFromAssemblyContaining<UserProfile>();
```

The scanner discovers all concrete, non-generic `Profile` subclasses that have a public parameterless constructor. Abstract profiles and profiles requiring constructor arguments are silently skipped. Scanning and explicit `AddProfile<T>()` calls can be combined.

To also scan assemblies referenced by the given assembly, pass `includeReferencedAssemblies: true`:

```csharp
cfg.AddProfilesFromAssembly(typeof(Startup).Assembly, includeReferencedAssemblies: true);
cfg.AddProfilesFromAssemblyContaining<Startup>(includeReferencedAssemblies: true);
```

## Type Converters

Register a global type converter once and it applies to every property pair of those types across all maps:

```csharp
MapperConfiguration.Create(cfg =>
{
    cfg.CreateTypeConverter<string, DateTime>(s => DateTime.Parse(s));
    cfg.CreateTypeConverter<int, string>(i => i.ToString("D6"));
    cfg.AddProfile<OrderProfile>();
});
```

The signature is:

```csharp
MapperConfigurationBuilder CreateTypeConverter<TSource, TDest>(Func<TSource, TDest> converter);
```

A converter is only invoked when the source and destination property types differ. Same-type properties continue to use the direct convention assignment path. When the source value is null the converter is not called; the destination receives null or default.

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
    cfg.AddProfilesFromAssemblyContaining<UserProfile>();
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
Register a mapping in a Profile using CreateMap<User, UserDto>().
```

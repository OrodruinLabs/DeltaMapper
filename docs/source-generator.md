# Source Generator

## Overview

`DeltaMapper.SourceGen` is a Roslyn `IIncrementalGenerator` that emits direct property assignment code at build time. No reflection at runtime.

## Install

```bash
dotnet add package DeltaMapper.SourceGen
```

## Usage

Add `[GenerateMap]` to a `partial` class:

```csharp
[GenerateMap(typeof(User), typeof(UserDto))]
public partial class UserProfile { }
```

The generator emits:
- A private `Map_User_To_UserDto(src, dst)` method with direct assignments
- A private `Create_User_To_UserDto(src)` factory (object initializer for flat types, two-step for nested/collections)
- A public `MapUserToUserDto(src)` method for zero-overhead direct calls
- A `[ModuleInitializer]` that registers delegates in `GeneratedMapRegistry`

## Two Performance Tiers

| Call Pattern | Mean (flat) | Use When |
|---|---:|---|
| `UserProfile.MapUserToUserDto(src)` | **7.2 ns** | Hot path, no middleware needed |
| `mapper.Map<User, UserDto>(src)` | **24 ns** | DI, middleware, hooks, Patch |

The IMapper path automatically detects and uses source-gen delegates when available, unless an explicit runtime profile is registered for the same type pair (compiled maps take precedence).

## Attributes

Attributes let you customize a source-generated map without writing a runtime Profile or calling `ForMember`. Apply them to the same `partial` class as `[GenerateMap]`.

### `[IgnoreMember]`

Exclude a destination member from the generated map entirely.

```csharp
[GenerateMap(typeof(User), typeof(UserDto))]
[IgnoreMember(typeof(User), typeof(UserDto), nameof(UserDto.InternalId))]
public partial class UserMappingProfile { }
```

Signature:

```csharp
[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
public sealed class IgnoreMemberAttribute : Attribute
{
    public IgnoreMemberAttribute(Type sourceType, Type destinationType, string memberName) { }
}
```

### `[MapMember]`

Map a source member to a differently named destination member.

```csharp
[GenerateMap(typeof(User), typeof(UserDto))]
[MapMember(typeof(User), typeof(UserDto), nameof(UserDto.FullName), nameof(User.Name))]
public partial class UserMappingProfile { }
```

Signature:

```csharp
[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
public sealed class MapMemberAttribute : Attribute
{
    public MapMemberAttribute(
        Type sourceType,
        Type destinationType,
        string destinationMember,
        string sourceMember) { }
}
```

### `[NullSubstitute]`

Use a substitute value when the source member is null.

```csharp
[GenerateMap(typeof(User), typeof(UserDto))]
[NullSubstitute(typeof(User), typeof(UserDto), nameof(UserDto.DisplayName), "Anonymous")]
public partial class UserMappingProfile { }
```

Signature:

```csharp
[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
public sealed class NullSubstituteAttribute : Attribute
{
    public NullSubstituteAttribute(
        Type sourceType,
        Type destinationType,
        string memberName,
        object value) { }
}
```

### Multi-pair profiles

Because each attribute takes explicit `(Type sourceType, Type destinationType, ...)` parameters, a single `partial` class can carry customizations for multiple `[GenerateMap]` pairs without ambiguity:

```csharp
[GenerateMap(typeof(User), typeof(UserDto))]
[GenerateMap(typeof(Order), typeof(OrderDto))]
[IgnoreMember(typeof(User), typeof(UserDto), nameof(UserDto.InternalId))]
[MapMember(typeof(User), typeof(UserDto), nameof(UserDto.FullName), nameof(User.Name))]
[NullSubstitute(typeof(Order), typeof(OrderDto), nameof(OrderDto.Notes), "N/A")]
public partial class AppMappingProfile { }
```

## Analyzer Diagnostics

| Code | Severity | Description |
|---|---|---|
| DM001 | Warning | Unmapped destination property |
| DM002 | Error | Type argument could not be resolved |
| DM003 | Warning | Attribute references a property that does not exist on the declared type |
| DM004 | Warning | `[MapMember]` source and destination property types are incompatible |

## How It Works

1. The generator scans for `[GenerateMap(typeof(Src), typeof(Dst))]` attributes
2. For each pair, it matches readable source properties to writable destination properties (case-insensitive)
3. It applies `[IgnoreMember]`, `[MapMember]`, and `[NullSubstitute]` overrides from sibling attributes on the same class
4. It emits assignment code handling simple types, nested objects, and collections
5. A `[ModuleInitializer]` static method registers all delegates in `GeneratedMapRegistry` before `Main()` runs
6. `Mapper.Map<>()` checks the registry and uses the generated factory on the fast path

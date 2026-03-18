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

## Analyzer Diagnostics

| Code | Severity | Description |
|---|---|---|
| DM001 | Warning | Unmapped destination property |
| DM002 | Error | Type argument could not be resolved |

## How It Works

1. The generator scans for `[GenerateMap(typeof(Src), typeof(Dst))]` attributes
2. For each pair, it matches readable source properties to writable destination properties (case-insensitive)
3. It emits assignment code handling simple types, nested objects, and collections
4. A `[ModuleInitializer]` static method registers all delegates in `GeneratedMapRegistry` before `Main()` runs
5. `Mapper.Map<>()` checks the registry and uses the generated factory on the fast path

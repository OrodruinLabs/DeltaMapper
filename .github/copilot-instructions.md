# DeltaMapper — Copilot Review Instructions

## Architecture

4 NuGet packages:
- **DeltaMapper.Core** (net8.0/9.0/10.0) — Runtime mapper, expression-compiled delegates, FrozenDictionary registry, Profile API, MappingDiff/Patch, middleware, DI
- **DeltaMapper.SourceGen** (netstandard2.0) — Roslyn IIncrementalGenerator: [GenerateMap], [IgnoreMember], [NullSubstitute], [MapMember]. DM001-DM004 diagnostics
- **DeltaMapper.EFCore** (net8.0/9.0/10.0) — ProjectTo IQueryable projections + proxy-aware middleware
- **DeltaMapper.OpenTelemetry** (net8.0/9.0/10.0) — Activity span tracing

Dependencies: EFCore/OpenTelemetry depend on Core. SourceGen is independent. Core exposes internals to EFCore, UnitTests, IntegrationTests, SourceGen.Tests via InternalsVisibleTo.

Two-tier execution: GeneratedMapRegistry factory delegates (~7ns) vs compiled FrozenDictionary delegates (~24ns). Compiled maps take precedence so BeforeMap/AfterMap hooks are honored.

## Source Generator Constraints

SourceGen targets netstandard2.0 — block-scoped namespaces required (`namespace X { }`), no C# 12+ features. Generated attribute source text must also use block-scoped namespaces.

- `SymbolEqualityComparer.Default` for all ISymbol comparisons
- `CultureInfo.InvariantCulture` for all numeric ToString() in emitted code
- Diagnostic IDs: DM001 (unmapped prop), DM002 (type not found), DM003 (non-existent property ref), DM004 (type incompatible)

## Conventions

- `StringComparison.OrdinalIgnoreCase` for ALL property name matching — no exceptions
- Nullable: use `T?` for optional, avoid `null!`. Unwrap `Nullable<T>` in type classification
- Internal types: `sealed` unless inheritance intended. `public` only for API surface
- Expression trees: verify type compat before `Expression.Bind`/`Coalesce`/`Convert`
- Tests: xUnit + FluentAssertions, `Method_Scenario_Expected` naming, multi-target net8/9/10

## Do NOT Flag

- `Enumerable.Select` inside expression trees for EF Core — EF translates both Enumerable and Queryable methods
- `int.ToString()`/`long.ToString()` without culture — integers are culture-invariant
- `Expression.Constant(boxedValue, nullableType)` with non-nullable value — .NET handles wrapping
- Reflection `GetMethods().First(...)` for Queryable.Select — filtered by param type to disambiguate
- DM001 warnings in `tests/DeltaMapper.Benchmarks/` — intentional test models
- `_typeMaps` retaining TypeMapConfiguration alongside ValidationSnapshot — intentional for ProjectTo
- Block-scoped namespaces in SourceGen — required by netstandard2.0

## Priority Focus

- **Thread safety**: new static state must use ConcurrentDictionary. MapperConfiguration is immutable post-Build()
- **Expression type safety**: Coalesce needs nullable left operand. Bind needs matching types. Missing Convert is a common bug
- **Silent failures**: code paths that skip without diagnostic when user config is invalid = bug
- **Cycle detection**: recursive BuildMemberBindings/BuildAssignmentLines must guard self-referential types
- **Nullable + `??`**: emitting `src.IntProp ?? 0` for non-nullable int = uncompilable

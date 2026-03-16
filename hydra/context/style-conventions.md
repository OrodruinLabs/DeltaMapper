# Style Conventions

## Naming Conventions
- Variables: camelCase — docs/DELTAMAP_PLAN.md:591 (`srcParam`, `castDst`, `valParam`)
- Functions/Methods: PascalCase — docs/DELTAMAP_PLAN.md:107-111 (`Map`, `MapList`, `CreateMap`)
- Properties: PascalCase — docs/DELTAMAP_PLAN.md:273 (`PropertyName`, `HasChanges`)
- Classes: PascalCase — docs/DELTAMAP_PLAN.md:171 (`Mapper`, `MapperConfiguration`, `MappingProfile`)
- Interfaces: IPascalCase — docs/DELTAMAP_PLAN.md:105 (`IMapper`, `IMappingMiddleware`)
- Files: PascalCase matching class name — docs/DELTAMAP_PLAN.md:27-41
- Directories: PascalCase for projects, PascalCase for subfolders — docs/DELTAMAP_PLAN.md:37-41 (`Middleware/`, `Exceptions/`)
- Generic parameters: T-prefixed PascalCase — docs/DELTAMAP_PLAN.md:107 (`TSource`, `TDestination`, `TDst`)

## Directory Structure
- Pattern: Layer-based at top level (src/tests/samples/docs), project-based within src/
- Evidence: docs/DELTAMAP_PLAN.md:23-63 — standard .NET solution layout

## Import Style
- C# `using` statements (implicit usings enabled): docs/DELTAMAP_PLAN.md:90
- Project references between solution projects

## Error Handling
- Pattern: Custom exception type with actionable messages
- Evidence: docs/DELTAMAP_PLAN.md:41 — `DeltaMapperException.cs`, docs/DELTAMAP_PLAN.md:665 — "Exceptions always include the source/destination type names and a resolution hint"

## Logging
- Logger: None — library does not log
- Observability via OpenTelemetry Activity spans (opt-in): docs/DELTAMAP_PLAN.md:462-486

## Comments
- Style: XML doc comments on all public APIs
- Evidence: docs/DELTAMAP_PLAN.md:663 — "Every public API has XML doc comments"

## Linter/Formatter
- None detected yet
- Recommend: `.editorconfig` with standard C# rules, `dotnet format`

## Git Conventions
- No commits exist yet (greenfield)
- Plan implies semantic versioning: docs/DELTAMAP_PLAN.md:95 — `<Version>0.1.0</Version>`
- Plan implies `v*` tag-based releases: docs/DELTAMAP_PLAN.md:643

## Code Standards (from plan — docs/DELTAMAP_PLAN.md:660-667)
- `nullable enable` everywhere
- No `dynamic`, no `object` casts outside core mapping engine internals
- Every public API has XML doc comments
- No reflection at call time — reflection only during `MapperConfiguration.Create()`
- Exceptions always include source/destination type names and resolution hint
- Tests use FluentAssertions for readable assertions
- Benchmarks use BenchmarkDotNet with MemoryDiagnoser

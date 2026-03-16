# Test Strategy

## Test Framework
- xunit: docs/DELTAMAP_PLAN.md:73 — "Test projects: `xunit`, `FluentAssertions`, `BenchmarkDotNet`"
- FluentAssertions: docs/DELTAMAP_PLAN.md:73 — assertion library for readable tests

## Test Location
- Separate `tests/` directory: docs/DELTAMAP_PLAN.md:50-53
  - `tests/DeltaMapper.UnitTests/`
  - `tests/DeltaMapper.IntegrationTests/`
  - `tests/DeltaMapper.Benchmarks/`

## Test Types Present
- [x] Unit — planned in `tests/DeltaMapper.UnitTests/` (docs/DELTAMAP_PLAN.md:234-249)
- [x] Integration — planned in `tests/DeltaMapper.IntegrationTests/` (docs/DELTAMAP_PLAN.md:52)
- [ ] E2E
- [ ] Snapshot
- [ ] Property-based
- [x] Benchmark — planned in `tests/DeltaMapper.Benchmarks/` (docs/DELTAMAP_PLAN.md:510-523)

## Coverage Tool
- None detected — recommend `coverlet` with `dotnet test --collect:"XPlat Code Coverage"`

## Test Commands
- `dotnet test`: runs all tests across the solution
- `dotnet test --no-build -c Release --logger trx`: CI test command (docs/DELTAMAP_PLAN.md:637)
- `dotnet run --project tests/DeltaMapper.Benchmarks -c Release`: run benchmarks

## Fixtures & Mocks
- No test code exists yet
- Plan specifies FluentAssertions for assertions: docs/DELTAMAP_PLAN.md:666
- Plan specifies BenchmarkDotNet with MemoryDiagnoser: docs/DELTAMAP_PLAN.md:667

## CI Integration
- GitHub Actions (planned): docs/DELTAMAP_PLAN.md:622-639
  - `ci.yml`: restore -> build -> test -> pack on push/PR
  - Tests run via `dotnet test --no-build -c Release --logger trx`

## Planned Test Coverage (from plan)

### Phase 1 Tests (docs/DELTAMAP_PLAN.md:234-249)
- Convention mapping: same name/type
- Convention mapping: nested objects
- Convention mapping: collections (List, arrays, IEnumerable)
- ForMember with MapFrom resolver
- ForMember with Ignore
- ForMember with NullSubstitute
- BeforeMap / AfterMap hooks
- ReverseMap
- Record type mapping
- Init-only property mapping
- Circular reference detection
- Non-generic Map(object, Type, Type) overload
- MapList — list of 0, 1, N items
- Error: no mapping registered -> DeltaMapperException
- DI registration via AddDeltaMapper

### Phase 2 Tests (docs/DELTAMAP_PLAN.md:331-339)
- Patch with single changed property
- Patch with no changes
- Patch with multiple changes
- Patch with nested object changes (dot-notation)
- Patch with collection: item added/removed/modified
- Patch with null source property + NullSubstitute
- MappingDiff serializes to JSON

### Phase 3 Tests (docs/DELTAMAP_PLAN.md:415-422)
- Generator emits correct file for flat type pair
- Generator handles nested types
- Generator handles List and array destinations
- Generator respects [Ignore] attribute
- Analyzer emits DM001 warning
- Generated code compiles without warnings

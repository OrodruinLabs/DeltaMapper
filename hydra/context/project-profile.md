# Project Profile

## Language(s)
- C#: docs/DELTAMAP_PLAN.md:70 — "Target: `net8.0` (minimum)" with `LangVersion>latest`

## Framework(s)
- .NET 8/9 (multi-target class library): docs/DELTAMAP_PLAN.md:71 — "Multi-target: `net8.0;net9.0`"
- ASP.NET Core (DI integration only, not a web app itself): docs/DELTAMAP_PLAN.md:203-224

## Package Manager
- NuGet: docs/DELTAMAP_PLAN.md:4 — "NuGet: `DeltaMapper`"

## Build System
- dotnet CLI: docs/DELTAMAP_PLAN.md:636 — `dotnet build --no-restore -c Release`
- GitHub Actions CI: docs/DELTAMAP_PLAN.md:622-639

## Monorepo Structure
- Multi-project solution (not a monorepo): docs/DELTAMAP_PLAN.md:23-63
  - `src/DeltaMapper.Core/` — runtime library
  - `src/DeltaMapper.SourceGen/` — Roslyn source generator
  - `src/DeltaMapper.EFCore/` — EF Core integration
  - `src/DeltaMapper.OpenTelemetry/` — OpenTelemetry middleware
  - `tests/DeltaMapper.UnitTests/` — unit tests
  - `tests/DeltaMapper.IntegrationTests/` — integration tests
  - `tests/DeltaMapper.Benchmarks/` — BenchmarkDotNet benchmarks

## Database
- None detected (EF Core integration is a consumer library, not a database user)

## API Style
- None — this is a library, not a service
- Fluent configuration API: docs/DELTAMAP_PLAN.md:119-133

## State Management
- None detected (library project)

## Deployment Target
- NuGet.org package: docs/DELTAMAP_PLAN.md:649-655
  - `DeltaMapper` (core)
  - `DeltaMapper.SourceGen`
  - `DeltaMapper.EFCore`
  - `DeltaMapper.OpenTelemetry`

## Cloud Provider
- None detected (library project, no cloud hosting)

## IaC Tool
- None detected
- NOTE: Not applicable — this is a NuGet library, not a deployed service

## Container Orchestration
- None detected

## CI/CD Platform
- GitHub Actions (planned): docs/DELTAMAP_PLAN.md:622-639
  - `ci.yml` — build, test, pack on push/PR
  - `publish-nuget.yml` — publish on `v*` tags

## Observability
- Logging: None (library does not log directly)
- Monitoring: None
- Error tracking: None
- NOTE: Library exposes OpenTelemetry Activity spans via `DeltaMapper.OpenTelemetry` package: docs/DELTAMAP_PLAN.md:462-486

## Secret Management
- NuGet API key (for publish workflow): docs/DELTAMAP_PLAN.md:643 — implied by publish-nuget.yml

## Key Dependencies
1. System.Linq.Expressions — expression tree compilation for mapping delegates (docs/DELTAMAP_PLAN.md:158)
2. System.Collections.Frozen (FrozenDictionary) — read-optimized type registry (docs/DELTAMAP_PLAN.md:156)
3. Microsoft.Extensions.DependencyInjection.Abstractions — DI integration (docs/DELTAMAP_PLAN.md:203-224)
4. Microsoft.CodeAnalysis.CSharp — Roslyn source generator (docs/DELTAMAP_PLAN.md:74)
5. Microsoft.EntityFrameworkCore — EF Core proxy awareness (docs/DELTAMAP_PLAN.md:431)
6. System.Diagnostics.DiagnosticSource — OpenTelemetry ActivitySource (docs/DELTAMAP_PLAN.md:460)
7. xunit — test framework (docs/DELTAMAP_PLAN.md:73)
8. FluentAssertions — test assertions (docs/DELTAMAP_PLAN.md:73)
9. BenchmarkDotNet — performance benchmarks (docs/DELTAMAP_PLAN.md:73)
10. Microsoft.NET.Sdk — .NET SDK project system (docs/DELTAMAP_PLAN.md:86)

## GitHub Integration
- **GitHub repo**: Not detected (no remote configured)
- **gh CLI**: installed at /opt/homebrew/bin/gh
- **project scope**: present
- **Existing projects**: 1 project found (unrelated: "Strumtry Development Roadmap")

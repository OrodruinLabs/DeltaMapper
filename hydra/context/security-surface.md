# Security Surface

## Authentication
- None detected — this is a library, not a service

## Authorization
- None detected — not applicable for a library

## Input Validation
- Type safety via C# generics and compile-time checks
- Runtime validation: MapperConfiguration validates all registered maps at startup (docs/DELTAMAP_PLAN.md:155-159)
- DeltaMapperException thrown for unregistered type pairs with actionable messages (docs/DELTAMAP_PLAN.md:248, 568-569)
- Nullable reference types enabled: docs/DELTAMAP_PLAN.md:89 — `<Nullable>enable</Nullable>`

## Data Sanitization
- Not directly applicable — library maps between in-process objects, no I/O boundary

## Secrets Management
- NuGet API key for publish workflow — stored in GitHub Secrets (implied by docs/DELTAMAP_PLAN.md:643)

## CORS/CSP
- None detected — not applicable for a library

## Rate Limiting
- None detected — not applicable for a library

## Known Vulnerable Patterns
- None detected in plan
- **Watch for**: Expression tree compilation uses reflection internally — ensure no user-controlled type names can be injected to trigger unintended type loading
- **Watch for**: `object` casts in core mapping engine — plan restricts these to internals only (docs/DELTAMAP_PLAN.md:662)

## Recommendations
- Ensure `MapperContext` circular reference tracking uses `ReferenceEqualityComparer` (plan specifies this: docs/DELTAMAP_PLAN.md:193)
- Validate that expression-compiled delegates cannot be exploited via adversarial type definitions
- Review source generator output for information leakage (type names, property names in generated code are intentional and expected)
- NuGet package signing should be considered for supply chain security
- Add `SourceLink` for debuggable NuGet packages

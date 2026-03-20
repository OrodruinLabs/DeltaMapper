# Security Review: TASK-058 — Directory.Build.props + Multi-Target Production Packages

**Reviewer**: Security Reviewer Agent
**Date**: 2026-03-20
**Diff**: `hydra/reviews/TASK-058/diff.patch`

---

## Security Assessment

### Attack Surface

This task modifies only MSBuild project files and introduces `Directory.Build.props`. There is no runtime code change. The security surface is limited to:

1. Supply chain risk from new or changed `PackageReference` versions
2. Potential for unintended global property injection via `Directory.Build.props`

### Supply Chain: Package References

The diff changes conditional `PackageReference` versions for:
- `Microsoft.Extensions.DependencyInjection.Abstractions`: 8.*, 9.*, 10.*
- `Microsoft.EntityFrameworkCore`: 8.*, 9.*, 10.*

Both are first-party Microsoft packages published by `Microsoft`. The wildcard minor/patch resolution (`8.*`) is safe for LTS packages maintained by Microsoft — security patches are released as minor/patch increments within the major version. No third-party package references were added or changed.

**Finding**: No supply chain risk introduced. Confidence: 98.

### Directory.Build.props: Property Injection Risk

`Directory.Build.props` properties apply to every project under the repo root, including Benchmarks. The three properties set (`Nullable`, `ImplicitUsings`, `LangVersion`) are purely compiler-time flags with no runtime security implications. No `<DefineConstants>`, `<AllowUnsafeBlocks>`, `<Optimize>`, or execution-affecting properties are set.

**Finding**: No unsafe property injection. Confidence: 99.

### No Secrets or Credentials

The diff contains no API keys, tokens, connection strings, or credentials.

### No Runtime Logic Changed

All changes are build-system configuration. No C# source files were modified. No new attack surface at runtime.

---

## Final Verdict

**APPROVED**

No security concerns. Package references are first-party Microsoft libraries with appropriate wildcard version bounds. `Directory.Build.props` introduces no unsafe build flags. No secrets, no runtime code changes.

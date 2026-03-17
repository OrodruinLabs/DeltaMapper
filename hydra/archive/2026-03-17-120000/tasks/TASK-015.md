---
id: TASK-015
title: ChangeKind enum, PropertyChange record, MappingDiff<T>
status: DONE
depends_on: []
wave: 1
delegates_to: implementer
traces_to: "Phase 2 spec sections 2.1 (Types)"
files_to_create:
  - src/DeltaMapper.Core/Diff/ChangeKind.cs
  - src/DeltaMapper.Core/Diff/PropertyChange.cs
  - src/DeltaMapper.Core/Diff/MappingDiff.cs
files_to_modify: []
acceptance_criteria:
  - ChangeKind enum has exactly three members: Modified, Added, Removed
  - PropertyChange is a sealed record with PropertyName (string), From (object?), To (object?), Kind (ChangeKind)
  - MappingDiff<T> has Result (T), Changes (IReadOnlyList<PropertyChange>), and HasChanges computed property
---

**Retry count**: 0/3

## Implementation Log

- Created `src/DeltaMapper.Core/Diff/ChangeKind.cs` — `public enum ChangeKind` with Modified, Added, Removed
- Created `src/DeltaMapper.Core/Diff/PropertyChange.cs` — `public sealed record PropertyChange(string PropertyName, object? From, object? To, ChangeKind Kind)`
- Created `src/DeltaMapper.Core/Diff/MappingDiff.cs` — `public sealed class MappingDiff<T>` with `Result`, `Changes`, and `HasChanges` computed property
- `dotnet build -c Release` — Build succeeded, 0 warnings, 0 errors
- Namespace: `DeltaMapper.Diff` (file-scoped, matches Diff/ folder convention)

## Description
Create the three core Phase 2 types under a new `DeltaMapper.Diff` namespace in `src/DeltaMapper.Core/Diff/`. These are pure data types with no behavior beyond the `HasChanges` computed property.

## Implementation Notes
- Namespace: `DeltaMapper.Diff` (matches folder `Diff/`)
- One type per file (project convention from `feedback_namespace_conventions.md`)
- `PropertyChange` must be a `sealed record` (immutable, value equality)
- `MappingDiff<T>` should be a `sealed class` with `init` setters for `Result` and `Changes`
- `HasChanges` is `=> Changes.Count > 0` (expression-bodied, not stored)
- Follow the same file-scoped namespace style used in all existing `.cs` files

### Pattern Reference
- `src/DeltaMapper.Core/Configuration/MemberConfiguration.cs:1-13` — file-scoped namespace, sealed class, init properties
- `src/DeltaMapper.Core/Exceptions/DeltaMapperException.cs` — single-type-per-file pattern

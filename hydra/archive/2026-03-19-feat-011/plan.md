# Plan — Feature Gaps v0.2.0

## Objective

Close feature gaps vs AutoMapper/Mapperly — one feature per branch, one PR per feature, ship as v0.2.0.

## Status Summary

| Status | Count |
|--------|-------|
| DONE   | 7     |
| TOTAL  | 7     |

## Recovery Pointer

**Next**: Post-loop agents (documentation, release-manager)
**State**: All 7 tasks complete and approved
**Last updated**: 2026-03-18T16:30:00Z

## Tasks

| ID | Title | Status | Wave | Branch | Depends On |
|----|-------|--------|------|--------|------------|
| TASK-049 | Flattening (`Order.Customer.Name` → `CustomerName`) | DONE | 2 | `feat/flattening` | -- |
| TASK-050 | Assembly scanning (`AddProfilesFromAssembly`) | DONE | 2 | `feat/assembly-scanning` | -- |
| TASK-051 | Unflattening (`CustomerName` → `Order.Customer.Name`) | DONE | 3 | `feat/unflattening` | TASK-049 |
| TASK-052 | Type converters (`CreateTypeConverter<string, DateTime>`) | DONE | 4 | `feat/type-converters` | -- |

## Completed

- [x] Enum mapping (PR #15, merged)
- [x] Dictionary mapping (PR #16, merged)
- [x] Conditional mapping (PR #17, merged)
- [x] Flattening (TASK-049, approved)
- [x] Assembly scanning (TASK-050, approved)
- [x] Unflattening (TASK-051, approved)
- [x] Type converters (TASK-052, approved)

## Wave Groups

### Wave 2 (parallel — independent) ✓ COMPLETE
- TASK-049: Flattening ✓
- TASK-050: Assembly scanning ✓

### Wave 3 ✓ COMPLETE
- TASK-051: Unflattening ✓

### Wave 4 ✓ COMPLETE
- TASK-052: Type converters ✓

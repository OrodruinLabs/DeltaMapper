# TASK-044: Emit object initializer pattern in factory methods

**Status**: READY
**Wave**: 1

## Description
Change EmitFactoryMethod to generate `=> new Dst { Prop = src.Prop }` (single expression) instead of `var dst = new Dst(); dst.Prop = src.Prop;` (two-step). The JIT can inline and vectorize the single-expression pattern better.

Only use initializer for simple flat types (all direct assignments). Keep two-step for nested/collection types with control flow.

## Files
- Modify: `src/DeltaMapper.SourceGen/EmitHelper.cs`

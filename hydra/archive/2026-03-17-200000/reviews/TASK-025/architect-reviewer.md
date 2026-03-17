## Review: architect-reviewer
**Task**: TASK-025
**Verdict**: APPROVED

### Summary
MapperGenerator correctly implements IIncrementalGenerator using ForAttributeWithMetadataName for efficient filtering. The pipeline follows the standard pattern: RegisterPostInitializationOutput for the attribute, syntax provider for class filtering, and RegisterSourceOutput for code emission. EmitHelper cleanly separates code generation concerns from the generator pipeline.

### Findings
- ForAttributeWithMetadataName is the most efficient Roslyn API for attribute-based filtering, avoiding unnecessary syntax tree walks.
- The MappingInfo record correctly carries the profile class symbol and raw attribute data with locations for diagnostic reporting.
- GetMatchingProperties uses case-insensitive name matching with SymbolEqualityComparer for type checking, which is the correct approach for Roslyn symbol comparison.
- The generator emits one file per (src, dst) pair, which provides good incremental cache granularity.

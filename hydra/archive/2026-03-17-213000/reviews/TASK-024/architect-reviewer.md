## Review: architect-reviewer
**Task**: TASK-024
**Verdict**: APPROVED

### Summary
GenerateMapAttributeSource correctly defines the attribute source text as a const string for injection via RegisterPostInitializationOutput. The attribute lives in the DeltaMapper namespace (not SourceGen), uses fully-qualified System types to avoid using conflicts, and supports AllowMultiple=true for multi-mapping profiles.

### Findings
- The source-text-as-const pattern is the standard approach for Roslyn source generators that need to inject attributes into consuming compilations.
- AttributeName and HintName constants centralize the magic strings used by the generator pipeline.
- The attribute exposes Source and Destination as properties with a constructor, which is the correct pattern for attribute-based metadata.

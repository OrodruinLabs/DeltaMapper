## Review: code-reviewer
**Task**: TASK-024
**Verdict**: APPROVED

### Summary
Clean, minimal implementation. The raw string literal syntax is appropriate for multi-line source text. The GenerateMapAttributeTests.cs verifies the source text compiles without errors against a basic compilation.

### Findings
- The class is correctly marked internal static, limiting visibility to the generator assembly.
- The test creates a compilation with only the object assembly reference, which is sufficient to validate the attribute source text compiles standalone.

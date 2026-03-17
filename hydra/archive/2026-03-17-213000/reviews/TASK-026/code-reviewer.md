## Review: code-reviewer
**Task**: TASK-026
**Verdict**: APPROVED

### Summary
The MapperGeneratorAdvancedTests cover all required scenarios: nested type recursive calls, nested type skipping when no pair exists, List<T> with Select+ToList, array with Select+ToArray, primitive list/array direct copy, [Ignore] attribute, and [DeltaMapperIgnore] attribute. All tests include compilation verification.

### Findings
- The `using System.Linq;` directive is correctly added to emitted source for collection mapping.
- FindPair uses SymbolEqualityComparer.Default for reliable Roslyn symbol comparison.
- Null-conditional operator (?.) in generated collection assignments correctly handles null source collections.
- 12 advanced tests all pass with zero errors.

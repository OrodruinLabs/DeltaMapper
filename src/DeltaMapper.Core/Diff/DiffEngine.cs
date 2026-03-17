using System.Collections;
using System.Reflection;

namespace DeltaMapper.Diff;

/// <summary>
/// Compares before/after property snapshots and returns a list of detected changes.
/// Supports flat (primitive + string) and nested (dot-notation) comparison.
/// </summary>
internal static class DiffEngine
{
    /// <summary>
    /// Types considered "simple" for diff purposes: primitives, string, decimal,
    /// DateTime, DateTimeOffset, Guid, enums, and their nullable counterparts.
    /// </summary>
    internal static bool IsSimpleType(Type type)
    {
        var t = Nullable.GetUnderlyingType(type) ?? type;
        return t.IsPrimitive
            || t.IsEnum
            || t == typeof(string)
            || t == typeof(decimal)
            || t == typeof(DateTime)
            || t == typeof(DateTimeOffset)
            || t == typeof(Guid);
    }

    /// <summary>
    /// Compares two property snapshots and emits a <see cref="PropertyChange"/> for every
    /// property whose value differs. Properties where both before and after are <see langword="null"/>
    /// are skipped. Complex (non-simple) property values are recursed into with dot-notation keys.
    /// </summary>
    internal static List<PropertyChange> Compare(
        Dictionary<string, object?> before,
        Dictionary<string, object?> after,
        string prefix = "")
    {
        var changes = new List<PropertyChange>();

        foreach (var key in before.Keys)
        {
            var beforeValue = before[key];
            after.TryGetValue(key, out var afterValue);

            var fullKey = prefix.Length > 0 ? prefix + key : key;

            // Skip properties where both sides are null
            if (beforeValue is null && afterValue is null)
                continue;

            // One side null → emit single change for the whole property (no recursion)
            if (beforeValue is null || afterValue is null)
            {
                changes.Add(new PropertyChange(fullKey, beforeValue, afterValue, ChangeKind.Modified));
                continue;
            }

            // Determine if this is a simple or complex type
            var valueType = beforeValue.GetType();
            if (IsSimpleType(valueType))
            {
                if (!object.Equals(beforeValue, afterValue))
                    changes.Add(new PropertyChange(fullKey, beforeValue, afterValue, ChangeKind.Modified));
            }
            else if (beforeValue is IList beforeList && afterValue is IList afterList)
            {
                // Collection — compare by index
                changes.AddRange(CompareCollection(beforeList, afterList, fullKey));
            }
            else
            {
                // Complex object — recurse with dot-notation prefix
                var nestedBefore = Snapshot(beforeValue);
                var nestedAfter = Snapshot(afterValue);
                changes.AddRange(Compare(nestedBefore, nestedAfter, fullKey + "."));
            }
        }

        return changes;
    }

    /// <summary>
    /// Compares two collections element-by-element by index.
    /// Added/removed items beyond the shared range are emitted as Added/Removed.
    /// </summary>
    private static List<PropertyChange> CompareCollection(IList before, IList after, string propertyName)
    {
        var changes = new List<PropertyChange>();
        var shared = Math.Min(before.Count, after.Count);

        for (var i = 0; i < shared; i++)
        {
            var bItem = before[i];
            var aItem = after[i];
            var path = $"{propertyName}[{i}]";

            if (bItem is null && aItem is null)
                continue;

            if (bItem is null || aItem is null)
            {
                changes.Add(new PropertyChange(path, bItem, aItem, ChangeKind.Modified));
                continue;
            }

            if (IsSimpleType(bItem.GetType()))
            {
                if (!object.Equals(bItem, aItem))
                    changes.Add(new PropertyChange(path, bItem, aItem, ChangeKind.Modified));
            }
            else
            {
                // Complex element — recurse into its properties
                var nestedBefore = Snapshot(bItem);
                var nestedAfter = Snapshot(aItem);
                changes.AddRange(Compare(nestedBefore, nestedAfter, path + "."));
            }
        }

        // Items added (after is longer)
        for (var i = shared; i < after.Count; i++)
            changes.Add(new PropertyChange($"{propertyName}[{i}]", null, after[i], ChangeKind.Added));

        // Items removed (before was longer)
        for (var i = shared; i < before.Count; i++)
            changes.Add(new PropertyChange($"{propertyName}[{i}]", before[i], null, ChangeKind.Removed));

        return changes;
    }

    /// <summary>
    /// Snapshots all readable public instance properties of the target object.
    /// This method is flat — it does not recurse into nested objects.
    /// </summary>
    internal static Dictionary<string, object?> Snapshot(object target)
    {
        var props = target.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance);
        var snapshot = new Dictionary<string, object?>(props.Length);

        foreach (var prop in props)
        {
            if (prop.CanRead)
                snapshot[prop.Name] = prop.GetValue(target);
        }

        return snapshot;
    }
}

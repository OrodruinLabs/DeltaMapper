using System.Reflection;

namespace DeltaMapper.Diff;

/// <summary>
/// Compares before/after property snapshots and returns a list of detected changes.
/// Flat (primitive + string) comparison only — nested and collection diff are handled in later tasks.
/// </summary>
internal static class DiffEngine
{
    /// <summary>
    /// Compares two property snapshots and emits a <see cref="PropertyChange"/> for every
    /// property whose value differs. Properties where both before and after are <see langword="null"/>
    /// are skipped.
    /// </summary>
    internal static List<PropertyChange> Compare(
        Dictionary<string, object?> before,
        Dictionary<string, object?> after)
    {
        var changes = new List<PropertyChange>();

        foreach (var key in before.Keys)
        {
            var beforeValue = before[key];
            after.TryGetValue(key, out var afterValue);

            // Skip properties where both sides are null
            if (beforeValue is null && afterValue is null)
                continue;

            if (!object.Equals(beforeValue, afterValue))
                changes.Add(new PropertyChange(key, beforeValue, afterValue, ChangeKind.Modified));
        }

        return changes;
    }

    /// <summary>
    /// Snapshots all readable public instance properties of the target object.
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

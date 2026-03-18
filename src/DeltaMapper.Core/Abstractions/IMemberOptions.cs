using System.Linq.Expressions;

namespace DeltaMapper.Abstractions;

/// <summary>
/// Options for configuring how a specific destination member is mapped.
/// </summary>
public interface IMemberOptions<TSrc>
{
    /// <summary>
    /// Map this member using a custom expression.
    /// </summary>
    void MapFrom<TResult>(Expression<Func<TSrc, TResult>> resolver);

    /// <summary>
    /// Ignore this destination member (do not map it).
    /// </summary>
    void Ignore();

    /// <summary>
    /// Use the specified value when the source value is null.
    /// </summary>
    void NullSubstitute(object value);

    /// <summary>
    /// Only map this member when the condition is true.
    /// </summary>
    void Condition(Expression<Func<TSrc, bool>> predicate);
}

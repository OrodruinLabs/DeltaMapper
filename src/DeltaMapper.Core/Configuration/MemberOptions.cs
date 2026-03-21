using System.Linq.Expressions;

namespace DeltaMapper.Configuration;

internal sealed class MemberOptions<TSrc> : IMemberOptions<TSrc>
{
    public Func<object, object?>? Resolver { get; private set; }
    public Type? ResolverReturnType { get; private set; }
    public bool IsIgnored { get; private set; }
    public object? NullSubstituteValue { get; private set; }
    public bool HasNullSubstitute { get; private set; }
    public Func<object, bool>? ConditionPredicate { get; private set; }

    /// <summary>
    /// Source member names referenced in the MapFrom expression (best-effort extraction).
    /// Used for MemberList.Source validation tracking.
    /// </summary>
    public List<string> ReferencedSourceMembers { get; } = [];

    public void MapFrom<TResult>(Expression<Func<TSrc, TResult>> resolver)
    {
        ArgumentNullException.ThrowIfNull(resolver);
        var compiled = resolver.Compile();
        Resolver = src => compiled((TSrc)src);
        ResolverReturnType = typeof(TResult);
        // Extract source member names from simple expression patterns (s => s.Prop, s => s.Prop.Sub)
        ExtractSourceMembers(resolver.Body);
    }

    private void ExtractSourceMembers(Expression expr)
    {
        switch (expr)
        {
            case UnaryExpression unary:
                ExtractSourceMembers(unary.Operand);
                break;
            case BinaryExpression binary:
                ExtractSourceMembers(binary.Left);
                ExtractSourceMembers(binary.Right);
                break;
            case MethodCallExpression call:
                if (call.Object != null) ExtractSourceMembers(call.Object);
                foreach (var arg in call.Arguments) ExtractSourceMembers(arg);
                break;
            case MemberExpression member:
                // Walk chain to find root parameter member (e.g., s.Customer.Name → "Customer")
                var current = (Expression)member;
                while (current is MemberExpression m)
                {
                    if (m.Expression is ParameterExpression)
                    {
                        ReferencedSourceMembers.Add(m.Member.Name);
                        return;
                    }
                    current = m.Expression!;
                }
                break;
        }
    }

    public void Ignore()
    {
        if (ConditionPredicate is not null)
            throw new InvalidOperationException(
                "Cannot combine Ignore() with Condition() on the same member. " +
                "Use Condition() alone to conditionally skip mapping.");
        IsIgnored = true;
    }

    public void NullSubstitute(object value)
    {
        NullSubstituteValue = value;
        HasNullSubstitute = true;
    }

    public void Condition(Expression<Func<TSrc, bool>> predicate)
    {
        ArgumentNullException.ThrowIfNull(predicate);
        if (IsIgnored)
            throw new InvalidOperationException(
                "Cannot combine Condition() with Ignore() on the same member. " +
                "Use Condition() alone to conditionally skip mapping.");
        var compiled = predicate.Compile();
        ConditionPredicate = src => compiled((TSrc)src);
    }
}

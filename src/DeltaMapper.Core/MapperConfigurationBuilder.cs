using System.Reflection;
using DeltaMapper.Middleware;

namespace DeltaMapper;

/// <summary>
/// Builder for constructing MapperConfiguration. Collects profiles and middleware,
/// then compiles all type maps into cached delegates at Build() time.
/// </summary>
public sealed class MapperConfigurationBuilder
{
    private readonly List<MappingProfile> _profiles = new();
    private readonly List<IMappingMiddleware> _middlewares = new();

    /// <summary>
    /// Adds a mapping profile by type (instantiates via parameterless constructor).
    /// </summary>
    public MapperConfigurationBuilder AddProfile<TProfile>() where TProfile : MappingProfile, new()
    {
        _profiles.Add(new TProfile());
        return this;
    }

    /// <summary>
    /// Adds an existing mapping profile instance.
    /// </summary>
    public MapperConfigurationBuilder AddProfile(MappingProfile profile)
    {
        ArgumentNullException.ThrowIfNull(profile);
        _profiles.Add(profile);
        return this;
    }

    /// <summary>
    /// Registers a middleware component in the mapping pipeline.
    /// </summary>
    public MapperConfigurationBuilder Use<TMiddleware>() where TMiddleware : IMappingMiddleware, new()
    {
        _middlewares.Add(new TMiddleware());
        return this;
    }

    /// <summary>
    /// Compiles all registered profiles into an immutable MapperConfiguration.
    /// All reflection happens here — the resulting delegates use cached PropertyInfo.
    /// </summary>
    public MapperConfiguration Build()
    {
        var allTypeMaps = new List<TypeMapConfiguration>();

        foreach (var profile in _profiles)
        {
            allTypeMaps.AddRange(profile.TypeMaps);
        }

        // Handle ReverseMap — generate inverse type maps with convention-only matching
        var reverseMaps = new List<TypeMapConfiguration>();
        foreach (var tm in allTypeMaps)
        {
            if (tm.HasReverseMap)
            {
                reverseMaps.Add(new TypeMapConfiguration
                {
                    SourceType = tm.DestinationType,
                    DestinationType = tm.SourceType
                });
            }
        }
        allTypeMaps.AddRange(reverseMaps);

        // Compile each type map into a delegate
        var compiled = new Dictionary<(Type, Type), CompiledMap>();
        foreach (var tm in allTypeMaps)
        {
            var key = (tm.SourceType, tm.DestinationType);
            compiled[key] = CompileTypeMap(tm);
        }

        return MapperConfiguration.CreateFromBuilder(compiled, _middlewares);
    }

    private static CompiledMap CompileTypeMap(TypeMapConfiguration tm)
    {
        var srcType = tm.SourceType;
        var dstType = tm.DestinationType;
        var srcProps = srcType.GetProperties(BindingFlags.Public | BindingFlags.Instance);
        var dstProps = dstType.GetProperties(BindingFlags.Public | BindingFlags.Instance);

        // Build property assignment list at compile time — reflection cost is paid here only
        var assignments = new List<Action<object, object, MapperContext>>();

        foreach (var dstProp in dstProps)
        {
            if (!dstProp.CanWrite) continue;

            // Check for member configuration override
            var memberConfig = tm.MemberConfigurations
                .FirstOrDefault(mc => mc.DestinationMemberName.Equals(dstProp.Name, StringComparison.OrdinalIgnoreCase));

            if (memberConfig != null)
            {
                if (memberConfig.IsIgnored)
                    continue;

                if (memberConfig.CustomResolver != null)
                {
                    var resolver = memberConfig.CustomResolver;
                    var setter = dstProp;
                    assignments.Add((src, dst, ctx) =>
                    {
                        var value = resolver(src);
                        setter.SetValue(dst, value);
                    });
                    continue;
                }

                if (memberConfig.HasNullSubstitute)
                {
                    var srcProp = FindSourceProperty(srcProps, dstProp.Name);
                    var substituteValue = memberConfig.NullSubstituteValue;
                    var setter = dstProp;
                    if (srcProp != null)
                    {
                        assignments.Add((src, dst, ctx) =>
                        {
                            var value = srcProp.GetValue(src);
                            setter.SetValue(dst, value ?? substituteValue);
                        });
                    }
                    else
                    {
                        assignments.Add((src, dst, ctx) => setter.SetValue(dst, substituteValue));
                    }
                    continue;
                }
            }

            // Convention matching — find source property with same name (case-insensitive)
            var matchingSrcProp = FindSourceProperty(srcProps, dstProp.Name);
            if (matchingSrcProp == null) continue;

            // Capture loop variables for closure
            var srcPropCaptured = matchingSrcProp;
            var dstPropCaptured = dstProp;

            if (IsDirectlyAssignable(srcPropCaptured.PropertyType, dstPropCaptured.PropertyType))
            {
                // Direct assign — same type or implicitly assignable (e.g., int to long)
                assignments.Add((src, dst, ctx) =>
                {
                    var value = srcPropCaptured.GetValue(src);
                    dstPropCaptured.SetValue(dst, value);
                });
            }
            else if (IsCollectionMapping(srcPropCaptured.PropertyType, dstPropCaptured.PropertyType,
                         out var srcElementType, out var dstElementType))
            {
                // Collection mapping — map each element
                var srcElem = srcElementType!;
                var dstElem = dstElementType!;
                assignments.Add((src, dst, ctx) =>
                {
                    var srcCollection = srcPropCaptured.GetValue(src);
                    if (srcCollection == null)
                    {
                        dstPropCaptured.SetValue(dst, null);
                        return;
                    }

                    var enumerable = (System.Collections.IEnumerable)srcCollection;
                    var items = new List<object>();
                    foreach (var item in enumerable)
                    {
                        if (item == null) continue;
                        // If element types are directly assignable, no mapping needed
                        if (IsDirectlyAssignable(srcElem, dstElem))
                        {
                            items.Add(item);
                        }
                        else
                        {
                            var mappedItem = ctx.Config.Execute(item, srcElem, dstElem, ctx);
                            items.Add(mappedItem);
                        }
                    }

                    var destCollection = CreateCollection(dstPropCaptured.PropertyType, dstElem, items);
                    dstPropCaptured.SetValue(dst, destCollection);
                });
            }
            else if (IsComplexType(srcPropCaptured.PropertyType))
            {
                // Complex object — recursive mapping
                assignments.Add((src, dst, ctx) =>
                {
                    var srcValue = srcPropCaptured.GetValue(src);
                    if (srcValue == null)
                    {
                        dstPropCaptured.SetValue(dst, null);
                        return;
                    }
                    var mapped = ctx.Config.Execute(srcValue, srcPropCaptured.PropertyType, dstPropCaptured.PropertyType, ctx);
                    dstPropCaptured.SetValue(dst, mapped);
                });
            }
        }

        // Build the combined delegate — this closure captures assignments, dstType, and tm hooks
        var beforeMap = tm.BeforeMapAction;
        var afterMap = tm.AfterMapAction;

        Func<object, object?, MapperContext, object> mapFunc = (src, existingDst, ctx) =>
        {
            var dst = existingDst ?? Activator.CreateInstance(dstType)!;

            // Register for circular reference detection BEFORE property assignment
            ctx.Register(src, dst);

            // BeforeMap hook
            beforeMap?.Invoke(src, dst);

            // Execute all property assignments
            foreach (var assign in assignments)
            {
                assign(src, dst, ctx);
            }

            // AfterMap hook
            afterMap?.Invoke(src, dst);

            return dst;
        };

        return new CompiledMap(mapFunc);
    }

    private static PropertyInfo? FindSourceProperty(PropertyInfo[] srcProps, string dstName)
    {
        return srcProps.FirstOrDefault(p => p.Name.Equals(dstName, StringComparison.OrdinalIgnoreCase));
    }

    private static bool IsDirectlyAssignable(Type srcType, Type dstType)
    {
        return dstType.IsAssignableFrom(srcType);
    }

    private static bool IsComplexType(Type type)
    {
        return !type.IsPrimitive
            && !type.IsEnum
            && type != typeof(string)
            && type != typeof(decimal)
            && type.IsClass;
    }

    private static bool IsCollectionMapping(Type srcType, Type dstType, out Type? srcElement, out Type? dstElement)
    {
        srcElement = null;
        dstElement = null;

        var srcElementType = GetEnumerableElementType(srcType);
        var dstElementType = GetEnumerableElementType(dstType);

        if (srcElementType != null && dstElementType != null)
        {
            srcElement = srcElementType;
            dstElement = dstElementType;
            return true;
        }

        return false;
    }

    private static Type? GetEnumerableElementType(Type type)
    {
        // Skip string — it implements IEnumerable<char> but should not be treated as a collection
        if (type == typeof(string))
            return null;

        if (type.IsArray)
            return type.GetElementType();

        if (type.IsGenericType)
        {
            var genArgs = type.GetGenericArguments();
            if (genArgs.Length == 1)
            {
                var enumerableInterface = typeof(IEnumerable<>).MakeGenericType(genArgs[0]);
                if (enumerableInterface.IsAssignableFrom(type))
                    return genArgs[0];
            }
        }

        return null;
    }

    private static object CreateCollection(Type destCollectionType, Type elementType, List<object> items)
    {
        if (destCollectionType.IsArray)
        {
            var array = Array.CreateInstance(elementType, items.Count);
            for (int i = 0; i < items.Count; i++)
                array.SetValue(items[i], i);
            return array;
        }

        // Default to List<T>
        var listType = typeof(List<>).MakeGenericType(elementType);
        var list = (System.Collections.IList)Activator.CreateInstance(listType, items.Count)!;
        foreach (var item in items)
            list.Add(item);
        return list;
    }
}

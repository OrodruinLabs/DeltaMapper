using System.Reflection;
using DeltaMapper.Exceptions;
using DeltaMapper.Middleware;

namespace DeltaMapper;

/// <summary>
/// Builder for constructing MapperConfiguration. Collects profiles and middleware,
/// then compiles all type maps into cached delegates at Build() time.
/// </summary>
public sealed class MapperConfigurationBuilder
{
    private readonly List<MappingProfile> _profiles = [];
    private readonly List<IMappingMiddleware> _middlewares = [];

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
    /// PropertyInfo lookups are resolved here at build time; the resulting delegates
    /// use cached PropertyInfo.GetValue/SetValue for property access at map time.
    /// </summary>
    public MapperConfiguration Build()
    {
        List<TypeMapConfiguration> allTypeMaps = [];

        foreach (var profile in _profiles)
        {
            allTypeMaps.AddRange(profile.TypeMaps);
        }

        // Handle ReverseMap — generate inverse type maps with convention-only matching
        List<TypeMapConfiguration> reverseMaps = [];
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

        // Check if destination type needs constructor injection (records or init-only properties)
        if (NeedsConstructorInjection(dstType, dstProps))
        {
            return CompileConstructorMap(tm, srcType, dstType, srcProps, dstProps);
        }

        // Build property assignment list at compile time — reflection cost is paid here only
        List<Action<object, object, MapperContext>> assignments = [];

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
            else if (IsNumericWidening(srcPropCaptured.PropertyType, dstPropCaptured.PropertyType))
            {
                // Numeric widening conversion — e.g., int → long, float → double
                assignments.Add((src, dst, ctx) =>
                {
                    var value = srcPropCaptured.GetValue(src);
                    if (value == null)
                    {
                        dstPropCaptured.SetValue(dst, null);
                        return;
                    }
                    var converted = Convert.ChangeType(value, dstPropCaptured.PropertyType);
                    dstPropCaptured.SetValue(dst, converted);
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
                    List<object> items = [];
                    foreach (var item in enumerable)
                    {
                        if (item == null)
                        {
                            items.Add(null!);
                            continue;
                        }
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

    private static bool NeedsConstructorInjection(Type dstType, PropertyInfo[] dstProps)
    {
        // Check if any writable property has an init-only setter (IsExternalInit modreq)
        foreach (var prop in dstProps)
        {
            if (!prop.CanWrite) continue;
            var setMethod = prop.GetSetMethod(true);
            if (setMethod == null) continue;

            var returnParam = setMethod.ReturnParameter;
            var requiredMods = returnParam.GetRequiredCustomModifiers();
            if (requiredMods.Any(m => m.FullName == "System.Runtime.CompilerServices.IsExternalInit"))
                return true;
        }

        return false;
    }

    private static CompiledMap CompileConstructorMap(
        TypeMapConfiguration tm, Type srcType, Type dstType,
        PropertyInfo[] srcProps, PropertyInfo[] dstProps)
    {
        // Find the best constructor — prefer the one with the most parameters matching source properties
        var constructors = dstType.GetConstructors(BindingFlags.Public | BindingFlags.Instance);
        var bestCtor = constructors
            .OrderByDescending(c => c.GetParameters().Length)
            .FirstOrDefault(c => c.GetParameters().All(p =>
                FindSourceProperty(srcProps, p.Name!) != null ||
                tm.MemberConfigurations.Any(mc => mc.DestinationMemberName.Equals(p.Name, StringComparison.OrdinalIgnoreCase))));

        if (bestCtor == null)
        {
            // Fallback: try parameterless constructor
            bestCtor = constructors.FirstOrDefault(c => c.GetParameters().Length == 0);
            if (bestCtor == null)
                throw new DeltaMapperException(
                    $"No suitable constructor found for '{dstType.Name}'. Ensure constructor parameter names match source property names.");
        }

        var ctorParams = bestCtor.GetParameters();

        // Build parameter resolvers
        List<Func<object, MapperContext, object?>> paramResolvers = [];
        foreach (var param in ctorParams)
        {
            // Check for ForMember override
            var memberConfig = tm.MemberConfigurations
                .FirstOrDefault(mc => mc.DestinationMemberName.Equals(param.Name!, StringComparison.OrdinalIgnoreCase));

            if (memberConfig?.IsIgnored == true)
            {
                // Ignored — use parameter default or type default
                var defaultValue = param.HasDefaultValue && param.DefaultValue != DBNull.Value
                    ? param.DefaultValue
                    : (param.ParameterType.IsValueType ? Activator.CreateInstance(param.ParameterType) : null);
                paramResolvers.Add((src, ctx) => defaultValue);
            }
            else if (memberConfig?.CustomResolver != null)
            {
                var resolver = memberConfig.CustomResolver;
                paramResolvers.Add((src, ctx) => resolver(src));
            }
            else if (memberConfig?.HasNullSubstitute == true)
            {
                var srcProp = FindSourceProperty(srcProps, param.Name!);
                var substituteValue = memberConfig.NullSubstituteValue;
                if (srcProp != null)
                {
                    var capturedSrcProp = srcProp;
                    paramResolvers.Add((src, ctx) => capturedSrcProp.GetValue(src) ?? substituteValue);
                }
                else
                {
                    paramResolvers.Add((src, ctx) => substituteValue);
                }
            }
            else
            {
                var srcProp = FindSourceProperty(srcProps, param.Name!);
                if (srcProp != null)
                {
                    var capturedSrcProp = srcProp;
                    paramResolvers.Add((src, ctx) => capturedSrcProp.GetValue(src));
                }
                else
                {
                    var defaultValue = param.HasDefaultValue && param.DefaultValue != DBNull.Value
                        ? param.DefaultValue
                        : (param.ParameterType.IsValueType ? Activator.CreateInstance(param.ParameterType) : null);
                    paramResolvers.Add((src, ctx) => defaultValue);
                }
            }
        }

        // Find init-only properties NOT covered by constructor params
        var ctorParamNames = new HashSet<string>(ctorParams.Select(p => p.Name!), StringComparer.OrdinalIgnoreCase);
        List<Action<object, object, MapperContext>> initOnlyAssignments = [];

        foreach (var dstProp in dstProps)
        {
            if (!dstProp.CanWrite) continue;
            if (ctorParamNames.Contains(dstProp.Name)) continue; // Already handled by constructor

            var memberConfig = tm.MemberConfigurations
                .FirstOrDefault(mc => mc.DestinationMemberName.Equals(dstProp.Name, StringComparison.OrdinalIgnoreCase));

            if (memberConfig?.IsIgnored == true) continue;

            if (memberConfig?.CustomResolver != null)
            {
                var resolver = memberConfig.CustomResolver;
                var setter = dstProp;
                initOnlyAssignments.Add((src, dst, ctx) => setter.SetValue(dst, resolver(src)));
                continue;
            }

            if (memberConfig?.HasNullSubstitute == true)
            {
                var srcProp2 = FindSourceProperty(srcProps, dstProp.Name);
                var substituteValue = memberConfig.NullSubstituteValue;
                var setter = dstProp;
                if (srcProp2 != null)
                {
                    initOnlyAssignments.Add((src, dst, ctx) => setter.SetValue(dst, srcProp2.GetValue(src) ?? substituteValue));
                }
                else
                {
                    initOnlyAssignments.Add((src, dst, ctx) => setter.SetValue(dst, substituteValue));
                }
                continue;
            }

            var srcProp = FindSourceProperty(srcProps, dstProp.Name);
            if (srcProp != null && IsDirectlyAssignable(srcProp.PropertyType, dstProp.PropertyType))
            {
                var capturedSrc = srcProp;
                var capturedDst = dstProp;
                initOnlyAssignments.Add((src, dst, ctx) => capturedDst.SetValue(dst, capturedSrc.GetValue(src)));
            }
        }

        var beforeMap = tm.BeforeMapAction;
        var afterMap = tm.AfterMapAction;
        var ctor = bestCtor;

        Func<object, object?, MapperContext, object> mapFunc = (src, existingDst, ctx) =>
        {
            // For constructor-injected types, we must always create a new instance
            // because the values are set via the constructor. If an existing destination
            // was provided, we log this but still construct fresh.
            var args = new object?[paramResolvers.Count];
            for (int i = 0; i < paramResolvers.Count; i++)
            {
                args[i] = paramResolvers[i](src, ctx);
            }

            var dst = ctor.Invoke(args);

            ctx.Register(src, dst);

            beforeMap?.Invoke(src, dst);

            // Set additional init-only properties not covered by constructor
            foreach (var assign in initOnlyAssignments)
            {
                assign(src, dst, ctx);
            }

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

    /// <summary>
    /// Defines the safe widening paths for numeric types.
    /// A mapping is only considered "widening" if the destination type can represent
    /// all values of the source type without overflow or precision loss.
    /// </summary>
    private static readonly Dictionary<Type, HashSet<Type>> _wideningMap = new()
    {
        [typeof(byte)]   = new() { typeof(short), typeof(ushort), typeof(int), typeof(uint), typeof(long), typeof(ulong), typeof(float), typeof(double), typeof(decimal) },
        [typeof(sbyte)]  = new() { typeof(short), typeof(int), typeof(long), typeof(float), typeof(double), typeof(decimal) },
        [typeof(short)]  = new() { typeof(int), typeof(long), typeof(float), typeof(double), typeof(decimal) },
        [typeof(ushort)] = new() { typeof(int), typeof(uint), typeof(long), typeof(ulong), typeof(float), typeof(double), typeof(decimal) },
        [typeof(int)]    = new() { typeof(long), typeof(double), typeof(decimal) },
        [typeof(uint)]   = new() { typeof(long), typeof(ulong), typeof(double), typeof(decimal) },
        [typeof(long)]   = new() { typeof(decimal) },
        [typeof(ulong)]  = new() { typeof(decimal) },
        [typeof(float)]  = new() { typeof(double) },
    };

    /// <summary>
    /// Returns true when srcType can be safely widened to dstType without overflow or precision loss.
    /// e.g. int → long, float → double. Handles nullable variants too.
    /// </summary>
    private static bool IsNumericWidening(Type srcType, Type dstType)
    {
        var srcUnderlying = Nullable.GetUnderlyingType(srcType) ?? srcType;
        var dstUnderlying = Nullable.GetUnderlyingType(dstType) ?? dstType;
        return _wideningMap.TryGetValue(srcUnderlying, out var targets) && targets.Contains(dstUnderlying);
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

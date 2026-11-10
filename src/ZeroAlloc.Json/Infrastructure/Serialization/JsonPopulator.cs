using System.Buffers;
using System.Buffers.Text;
using System.Collections;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using GapSonar.Core.Shared;

namespace GapSonar.Core.Shared.Infrastructure.Serialization;

public static class JsonPopulator
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Populate<T>(ReadOnlySpan<byte> json, T target, StringInternerDelegate? interner = null) where T : class
    {
        var reader = new Utf8JsonReader(json);
        Populate(reader, target, interner);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Populate<T>(ReadOnlySequence<byte> json, T target, StringInternerDelegate? interner = null) where T : class
    {
        var reader = new Utf8JsonReader(json);
        Populate(reader, target, interner);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Populate<T>(ReadOnlySpan<byte> json, ref T target, StringInternerDelegate? interner = null)
    {
        var reader = new Utf8JsonReader(json);
        JsonPopulator<T>.Populate(ref reader, ref target, interner);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Populate<T>(ReadOnlySequence<byte> json, ref T target, StringInternerDelegate? interner = null)
    {
        var reader = new Utf8JsonReader(json);
        JsonPopulator<T>.Populate(ref reader, ref target, interner);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Populate<T>(ref Utf8JsonReader reader, ref T target, StringInternerDelegate? interner = null)
    {
#if DEBUG
        object? originalRef = null;
        bool isClass = !typeof(T).IsValueType;
        if (isClass) originalRef = target;
#endif
        JsonPopulator<T>.Populate(ref reader, ref target, interner);
#if DEBUG
        if (isClass && !ReferenceEquals(originalRef, target))
        {
            throw new InvalidOperationException($"JsonPopulator: Object reference replaced during Populate for {typeof(T).Name}!");
        }
#endif
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Populate<T>(Utf8JsonReader reader, T target, StringInternerDelegate? interner = null) where T : class
    {
#if DEBUG
        object? originalRef = target;
#endif
        JsonPopulator<T>.Populate(ref reader, ref target, interner);
#if DEBUG
        if (!ReferenceEquals(originalRef, target))
        {
            throw new InvalidOperationException($"JsonPopulator: Object reference replaced during Populate for {typeof(T).Name}! Ensure you are not assigning to 'target' in custom parsers.");
        }
#endif
    }
}

internal static class JsonPopulator<T>
{
    internal delegate void PropAction(ref T target, ref Utf8JsonReader reader, StringInternerDelegate? interner);
    internal delegate void SetterDelegate<TProp>(ref T target, TProp value);
    private delegate TProp GetterDelegate<TProp>(ref T target);

    private static readonly PropertyLookupEntry[][]? _lookup;
    private static readonly PropAction[]? _positionalActions;
    private static readonly int _minLen;
    private static readonly int _maxLen;
    private static readonly PropAction? _rootAction;

    private readonly struct PropertyLookupEntry(byte[] nameBytes, PropAction action)
    {
        public readonly byte[] NameBytes = nameBytes;
        public readonly PropAction Action = action;
    }

    static JsonPopulator()
    {
        // 1. Check if T itself is a collection (for Root Array support)
        if (typeof(T).IsGenericType)
        {
            var genDef = typeof(T).GetGenericTypeDefinition();
            if (genDef == typeof(List<>))
            {
                var itemType = typeof(T).GetGenericArguments()[0];
                var method = typeof(JsonPopulator<T>).GetMethod(nameof(CreateRootListAction), BindingFlags.Static | BindingFlags.NonPublic)!
                    .MakeGenericMethod(itemType);
                _rootAction = (PropAction)method.Invoke(null, null)!;
            }
            else if (genDef == typeof(FastList<>))
            {
                var itemType = typeof(T).GetGenericArguments()[0];
                var method = typeof(JsonPopulator<T>).GetMethod(nameof(CreateRootFastListAction), BindingFlags.Static | BindingFlags.NonPublic)!
                    .MakeGenericMethod(itemType);
                _rootAction = (PropAction)method.Invoke(null, null)!;
            }
        }

        // 2. Map properties/fields
        var properties = typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => (p.CanWrite || IsCollectionType(p.PropertyType) || (p.PropertyType.IsClass && p.PropertyType != typeof(string))) && p.GetIndexParameters().Length == 0)
            .Select(p => new { Member = (MemberInfo)p, Type = p.PropertyType, Name = p.Name });

        var fields = typeof(T).GetFields(BindingFlags.Public | BindingFlags.Instance)
            .Select(f => new { Member = (MemberInfo)f, Type = f.FieldType, Name = f.Name });

        var members = properties.Concat(fields).ToList();
        var map = new Dictionary<int, List<PropertyLookupEntry>>();
        var positionalMap = new SortedDictionary<int, PropAction>();

        foreach (var member in members)
        {
            string name = member.Name;
            var jsonAttr = member.Member.GetCustomAttribute<JsonPropertyNameAttribute>();
            if (jsonAttr is not null) name = jsonAttr.Name;

            byte[] nameBytes = Encoding.UTF8.GetBytes(name);
            int len = nameBytes.Length;

            try
            {
                PropAction? action = CreateAction(member.Member);
                if (action is null) continue;

                // For name-based lookup
                if (!map.TryGetValue(len, out var list))
                {
                    list = [];
                    map[len] = list;
                }
                list.Add(new PropertyLookupEntry(nameBytes, action));

                // For positional lookup
                var posAttr = member.Member.GetCustomAttribute<JsonPositionAttribute>();
                if (posAttr is not null)
                {
                    positionalMap[posAttr.Position] = action;
                }
            }
            catch
            {
                continue;
            }
        }

        // Finalize name lookup
        if (map.Count > 0)
        {
            _minLen = map.Keys.Min();
            _maxLen = map.Keys.Max();
            _lookup = new PropertyLookupEntry[_maxLen + 1][];
            foreach (var kvp in map)
            {
                _lookup[kvp.Key] = [.. kvp.Value];
            }
        }
        else
        {
            _lookup = [];
            _minLen = 0;
            _maxLen = 0;
        }

        // Finalize positional actions
        if (positionalMap.Count > 0)
        {
            int maxPos = positionalMap.Keys.Max();
            _positionalActions = new PropAction[maxPos + 1];
            foreach (var kvp in positionalMap)
            {
                _positionalActions[kvp.Key] = kvp.Value;
            }
        }
        else if (members.Count > 0)
        {
            // Default: by declaration order if no attributes (useful for simple structs)
            _positionalActions = members
                .Select(m => CreateAction(m.Member))
                .Where(a => a is not null)
                .Cast<PropAction>()
                .ToArray();
        }
    }

    private static bool IsCollectionType(Type t)
    {
        return t.IsGenericType && (
            t.GetGenericTypeDefinition() == typeof(List<>) ||
            t.GetGenericTypeDefinition() == typeof(FastList<>)
        );
    }

    public static void Populate(ref Utf8JsonReader reader, ref T target, StringInternerDelegate? interner)
    {
        if (reader.TokenType == JsonTokenType.None) reader.Read();

        // Support for Root Arrays (List/FastList as target)
        if (reader.TokenType == JsonTokenType.StartArray && _rootAction is not null)
        {
            _rootAction(ref target, ref reader, interner);
            return;
        }

        // Support for Positional Arrays (Object/Struct as target, but JSON is array)
        if (reader.TokenType == JsonTokenType.StartArray && _positionalActions is not null)
        {
            int index = 0;
            while (reader.Read() && reader.TokenType != JsonTokenType.EndArray)
            {
                if (index < _positionalActions.Length)
                {
                    _positionalActions[index]?.Invoke(ref target, ref reader, interner);
                }
                else
                {
                    reader.Skip();
                }
                index++;
            }
            return;
        }

        if (reader.TokenType != JsonTokenType.StartObject) return;
        if (_lookup is null || _lookup.Length == 0) return;

        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.EndObject) return;

            if (reader.TokenType == JsonTokenType.PropertyName)
            {
                int len = reader.HasValueSequence ? (int)reader.ValueSequence.Length : reader.ValueSpan.Length;

                if (len >= _minLen && len <= _maxLen)
                {
                    var candidates = _lookup[len];
                    if (candidates is not null)
                    {
                        bool found = false;
                        for (int i = 0; i < candidates.Length; i++)
                        {
                            ref readonly var entry = ref candidates[i];
                            // ValueTextEquals is highly optimized and correctly handles multi-segment ReadOnlySequence.
                            if (reader.ValueTextEquals(entry.NameBytes))
                            {
                                reader.Read();
                                entry.Action(ref target, ref reader, interner);
                                found = true;
                                break;
                            }
                        }
                        if (!found) reader.Skip();
                    }
                    else reader.Skip();
                }
                else reader.Skip();
            }
        }
    }

    private static PropAction CreateRootListAction<TItem>()
    {
        ParserDelegate<TItem>? itemParser = GetParserForItem<TItem>();
        return (ref target, ref reader, interner) =>
        {
            target ??= (T)(object)new List<TItem>();
            var list = (List<TItem>)(object)target!;
            list.Clear();
            while (reader.Read() && reader.TokenType != JsonTokenType.EndArray)
            {
                if (itemParser is not null) list.Add(itemParser(ref reader, interner));
                else
                {
                    TItem item = typeof(TItem).IsValueType ? default! : Activator.CreateInstance<TItem>();
                    JsonPopulator.Populate(ref reader, ref item, interner);
                    list.Add(item);
                }
            }
        };
    }

    private static PropAction CreateRootFastListAction<TItem>()
    {
        ParserDelegate<TItem>? itemParser = GetParserForItem<TItem>();
        return (ref target, ref reader, interner) =>
        {
            if (target is null) throw new InvalidOperationException("FastList must be pre-allocated.");
            var list = (FastList<TItem>)(object)target;
            list.Clear();
            while (reader.Read() && reader.TokenType != JsonTokenType.EndArray)
            {
                ref TItem item = ref list.AddAndGetRef();
                if (itemParser is not null) item = itemParser(ref reader, interner);
                else JsonPopulator.Populate(ref reader, ref item, interner);
            }
        };
    }

    private static PropAction? CreateAction(MemberInfo member)
    {
        Type type;
        bool canWrite;

        if (member is PropertyInfo p)
        {
            type = p.PropertyType;
            canWrite = p.CanWrite;
        }
        else if (member is FieldInfo f)
        {
            type = f.FieldType;
            canWrite = !f.IsInitOnly;
        }
        else throw new NotSupportedException();

        bool useInterner = Attribute.IsDefined(member, typeof(JsonInternAttribute));

        var paramTarget = Expression.Parameter(typeof(T).MakeByRefType(), "target");
        var paramVal = Expression.Parameter(type, "val");

        Expression memberExpr = member is PropertyInfo pi
            ? Expression.Property(paramTarget, pi)
            : Expression.Field(paramTarget, (FieldInfo)member);

        object? setterDelegate = null;
        if (canWrite)
        {
            var assign = Expression.Assign(memberExpr, paramVal);
            MethodInfo createSetterMethod = typeof(JsonPopulator<T>)
                .GetMethod(nameof(CreateSetterDelegate), BindingFlags.Static | BindingFlags.NonPublic)!
                .MakeGenericMethod(type);
            setterDelegate = createSetterMethod.Invoke(null, [paramTarget, paramVal, assign]);
        }

        if (type == typeof(double) && setterDelegate is not null) return CreateParser<double>(setterDelegate, ParserHelpers.ParseDouble, useInterner);
        if (type == typeof(decimal) && setterDelegate is not null) return CreateParser<decimal>(setterDelegate, ParserHelpers.ParseDecimal, useInterner);
        if (type == typeof(long) && setterDelegate is not null) return CreateParser<long>(setterDelegate, ParserHelpers.ParseLong, useInterner);
        if (type == typeof(int) && setterDelegate is not null) return CreateParser<int>(setterDelegate, ParserHelpers.ParseInt, useInterner);
        if (type == typeof(bool) && setterDelegate is not null) return CreateParser<bool>(setterDelegate, ParserHelpers.ParseBool, useInterner);
        if (type == typeof(string) && setterDelegate is not null) return CreateParser<string>(setterDelegate, ParserHelpers.ParseString, useInterner);
        if (type == typeof(DateTime) && setterDelegate is not null) return CreateParser<DateTime>(setterDelegate, ParserHelpers.ParseDateTime, useInterner);
        if (type == typeof(Guid) && setterDelegate is not null) return CreateParser<Guid>(setterDelegate, ParserHelpers.ParseGuid, useInterner);

        // Fixed-size string types - zero allocation, no interning needed
        if (type == typeof(FixedString8) && setterDelegate is not null) return CreateParser<FixedString8>(setterDelegate, ParserHelpers.ParseFixedString8, useInterner: false);
        if (type == typeof(FixedString16) && setterDelegate is not null) return CreateParser<FixedString16>(setterDelegate, ParserHelpers.ParseFixedString16, useInterner: false);
        if (type == typeof(FixedString32) && setterDelegate is not null) return CreateParser<FixedString32>(setterDelegate, ParserHelpers.ParseFixedString32, useInterner: false);
        if (type == typeof(FixedString64) && setterDelegate is not null) return CreateParser<FixedString64>(setterDelegate, ParserHelpers.ParseFixedString64, useInterner: false);
        if (type == typeof(FixedString128) && setterDelegate is not null) return CreateParser<FixedString128>(setterDelegate, ParserHelpers.ParseFixedString128, useInterner: false);

        if (type.IsEnum && setterDelegate is not null)
        {
            MethodInfo createEnumParser = typeof(JsonPopulator<T>)
                .GetMethod(nameof(CreateEnumParser), BindingFlags.Static | BindingFlags.NonPublic)!
                .MakeGenericMethod(type);
            return (PropAction)createEnumParser.Invoke(null, [setterDelegate])!;
        }

        MethodInfo createGetterMethod = typeof(JsonPopulator<T>)
            .GetMethod(nameof(CreateGetterDelegate), BindingFlags.Static | BindingFlags.NonPublic)!
            .MakeGenericMethod(type);
        object getterDelegate = createGetterMethod.Invoke(null, [paramTarget, memberExpr])!;

        if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(List<>))
        {
            Type itemType = type.GetGenericArguments()[0];
            MethodInfo createListParser = typeof(JsonPopulator<T>)
                .GetMethod(nameof(CreateListParser), BindingFlags.Static | BindingFlags.NonPublic)!
                .MakeGenericMethod(itemType);

            return (PropAction)createListParser.Invoke(null, [setterDelegate, getterDelegate])!;
        }

        if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(FastList<>))
        {
            Type itemType = type.GetGenericArguments()[0];
            MethodInfo createFastListParser = typeof(JsonPopulator<T>)
                .GetMethod(nameof(CreateFastListParser), BindingFlags.Static | BindingFlags.NonPublic)!
                .MakeGenericMethod(itemType);

            return (PropAction)createFastListParser.Invoke(null, [getterDelegate])!;
        }

        {
            MethodInfo createComplexParser = typeof(JsonPopulator<T>)
                 .GetMethod(nameof(CreateComplexParser), BindingFlags.Static | BindingFlags.NonPublic)!
                 .MakeGenericMethod(type);

            return (PropAction)createComplexParser.Invoke(null, [setterDelegate, getterDelegate])!;
        }
    }

    private static PropAction CreateEnumParser<TEnum>(object setterObj) where TEnum : struct, Enum
    {
        var setter = (SetterDelegate<TEnum>)setterObj;
        var names = Enum.GetNames<TEnum>();
        var values = Enum.GetValues<TEnum>();
        var lookup = new (byte[] bytes, TEnum value)[names.Length];

        for (int i = 0; i < names.Length; i++)
        {
            var field = typeof(TEnum).GetField(names[i])!;
            var attr = field.GetCustomAttribute<JsonPropertyNameAttribute>();
            string name = attr?.Name ?? names[i];
            lookup[i] = (Encoding.UTF8.GetBytes(name), values[i]);
        }

        return (ref target, ref reader, _) =>
        {
            if (reader.TokenType == JsonTokenType.Number)
            {
                if (reader.TryGetInt32(out int val))
                {
                    setter(ref target, Unsafe.As<int, TEnum>(ref val));
                }
            }
            else if (reader.TokenType == JsonTokenType.String)
            {
                // Try exact match first (handles escaping correctly and supports multi-segment)
                for (int i = 0; i < lookup.Length; i++)
                {
                    if (reader.ValueTextEquals(lookup[i].bytes))
                    {
                        setter(ref target, lookup[i].value);
                        return;
                    }
                }

                // Fallback to case-insensitive or number-as-string
                if (!reader.HasValueSequence)
                {
                    SearchEnumFallback(ref target, reader.ValueSpan, lookup, setter);
                }
                else
                {
                    int len = (int)reader.ValueSequence.Length;
                    if (len <= 128)
                    {
                        Span<byte> buffer = stackalloc byte[len];
                        reader.ValueSequence.CopyTo(buffer);
                        SearchEnumFallback(ref target, buffer, lookup, setter);
                    }
                    else
                    {
                        using var buffer = new PoolRental<byte>((int)reader.ValueSequence.Length);
                        reader.ValueSequence.CopyTo(buffer.Span);
                        SearchEnumFallback(ref target, buffer.Span, lookup, setter);
                    }
                }
            }
        };
    }

    private static void SearchEnumFallback<TEnum>(ref T target, ReadOnlySpan<byte> span, (byte[] bytes, TEnum value)[] lookup, SetterDelegate<TEnum> setter) where TEnum : struct, Enum
    {
        for (int i = 0; i < lookup.Length; i++)
        {
            if (Ascii.EqualsIgnoreCase(span, lookup[i].bytes))
            {
                setter(ref target, lookup[i].value);
                return;
            }
        }

        // Fallback: try parsing as number from string
        if (Utf8Parser.TryParse(span, out int intVal, out int _))
        {
            setter(ref target, Unsafe.As<int, TEnum>(ref intVal));
        }
    }

    private static PropAction CreateListParser<TItem>(object? setterObj, object getterObj)
    {
        var setter = (SetterDelegate<List<TItem>>?)setterObj;
        var getter = (GetterDelegate<List<TItem>>)getterObj;
        ParserDelegate<TItem>? itemParser = GetParserForItem<TItem>();

        return (ref target, ref reader, interner) =>
        {
            if (reader.TokenType != JsonTokenType.StartArray) { reader.Skip(); return; }

            List<TItem>? list = getter(ref target);
            if (list is null)
            {
                if (setter is null) throw new InvalidOperationException($"Cannot populate null List property without setter.");
                list = [];
                setter(ref target, list);
            }
            else
            {
                list.Clear();
            }

            while (reader.Read() && reader.TokenType != JsonTokenType.EndArray)
            {
                if (itemParser is not null)
                {
                    list.Add(itemParser(ref reader, interner));
                }
                else
                {
                    TItem item;
                    if (typeof(TItem).IsValueType)
                    {
                        item = default!;
                        JsonPopulator.Populate(ref reader, ref item, interner);
                    }
                    else
                    {
                        item = Activator.CreateInstance<TItem>();
                        JsonPopulator.Populate(ref reader, ref item, interner);
                    }
                    list.Add(item);
                }
            }
        };
    }

    private static PropAction CreateFastListParser<TItem>(object getterObj)
    {
        var getter = (GetterDelegate<FastList<TItem>>)getterObj;
        ParserDelegate<TItem>? itemParser = GetParserForItem<TItem>();

        return (ref target, ref reader, interner) =>
        {
            if (reader.TokenType != JsonTokenType.StartArray) { reader.Skip(); return; }

            FastList<TItem> list = getter(ref target) ?? throw new InvalidOperationException($"FastList property must be initialized.");
            list.Clear();

            while (reader.Read() && reader.TokenType != JsonTokenType.EndArray)
            {
                ref TItem item = ref list.AddAndGetRef();

                if (itemParser is not null)
                {
                    item = itemParser(ref reader, interner);
                }
                else
                {
                    JsonPopulator.Populate(ref reader, ref item, interner);
                }
            }
        };
    }

    private static PropAction CreateComplexParser<TProp>(object? setterObj, object getterObj)
    {
        var setter = (SetterDelegate<TProp>?)setterObj;
        var getter = (GetterDelegate<TProp>)getterObj;
        bool isValueType = typeof(TProp).IsValueType;

        return (ref target, ref reader, interner) =>
        {
            TProp val = getter(ref target);

            if (!isValueType && val is null)
            {
                val = Activator.CreateInstance<TProp>();
                if (setter is not null) setter(ref target, val);
                else throw new InvalidOperationException($"Cannot create instance for null property without setter. Type: {typeof(TProp).Name}");
            }

            JsonPopulator.Populate(ref reader, ref val, interner);

            if (isValueType && setter is not null)
            {
                setter(ref target, val);
            }
        };
    }

    private static ParserDelegate<TItem>? GetParserForItem<TItem>()
    {
        Type t = typeof(TItem);
        if (t == typeof(double)) return (ParserDelegate<TItem>)(object)new ParserDelegate<double>(ParserHelpers.ParseDouble);
        if (t == typeof(decimal)) return (ParserDelegate<TItem>)(object)new ParserDelegate<decimal>(ParserHelpers.ParseDecimal);
        if (t == typeof(long)) return (ParserDelegate<TItem>)(object)new ParserDelegate<long>(ParserHelpers.ParseLong);
        if (t == typeof(int)) return (ParserDelegate<TItem>)(object)new ParserDelegate<int>(ParserHelpers.ParseInt);
        if (t == typeof(bool)) return (ParserDelegate<TItem>)(object)new ParserDelegate<bool>(ParserHelpers.ParseBool);
        if (t == typeof(string)) return (ParserDelegate<TItem>)(object)new ParserDelegate<string>(ParserHelpers.ParseString);
        if (t == typeof(DateTime)) return (ParserDelegate<TItem>)(object)new ParserDelegate<DateTime>(ParserHelpers.ParseDateTime);
        if (t == typeof(Guid)) return (ParserDelegate<TItem>)(object)new ParserDelegate<Guid>(ParserHelpers.ParseGuid);
        return null;
    }

    private static SetterDelegate<TProp> CreateSetterDelegate<TProp>(ParameterExpression pTarget, ParameterExpression pVal, Expression body)
    {
        return Expression.Lambda<SetterDelegate<TProp>>(body, pTarget, pVal).Compile();
    }

    private static GetterDelegate<TProp> CreateGetterDelegate<TProp>(ParameterExpression pTarget, Expression body)
    {
        return Expression.Lambda<GetterDelegate<TProp>>(body, pTarget).Compile();
    }

    private static PropAction CreateParser<TProp>(object setterObj, ParserDelegate<TProp> parser, bool useInterner)
    {
        var setter = (SetterDelegate<TProp>)setterObj;
        return (ref target, ref reader, interner) =>
        {
            TProp val = parser(ref reader, useInterner ? interner : null);
            setter(ref target, val);
        };
    }

    private delegate TProp ParserDelegate<TProp>(ref Utf8JsonReader reader, StringInternerDelegate? interner);
}

internal static class ParserHelpers
{
    private const long MinUnixTimeSeconds = -62_135_596_800L;
    private const long MaxUnixTimeSeconds = 253_402_300_799L;
    private const long MinUnixTimeMilliseconds = -62_135_596_800_000L;
    private const long MaxUnixTimeMilliseconds = 253_402_300_799_999L;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static double ParseDouble(ref Utf8JsonReader reader, StringInternerDelegate? _)
    {
        if (reader.TokenType == JsonTokenType.Number) return reader.GetDouble();
        if (reader.TokenType == JsonTokenType.String)
        {
            if (!reader.HasValueSequence)
            {
                Utf8Parser.TryParse(reader.ValueSpan, out double val, out int _);
                return val;
            }

            int len = (int)reader.ValueSequence.Length;
            Span<byte> buffer = len <= 128 ? stackalloc byte[len] : new byte[len];
            reader.ValueSequence.CopyTo(buffer);
            Utf8Parser.TryParse(buffer, out double valSeq, out int _);
            return valSeq;
        }
        return 0;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static decimal ParseDecimal(ref Utf8JsonReader reader, StringInternerDelegate? _)
    {
        if (reader.TokenType == JsonTokenType.Number) return reader.GetDecimal();
        if (reader.TokenType == JsonTokenType.String)
        {
            if (!reader.HasValueSequence)
            {
                Utf8Parser.TryParse(reader.ValueSpan, out decimal val, out int _);
                return val;
            }

            int len = (int)reader.ValueSequence.Length;
            Span<byte> buffer = len <= 128 ? stackalloc byte[len] : new byte[len];
            reader.ValueSequence.CopyTo(buffer);
            Utf8Parser.TryParse(buffer, out decimal valSeq, out int _);
            return valSeq;
        }
        return 0;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static long ParseLong(ref Utf8JsonReader reader, StringInternerDelegate? _)
    {
        if (reader.TokenType == JsonTokenType.Number) return reader.GetInt64();
        if (reader.TokenType == JsonTokenType.String)
        {
            if (!reader.HasValueSequence)
            {
                Utf8Parser.TryParse(reader.ValueSpan, out long val, out int _);
                return val;
            }

            int len = (int)reader.ValueSequence.Length;
            Span<byte> buffer = len <= 128 ? stackalloc byte[len] : new byte[len];
            reader.ValueSequence.CopyTo(buffer);
            Utf8Parser.TryParse(buffer, out long valSeq, out int _);
            return valSeq;
        }
        return 0;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int ParseInt(ref Utf8JsonReader reader, StringInternerDelegate? _)
    {
        if (reader.TokenType == JsonTokenType.Number) return reader.GetInt32();
        if (reader.TokenType == JsonTokenType.String)
        {
            if (!reader.HasValueSequence)
            {
                Utf8Parser.TryParse(reader.ValueSpan, out int val, out int _);
                return val;
            }

            int len = (int)reader.ValueSequence.Length;
            Span<byte> buffer = len <= 128 ? stackalloc byte[len] : new byte[len];
            reader.ValueSequence.CopyTo(buffer);
            Utf8Parser.TryParse(buffer, out int valSeq, out int _);
            return valSeq;
        }
        return 0;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool ParseBool(ref Utf8JsonReader reader, StringInternerDelegate? _)
    {
        if (reader.TokenType == JsonTokenType.True) return true;
        if (reader.TokenType == JsonTokenType.False) return false;
        if (reader.TokenType == JsonTokenType.String)
        {
            return reader.ValueTextEquals("true"u8);
        }
        return false;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static string ParseString(ref Utf8JsonReader reader, StringInternerDelegate? interner)
    {
        return AbstractParser.GetStringInterned(ref reader, interner) ?? string.Empty;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static FixedString8 ParseFixedString8(ref Utf8JsonReader reader, StringInternerDelegate? _)
    {
        var result = new FixedString8();
        if (reader.TokenType == JsonTokenType.String)
        {
            if (!reader.HasValueSequence)
            {
                result.Set(reader.ValueSpan);
            }
            else
            {
                int len = (int)reader.ValueSequence.Length;
                if (len <= FixedString8.Capacity)
                {
                    Span<byte> buffer = stackalloc byte[len];
                    reader.ValueSequence.CopyTo(buffer);
                    result.Set(buffer);
                }
                else
                {
                    Span<byte> buffer = stackalloc byte[FixedString8.Capacity];
                    reader.ValueSequence.Slice(0, FixedString8.Capacity).CopyTo(buffer);
                    result.Set(buffer);
                }
            }
        }
        return result;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static FixedString16 ParseFixedString16(ref Utf8JsonReader reader, StringInternerDelegate? _)
    {
        var result = new FixedString16();
        if (reader.TokenType == JsonTokenType.String)
        {
            if (!reader.HasValueSequence)
            {
                result.Set(reader.ValueSpan);
            }
            else
            {
                int len = (int)reader.ValueSequence.Length;
                if (len <= FixedString16.Capacity)
                {
                    Span<byte> buffer = stackalloc byte[len];
                    reader.ValueSequence.CopyTo(buffer);
                    result.Set(buffer);
                }
                else
                {
                    Span<byte> buffer = stackalloc byte[FixedString16.Capacity];
                    reader.ValueSequence.Slice(0, FixedString16.Capacity).CopyTo(buffer);
                    result.Set(buffer);
                }
            }
        }
        return result;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static FixedString32 ParseFixedString32(ref Utf8JsonReader reader, StringInternerDelegate? _)
    {
        var result = new FixedString32();
        if (reader.TokenType == JsonTokenType.String)
        {
            if (!reader.HasValueSequence)
            {
                result.Set(reader.ValueSpan);
            }
            else
            {
                int len = (int)reader.ValueSequence.Length;
                if (len <= FixedString32.Capacity)
                {
                    Span<byte> buffer = stackalloc byte[len];
                    reader.ValueSequence.CopyTo(buffer);
                    result.Set(buffer);
                }
                else
                {
                    Span<byte> buffer = stackalloc byte[FixedString32.Capacity];
                    reader.ValueSequence.Slice(0, FixedString32.Capacity).CopyTo(buffer);
                    result.Set(buffer);
                }
            }
        }
        return result;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static FixedString64 ParseFixedString64(ref Utf8JsonReader reader, StringInternerDelegate? _)
    {
        var result = new FixedString64();
        if (reader.TokenType == JsonTokenType.String)
        {
            if (!reader.HasValueSequence)
            {
                result.Set(reader.ValueSpan);
            }
            else
            {
                int len = (int)reader.ValueSequence.Length;
                if (len <= 128)
                {
                    Span<byte> buffer = stackalloc byte[len];
                    reader.ValueSequence.CopyTo(buffer);
                    result.Set(buffer);
                }
                else
                {
                    Span<byte> buffer = stackalloc byte[FixedString64.Capacity];
                    reader.ValueSequence.Slice(0, FixedString64.Capacity).CopyTo(buffer);
                    result.Set(buffer);
                }
            }
        }
        return result;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static FixedString128 ParseFixedString128(ref Utf8JsonReader reader, StringInternerDelegate? _)
    {
        var result = new FixedString128();
        if (reader.TokenType == JsonTokenType.String)
        {
            if (!reader.HasValueSequence)
            {
                result.Set(reader.ValueSpan);
            }
            else
            {
                int len = (int)reader.ValueSequence.Length;
                if (len <= FixedString128.Capacity)
                {
                    Span<byte> buffer = len <= 256 ? stackalloc byte[len] : new byte[len];
                    reader.ValueSequence.CopyTo(buffer);
                    result.Set(buffer);
                }
                else
                {
                    Span<byte> buffer = stackalloc byte[FixedString128.Capacity];
                    reader.ValueSequence.Slice(0, FixedString128.Capacity).CopyTo(buffer);
                    result.Set(buffer);
                }
            }
        }
        return result;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static DateTime ParseDateTime(ref Utf8JsonReader reader, StringInternerDelegate? _)
    {
        if (reader.TokenType == JsonTokenType.Number)
        {
            if (reader.TryGetInt64(out long val))
            {
                return ParseUnixTimestamp(val);
            }
        }
        else if (reader.TokenType == JsonTokenType.String)
        {
            if (reader.TryGetDateTime(out DateTime dt))
                return dt;

            if (!reader.HasValueSequence)
            {
                return ParseHighPrecisionTimestamp(reader.ValueSpan);
            }

            int len = (int)reader.ValueSequence.Length;
            Span<byte> buffer = len <= 128 ? stackalloc byte[len] : new byte[len];
            reader.ValueSequence.CopyTo(buffer);
            return ParseHighPrecisionTimestamp(buffer);
        }
        return default;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static DateTime ParseUnixTimestamp(long value)
    {
        // Heuristic: If < 10 billion, it's likely seconds (valid until 2286).
        // Otherwise, it's milliseconds.
        if (value < 10_000_000_000L)
        {
            return value >= MinUnixTimeSeconds && value <= MaxUnixTimeSeconds
                ? DateTimeOffset.FromUnixTimeSeconds(value).UtcDateTime
                : default;
        }

        return value >= MinUnixTimeMilliseconds && value <= MaxUnixTimeMilliseconds
            ? DateTimeOffset.FromUnixTimeMilliseconds(value).UtcDateTime
            : default;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static DateTime ParseHighPrecisionTimestamp(ReadOnlySpan<byte> value)
    {
        try
        {
            return global::GapSonar.Core.Shared.Shared.ParseHighPrecisionTimestamp(value);
        }
        catch (ArgumentOutOfRangeException)
        {
            return default;
        }
        catch (OverflowException)
        {
            return default;
        }
        catch (FormatException)
        {
            return default;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Guid ParseGuid(ref Utf8JsonReader reader, StringInternerDelegate? _)
    {
        if (reader.TokenType == JsonTokenType.String)
        {
            if (reader.TryGetGuid(out Guid guid))
                return guid;

            if (!reader.HasValueSequence)
            {
                if (Utf8Parser.TryParse(reader.ValueSpan, out Guid val, out int _))
                    return val;
            }
            else
            {
                int len = (int)reader.ValueSequence.Length;
                Span<byte> buffer = len <= 128 ? stackalloc byte[len] : new byte[len];
                reader.ValueSequence.CopyTo(buffer);
                if (Utf8Parser.TryParse(buffer, out Guid valSeq, out int _))
                    return valSeq;
            }
        }
        return Guid.Empty;
    }
}

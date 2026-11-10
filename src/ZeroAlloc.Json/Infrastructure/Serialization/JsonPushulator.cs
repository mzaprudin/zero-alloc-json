#pragma warning disable MA0048

using System.Buffers;
using System.Buffers.Text;
using System.Collections;
using System.Globalization;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace GapSonar.Core.Shared.Infrastructure.Serialization;

/// <summary>
/// High-performance, zero-allocation declarative serializer for HFT.
/// Compiles a single monolithic method per type to avoid loops and delegate overhead.
/// Supports nested objects and FastList/List collections.
/// </summary>
public static class JsonPushulator
{
    /// <summary>
    /// Writes the target's members (excluding [RequestHeader]) as a JSON object to the writer.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Push<T>(in T target, Utf8JsonWriter writer)
    {
        JsonPushulatorInner<T>.Push(in target, writer);
    }

    /// <summary>
    /// Writes the target as an element in a collection (no WriteStartObject/WriteEndObject).
    /// Used for nested objects and collection items.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static void PushRaw<T>(in T target, Utf8JsonWriter writer)
    {
        JsonPushulatorInner<T>.PushRaw(in target, writer);
    }

    /// <summary>
    /// Collects members marked with [RequestHeader] into the provided collector.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void CollectHeaders<T>(in T target, IHeaderCollector collector)
    {
        JsonPushulatorInner<T>.CollectHeaders(in target, collector);
    }
}

/// <summary>
/// Interface for receiving header name/value pairs without mandatory string allocations for values.
/// </summary>
public interface IHeaderCollector
{
    void AddHeader(string name, ReadOnlySpan<byte> valueUtf8);
    void AddHeader(string name, string value);
}

internal static class JsonPushulatorInner<T>
{
    private delegate void PushDelegate(in T target, Utf8JsonWriter writer);
    private delegate void HeaderDelegate(in T target, IHeaderCollector collector);

    private static readonly PushDelegate? _pushDelegate;
    private static readonly PushDelegate? _pushRawDelegate;
    private static readonly HeaderDelegate? _headerDelegate;

    static JsonPushulatorInner()
    {
        var type = typeof(T);
        var targetParam = Expression.Parameter(type.MakeByRefType(), "target");
        var writerParam = Expression.Parameter(typeof(Utf8JsonWriter), "writer");
        var collectorParam = Expression.Parameter(typeof(IHeaderCollector), "collector");

        var allMembers = type.GetMembers(BindingFlags.Public | BindingFlags.Instance)
            .Where(static m =>
            {
                if (m is not (PropertyInfo or FieldInfo)) return false;
                var attr = m.GetCustomAttribute<JsonIgnoreAttribute>();
                return attr == null || attr.Condition != JsonIgnoreCondition.Always;
            })
            .ToArray();

        _pushDelegate = CompilePush(allMembers, targetParam, writerParam, wrapInObject: true);
        _pushRawDelegate = CompilePush(allMembers, targetParam, writerParam, wrapInObject: false);
        _headerDelegate = CompileHeaders(allMembers, targetParam, collectorParam);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Push(in T target, Utf8JsonWriter writer) => _pushDelegate?.Invoke(in target, writer);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void PushRaw(in T target, Utf8JsonWriter writer) => _pushRawDelegate?.Invoke(in target, writer);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void CollectHeaders(in T target, IHeaderCollector collector) => _headerDelegate?.Invoke(in target, collector);

    private static PushDelegate? CompilePush(MemberInfo[] members, ParameterExpression target, ParameterExpression writer, bool wrapInObject)
    {
        int bodyCount = 0;
        for (int i = 0; i < members.Length; i++)
        {
            if (!Attribute.IsDefined(members[i], typeof(RequestHeaderAttribute))) bodyCount++;
        }

        if (bodyCount == 0 && wrapInObject) return null;

        var asArray = Attribute.IsDefined(typeof(T), typeof(JsonAsArrayAttribute));
        var exprList = new List<Expression>(bodyCount + 2);

        if (wrapInObject)
        {
            var startMethod = asArray
                ? typeof(Utf8JsonWriter).GetMethod(nameof(Utf8JsonWriter.WriteStartArray), Type.EmptyTypes)!
                : typeof(Utf8JsonWriter).GetMethod(nameof(Utf8JsonWriter.WriteStartObject), Type.EmptyTypes)!;
            exprList.Add(Expression.Call(writer, startMethod));
        }

        for (int i = 0; i < members.Length; i++)
        {
            var member = members[i];
            if (Attribute.IsDefined(member, typeof(RequestHeaderAttribute))) continue;

            var memberType = member switch { PropertyInfo p => p.PropertyType, FieldInfo f => f.FieldType, _ => null! };
            var memberExpr = Expression.MakeMemberAccess(target, member);

            Expression pushExpr;
            if (asArray)
            {
                pushExpr = CreateValuePushExpr(writer, memberExpr, memberType, member);
            }
            else
            {
                var jsonAttr = member.GetCustomAttribute<JsonPropertyNameAttribute>();
                var name = jsonAttr?.Name ?? member.Name;
                var nameBytes = Encoding.UTF8.GetBytes(name);
                pushExpr = CreateMemberPushExpr(writer, nameBytes, memberExpr, memberType, member);
            }

            // --- OPTIMIZED CONDITION WRAPPING ---
            var ignoreAttr = member.GetCustomAttribute<JsonIgnoreAttribute>();
            bool shouldCheckDefault = ignoreAttr != null && ignoreAttr.Condition == JsonIgnoreCondition.WhenWritingDefault;
            bool isNullable = Nullable.GetUnderlyingType(memberType) != null;
            bool isReference = !memberType.IsValueType;

            if (shouldCheckDefault || isNullable || isReference)
            {
                Expression condition;
                if (isNullable)
                {
                    condition = Expression.Property(memberExpr, "HasValue");
                }
                else if (isReference)
                {
                    condition = Expression.NotEqual(memberExpr, Expression.Constant(null, memberType));
                }
                else // isValueType and shouldCheckDefault
                {
                    condition = Expression.NotEqual(memberExpr, Expression.Default(memberType));
                }
                pushExpr = Expression.IfThen(condition, pushExpr);
            }

            exprList.Add(pushExpr);
        }

        if (wrapInObject)
        {
            var endMethod = asArray
                ? typeof(Utf8JsonWriter).GetMethod(nameof(Utf8JsonWriter.WriteEndArray), Type.EmptyTypes)!
                : typeof(Utf8JsonWriter).GetMethod(nameof(Utf8JsonWriter.WriteEndObject), Type.EmptyTypes)!;
            exprList.Add(Expression.Call(writer, endMethod));
        }

        if (exprList.Count == 0) return null;

        return Expression.Lambda<PushDelegate>(Expression.Block(exprList), target, writer).Compile();
    }

    private static Expression CreateValuePushExpr(ParameterExpression writer, Expression val, Type type, MemberInfo member)
    {
        var toString = Attribute.IsDefined(member, typeof(JsonToStringAttribute));
        var toMs = Attribute.IsDefined(member, typeof(JsonToMsAttribute));
        var toStringMs = Attribute.IsDefined(member, typeof(JsonToStringMsAttribute));

        // Handle Nullable (unwrap value, condition handled in CompilePush)
        var underlyingType = Nullable.GetUnderlyingType(type);
        if (underlyingType != null)
        {
            return CreateValuePushExpr(writer, Expression.Property(val, "Value"), underlyingType, member);
        }

        // 1. FixedString
        if (typeof(IFixedString).IsAssignableFrom(type))
        {
            return Expression.Call(typeof(PushulatorHelper).GetMethod(nameof(PushulatorHelper.WriteFixedStringValue))!.MakeGenericMethod(type),
                writer, val);
        }

        // 2. SymbolId
        if (type == typeof(SymbolId))
        {
            return Expression.Call(typeof(PushulatorHelper).GetMethod(nameof(PushulatorHelper.WriteSymbolIdValue))!,
                writer, val);
        }

        // 3. Transformations
        if (toString)
        {
            return Expression.Call(typeof(PushulatorHelper).GetMethod(nameof(PushulatorHelper.WriteValueAsStringValue))!.MakeGenericMethod(type), writer, val);
        }

        if (type == typeof(DateTime))
        {
            if (toStringMs) return Expression.Call(typeof(PushulatorHelper).GetMethod(nameof(PushulatorHelper.WriteDateTimeToStringMsValue))!, writer, val);
            if (toMs) return Expression.Call(typeof(PushulatorHelper).GetMethod(nameof(PushulatorHelper.WriteDateTimeToLongMsValue))!, writer, val);
            return Expression.Call(writer, typeof(Utf8JsonWriter).GetMethod(nameof(Utf8JsonWriter.WriteStringValue), [typeof(DateTime)])!, val);
        }

        // 4. Standard Primitives
        MethodInfo? method = null;
        if (type == typeof(double)) method = typeof(Utf8JsonWriter).GetMethod(nameof(Utf8JsonWriter.WriteNumberValue), [typeof(double)]);
        else if (type == typeof(decimal)) method = typeof(Utf8JsonWriter).GetMethod(nameof(Utf8JsonWriter.WriteNumberValue), [typeof(decimal)]);
        else if (type == typeof(long)) method = typeof(Utf8JsonWriter).GetMethod(nameof(Utf8JsonWriter.WriteNumberValue), [typeof(long)]);
        else if (type == typeof(int)) method = typeof(Utf8JsonWriter).GetMethod(nameof(Utf8JsonWriter.WriteNumberValue), [typeof(int)]);
        else if (type == typeof(bool)) method = typeof(Utf8JsonWriter).GetMethod(nameof(Utf8JsonWriter.WriteBooleanValue), [typeof(bool)]);
        else if (type == typeof(string)) method = typeof(Utf8JsonWriter).GetMethod(nameof(Utf8JsonWriter.WriteStringValue), [typeof(string)]);
        else if (type.IsEnum)
        {
            return Expression.Call(typeof(PushulatorHelper).GetMethod(nameof(PushulatorHelper.WriteEnumAsStringValue))!.MakeGenericMethod(type), writer, val);
        }

        if (method is not null) return Expression.Call(writer, method, val);

        // 5. Nested Object
        if (type.IsClass || (type.IsValueType && !type.IsPrimitive))
        {
            return Expression.Call(typeof(PushulatorHelper).GetMethod(nameof(PushulatorHelper.WriteRawObject))!.MakeGenericMethod(type), writer, val);
        }

        return Expression.Empty();
    }

    private static HeaderDelegate? CompileHeaders(MemberInfo[] members, ParameterExpression target, ParameterExpression collector)
    {
        int headerCount = 0;
        for (int i = 0; i < members.Length; i++)
        {
            if (Attribute.IsDefined(members[i], typeof(RequestHeaderAttribute))) headerCount++;
        }

        if (headerCount == 0) return null;

        var exprs = new Expression[headerCount];
        int idx = 0;
        for (int i = 0; i < members.Length; i++)
        {
            var member = members[i];
            var attr = member.GetCustomAttribute<RequestHeaderAttribute>();
            if (attr is null) continue;

            var memberType = member switch { PropertyInfo p => p.PropertyType, FieldInfo f => f.FieldType, _ => null! };
            var memberExpr = Expression.MakeMemberAccess(target, member);

            if (typeof(IFixedString).IsAssignableFrom(memberType))
            {
                exprs[idx++] = Expression.Call(collector,
                    typeof(IHeaderCollector).GetMethod(nameof(IHeaderCollector.AddHeader), [typeof(string), typeof(ReadOnlySpan<byte>)])!,
                    Expression.Constant(attr.Name),
                    Expression.Property(memberExpr, memberType.GetProperty(nameof(IFixedString.ReadOnlySpan))!));
            }
            else if (memberType == typeof(string))
            {
                exprs[idx++] = Expression.Call(collector,
                    typeof(IHeaderCollector).GetMethod(nameof(IHeaderCollector.AddHeader), [typeof(string), typeof(string)])!,
                    Expression.Constant(attr.Name),
                    memberExpr);
            }
            else
            {
                var helper = typeof(PushulatorHelper).GetMethod(nameof(PushulatorHelper.AddHeaderConverted))!.MakeGenericMethod(memberType);
                exprs[idx++] = Expression.Call(helper, collector, Expression.Constant(attr.Name), memberExpr);
            }
        }

        return Expression.Lambda<HeaderDelegate>(Expression.Block(exprs), target, collector).Compile();
    }

    private static Expression CreateMemberPushExpr(ParameterExpression writer, byte[] name, Expression val, Type type, MemberInfo member)
    {
        var toString = Attribute.IsDefined(member, typeof(JsonToStringAttribute));
        var toMs = Attribute.IsDefined(member, typeof(JsonToMsAttribute));
        var toStringMs = Attribute.IsDefined(member, typeof(JsonToStringMsAttribute));
        var isInlineArray = Attribute.IsDefined(member, typeof(JsonInlineArrayAttribute));

        var nameExpr = Expression.Call(typeof(PushulatorHelper).GetMethod(nameof(PushulatorHelper.ToSpan))!, Expression.Constant(name));

        // Handle Nullable (unwrap value, condition handled in CompilePush)
        var underlyingType = Nullable.GetUnderlyingType(type);
        if (underlyingType != null)
        {
            return CreateMemberPushExpr(writer, name, Expression.Property(val, "Value"), underlyingType, member);
        }

        // 0. Handle Inline Array Wrapper
        if (isInlineArray)
        {
            var startArray = Expression.Call(writer, typeof(Utf8JsonWriter).GetMethod(nameof(Utf8JsonWriter.WriteStartArray), [typeof(ReadOnlySpan<byte>)])!, nameExpr);
            var writeItem = Expression.Call(typeof(PushulatorHelper).GetMethod(nameof(PushulatorHelper.WriteNestedObjectNoName))!.MakeGenericMethod(type), writer, val);
            var endArray = Expression.Call(writer, typeof(Utf8JsonWriter).GetMethod(nameof(Utf8JsonWriter.WriteEndArray), Type.EmptyTypes)!);
            return Expression.Block(startArray, writeItem, endArray);
        }

        // 1. FixedString
        if (typeof(IFixedString).IsAssignableFrom(type))
        {
            return Expression.Call(typeof(PushulatorHelper).GetMethod(nameof(PushulatorHelper.WriteFixedString))!.MakeGenericMethod(type),
                writer, nameExpr, val);
        }

        // 2. SymbolId
        if (type == typeof(SymbolId))
        {
            return Expression.Call(typeof(PushulatorHelper).GetMethod(nameof(PushulatorHelper.WriteSymbolId))!,
                writer, nameExpr, val);
        }

        // 3. Transformations
        if (toString)
        {
            return Expression.Call(typeof(PushulatorHelper).GetMethod(nameof(PushulatorHelper.WriteValueAsString))!.MakeGenericMethod(type), writer, nameExpr, val);
        }

        if (type == typeof(DateTime))
        {
            if (toStringMs) return Expression.Call(typeof(PushulatorHelper).GetMethod(nameof(PushulatorHelper.WriteDateTimeToStringMs))!, writer, nameExpr, val);
            if (toMs) return Expression.Call(typeof(PushulatorHelper).GetMethod(nameof(PushulatorHelper.WriteDateTimeToLongMs))!, writer, nameExpr, val);
            return Expression.Call(writer, typeof(Utf8JsonWriter).GetMethod(nameof(Utf8JsonWriter.WriteString), [typeof(ReadOnlySpan<byte>), typeof(DateTime)])!, nameExpr, val);
        }

        // 4. Collections (FastList / List)
        if (IsCollection(type, out var itemType))
        {
            var pushMethod = typeof(PushulatorHelper).GetMethod(nameof(PushulatorHelper.WriteCollection))!.MakeGenericMethod(type, itemType);
            return Expression.Call(pushMethod, writer, nameExpr, val);
        }

        // 5. Standard Primitives
        MethodInfo? method = null;
        if (type == typeof(double)) method = typeof(Utf8JsonWriter).GetMethod(nameof(Utf8JsonWriter.WriteNumber), [typeof(ReadOnlySpan<byte>), typeof(double)]);
        else if (type == typeof(decimal)) method = typeof(Utf8JsonWriter).GetMethod(nameof(Utf8JsonWriter.WriteNumber), [typeof(ReadOnlySpan<byte>), typeof(decimal)]);
        else if (type == typeof(long)) method = typeof(Utf8JsonWriter).GetMethod(nameof(Utf8JsonWriter.WriteNumber), [typeof(ReadOnlySpan<byte>), typeof(long)]);
        else if (type == typeof(int)) method = typeof(Utf8JsonWriter).GetMethod(nameof(Utf8JsonWriter.WriteNumber), [typeof(ReadOnlySpan<byte>), typeof(int)]);
        else if (type == typeof(bool)) method = typeof(Utf8JsonWriter).GetMethod(nameof(Utf8JsonWriter.WriteBoolean), [typeof(ReadOnlySpan<byte>), typeof(bool)]);
        else if (type == typeof(string)) method = typeof(Utf8JsonWriter).GetMethod(nameof(Utf8JsonWriter.WriteString), [typeof(ReadOnlySpan<byte>), typeof(string)]);
        else if (type.IsEnum)
        {
            return Expression.Call(typeof(PushulatorHelper).GetMethod(nameof(PushulatorHelper.WriteEnumAsString))!.MakeGenericMethod(type), writer, nameExpr, val);
        }

        if (method is not null) return Expression.Call(writer, method, nameExpr, val);

        // 6. Nested Object
        if (type.IsClass || (type.IsValueType && !type.IsPrimitive))
        {
            var pushNestedMethod = typeof(PushulatorHelper).GetMethod(nameof(PushulatorHelper.WriteNestedObject))!.MakeGenericMethod(type);
            return Expression.Call(pushNestedMethod, writer, nameExpr, val);
        }

        return Expression.Empty();
    }

    private static bool IsCollection(Type type, out Type itemType)
    {
        itemType = null!;
        if (!type.IsGenericType) return false;

        var def = type.GetGenericTypeDefinition();
        if (def == typeof(FastList<>) || def == typeof(List<>))
        {
            itemType = type.GetGenericArguments()[0];
            return true;
        }
        return false;
    }
}

internal static class PushulatorHelper
{
    [ThreadStatic] private static byte[]? _formatBuffer;
    private static byte[] GetBuffer() => _formatBuffer ??= new byte[128];

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ReadOnlySpan<byte> ToSpan(byte[] data) => data;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void WriteFixedString<TFS>(Utf8JsonWriter writer, ReadOnlySpan<byte> name, TFS value) where TFS : IFixedString
    {
        writer.WriteString(name, value.ReadOnlySpan);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void WriteFixedStringValue<TFS>(Utf8JsonWriter writer, TFS value) where TFS : IFixedString
    {
        writer.WriteStringValue(value.ReadOnlySpan);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void WriteSymbolId(Utf8JsonWriter writer, ReadOnlySpan<byte> name, SymbolId value)
    {
        writer.WriteString(name, value.NativeSymbol);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void WriteSymbolIdValue(Utf8JsonWriter writer, SymbolId value)
    {
        writer.WriteStringValue(value.NativeSymbol);
    }

    public static void WriteValueAsString<TVal>(Utf8JsonWriter writer, ReadOnlySpan<byte> name, TVal value)
    {
        var buf = GetBuffer();
        int written = 0;
        bool ok = false;
        if (value is double d) ok = Utf8Formatter.TryFormat(d, buf, out written);
        else if (value is decimal dec) ok = Utf8Formatter.TryFormat(dec, buf, out written);
        else if (value is long l) ok = Utf8Formatter.TryFormat(l, buf, out written);
        else if (value is int i) ok = Utf8Formatter.TryFormat(i, buf, out written);
        else if (value is bool b) { writer.WriteString(name, b ? "true"u8 : "false"u8); return; }

        if (ok) writer.WriteString(name, buf.AsSpan(0, written));
        else writer.WriteString(name, value?.ToString() ?? string.Empty);
    }

    public static void WriteValueAsStringValue<TVal>(Utf8JsonWriter writer, TVal value)
    {
        var buf = GetBuffer();
        int written = 0;
        bool ok = false;
        if (value is double d) ok = Utf8Formatter.TryFormat(d, buf, out written);
        else if (value is decimal dec) ok = Utf8Formatter.TryFormat(dec, buf, out written);
        else if (value is long l) ok = Utf8Formatter.TryFormat(l, buf, out written);
        else if (value is int i) ok = Utf8Formatter.TryFormat(i, buf, out written);
        else if (value is bool b) { writer.WriteStringValue(b ? "true"u8 : "false"u8); return; }

        if (ok) writer.WriteStringValue(buf.AsSpan(0, written));
        else writer.WriteStringValue(value?.ToString() ?? string.Empty);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void WriteDateTimeToLongMs(Utf8JsonWriter writer, ReadOnlySpan<byte> name, DateTime val)
        => writer.WriteNumber(name, new DateTimeOffset(val).ToUnixTimeMilliseconds());

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void WriteDateTimeToLongMsValue(Utf8JsonWriter writer, DateTime val)
        => writer.WriteNumberValue(new DateTimeOffset(val).ToUnixTimeMilliseconds());

    public static void WriteDateTimeToStringMs(Utf8JsonWriter writer, ReadOnlySpan<byte> name, DateTime val)
    {
        var ms = new DateTimeOffset(val).ToUnixTimeMilliseconds();
        var buf = GetBuffer();
        if (Utf8Formatter.TryFormat(ms, buf, out int written)) writer.WriteString(name, buf.AsSpan(0, written));
    }

    public static void WriteDateTimeToStringMsValue(Utf8JsonWriter writer, DateTime val)
    {
        var ms = new DateTimeOffset(val).ToUnixTimeMilliseconds();
        var buf = GetBuffer();
        if (Utf8Formatter.TryFormat(ms, buf, out int written)) writer.WriteStringValue(buf.AsSpan(0, written));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void WriteEnumAsString<TEnum>(Utf8JsonWriter writer, ReadOnlySpan<byte> name, TEnum val) where TEnum : struct, Enum
    {
        writer.WriteString(name, EnumNameCache<TEnum>.GetName(val));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void WriteEnumAsStringValue<TEnum>(Utf8JsonWriter writer, TEnum val) where TEnum : struct, Enum
    {
        writer.WriteStringValue(EnumNameCache<TEnum>.GetName(val));
    }

    public static void WriteRawObject<TVal>(Utf8JsonWriter writer, TVal value)
    {
        if (value is null) { writer.WriteNullValue(); return; }
        JsonPushulator.PushRaw(value, writer);
    }

    public static void WriteNestedObjectNoName<TVal>(Utf8JsonWriter writer, TVal value)
    {
        if (value is null) { writer.WriteNullValue(); return; }
        JsonPushulator.Push(value, writer);
    }

    private static class EnumNameCache<TEnum> where TEnum : struct, Enum
    {
        private static readonly byte[][] _cache;
        private static readonly int _min;
        private static readonly int _max;

        static EnumNameCache()
        {
            var values = Enum.GetValues<TEnum>();
            if (values.Length == 0)
            {
                _cache = [];
                return;
            }

            _min = values.Min(v => Convert.ToInt32(v, CultureInfo.InvariantCulture));
            _max = values.Max(v => Convert.ToInt32(v, CultureInfo.InvariantCulture));

            var range = _max - _min + 1;
            _cache = new byte[range][];

            foreach (var v in values)
            {
                var valInt = Convert.ToInt32(v, CultureInfo.InvariantCulture);
                var name = v.ToString()!;
                var field = typeof(TEnum).GetField(name);
                var attr = field?.GetCustomAttribute<JsonPropertyNameAttribute>();
                if (attr is not null) name = attr.Name;

                _cache[valInt - _min] = Encoding.UTF8.GetBytes(name);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ReadOnlySpan<byte> GetName(TEnum value)
        {
            int valInt;
            if (Unsafe.SizeOf<TEnum>() == 1) valInt = Unsafe.As<TEnum, byte>(ref value);
            else if (Unsafe.SizeOf<TEnum>() == 2) valInt = (int)Unsafe.As<TEnum, short>(ref value);
            else valInt = Unsafe.As<TEnum, int>(ref value);

            if (valInt < _min || valInt > _max) return default;
            return _cache[valInt - _min];
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void WriteNestedObject<TVal>(Utf8JsonWriter writer, ReadOnlySpan<byte> name, TVal value)
    {
        if (value is null) { writer.WriteNull(name); return; }
        writer.WritePropertyName(name);
        JsonPushulator.Push(value, writer);
    }

    public static void WriteCollection<TColl, TItem>(Utf8JsonWriter writer, ReadOnlySpan<byte> name, TColl value) where TColl : IEnumerable<TItem>
    {
        if (value is null) { writer.WriteNull(name); return; }
        writer.WriteStartArray(name);
        if (value is FastList<TItem> fastList)
        {
            for (int i = 0; i < fastList.Count; i++)
            {
                WriteItem(fastList[i], writer);
            }
        }
        else
        {
            foreach (var item in value)
            {
                WriteItem(item, writer);
            }
        }
        writer.WriteEndArray();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void WriteItem<TItem>(in TItem item, Utf8JsonWriter writer)
    {
        if (item is null) { writer.WriteNullValue(); return; }

        var type = typeof(TItem);
        if (type == typeof(double)) writer.WriteNumberValue((double)(object)item);
        else if (type == typeof(decimal)) writer.WriteNumberValue((decimal)(object)item);
        else if (type == typeof(long)) writer.WriteNumberValue((long)(object)item);
        else if (type == typeof(int)) writer.WriteNumberValue((int)(object)item);
        else if (type == typeof(bool)) writer.WriteBooleanValue((bool)(object)item);
        else if (type == typeof(string)) writer.WriteStringValue((string)(object)item);
        else if (typeof(IFixedString).IsAssignableFrom(type)) writer.WriteStringValue(((IFixedString)item).ReadOnlySpan);
        else if (type == typeof(SymbolId)) writer.WriteStringValue(((SymbolId)(object)item).NativeSymbol);
        else
        {
            // Use Push (not PushRaw) so that nested objects/arrays are correctly wrapped in {} or []
            JsonPushulator.Push(item, writer);
        }
    }

    /// <summary>
    /// Generic fallback converter used by the compiled header pipeline for non-string,
    /// non-<see cref="IFixedString"/> header members. Invariant-culture string formatting is used
    /// so signatures and recv-window values are stable across locales.
    /// <para>
    /// <b>DateTime convention:</b> the captured <see cref="DateTime"/> value is converted to Unix
    /// milliseconds via <c>(DateTimeOffset)dt</c>. Project convention is UTC-only timestamps; a
    /// <see cref="DateTime"/> with <see cref="DateTimeKind.Unspecified"/> is interpreted by
    /// <see cref="DateTimeOffset"/> using the local time zone, which would skew the emitted
    /// epoch. Callers must construct headers with <see cref="DateTimeKind.Utc"/>.
    /// </para>
    /// </summary>
    public static void AddHeaderConverted<TVal>(IHeaderCollector collector, string name, TVal value)
    {
        if (value is null) return;
        if (value is DateTime dt) collector.AddHeader(name, ((DateTimeOffset)dt).ToUnixTimeMilliseconds().ToString(CultureInfo.InvariantCulture));
        else collector.AddHeader(name, value.ToString() ?? string.Empty);
    }
}

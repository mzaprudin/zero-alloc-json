#pragma warning disable MA0048

using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

namespace GapSonar.Core.Shared.Infrastructure.Serialization;

/// <summary>
/// Interface for fixed-size string structures using inline arrays.
/// Provides unified access for zero-allocation JSON parsing and direct buffer access.
/// </summary>
public interface IFixedString
{
    /// <summary>
    /// Maximum capacity in bytes.
    /// </summary>
    static abstract int Capacity { get; }

    /// <summary>
    /// Gets the span of valid bytes.
    /// </summary>
    Span<byte> Span { get; }

    /// <summary>
    /// Gets the read-only span of valid bytes.
    /// </summary>
    ReadOnlySpan<byte> ReadOnlySpan { get; }

    /// <summary>
    /// Gets the raw buffer span of full capacity, regardless of current length.
    /// Useful for direct formatting into the buffer.
    /// </summary>
    Span<byte> RawSpan { get; }

    /// <summary>
    /// Returns true if the string is empty.
    /// </summary>
    bool IsEmpty { get; }

    /// <summary>
    /// Returns the length of the string.
    /// </summary>
    int Length { get; }


    /// <summary>
    /// Sets the string from UTF-8 bytes.
    /// Truncates if input exceeds capacity.
    /// </summary>
    void Set(ReadOnlySpan<byte> utf8);

    /// <summary>
    /// Sets the string from UTF-8 bytes.
    /// Truncates if input exceeds capacity.
    /// </summary>
    void Set(string value);

    /// <summary>
    /// Manually sets the length of valid data in the buffer.
    /// </summary>
    void SetLength(int length);

    /// <summary>
    /// Clears the string.
    /// </summary>
    void Clear();

    /// <summary>
    /// Returns the string representation.
    /// Note: This allocates - use only for logging/debugging.
    /// </summary>
    string ToString();

    /// <summary>
    /// Tries to parse the string as a long.
    /// </summary>
    bool TryParseLong(out long result);

    /// <summary>
    /// Tries to parse the string as a double.
    /// </summary>
    bool TryParseDouble(out double result);
}

/// <summary>
/// Constants for fixed string size limits.
/// </summary>
public static class FixedStringSizes
{
    public const int Size8 = 8;
    public const int Size16 = 16;
    public const int Size32 = 32;
    public const int Size64 = 64;
    public const int Size128 = 128;
}

[InlineArray(FixedStringSizes.Size8)] internal struct FixedString8Buffer { private byte _element0; }
[InlineArray(FixedStringSizes.Size16)] internal struct FixedString16Buffer { private byte _element0; }
[InlineArray(FixedStringSizes.Size32)] internal struct FixedString32Buffer { private byte _element0; }
[InlineArray(FixedStringSizes.Size64)] internal struct FixedString64Buffer { private byte _element0; }
[InlineArray(FixedStringSizes.Size128)] internal struct FixedString128Buffer { private byte _element0; }

[StructLayout(LayoutKind.Auto)]
public struct FixedString8 : IFixedString, IEquatable<FixedString8>
{
    private FixedString8Buffer _data;
    private byte _length;

    public FixedString8(string value) => Set(value);
    public FixedString8(ReadOnlySpan<byte> utf8) => Set(utf8);

    public static int Capacity => FixedStringSizes.Size8;
    public readonly Span<byte> Span => MemoryMarshal.CreateSpan(ref Unsafe.AsRef(in _data[0]), _length);
    public readonly ReadOnlySpan<byte> ReadOnlySpan => MemoryMarshal.CreateReadOnlySpan(ref Unsafe.AsRef(in _data[0]), _length);
    public readonly Span<byte> RawSpan => MemoryMarshal.CreateSpan(ref Unsafe.AsRef(in _data[0]), Capacity);
    public readonly bool IsEmpty => _length == 0;
    public readonly int Length => _length;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Set(ReadOnlySpan<byte> utf8)
    {
        if (utf8.Length > Capacity) utf8 = utf8[..Capacity];
        _length = (byte)utf8.Length;
        utf8.CopyTo(RawSpan);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Set(string value)
    {
        if (string.IsNullOrEmpty(value)) { _length = 0; return; }
        // Use most efficient non-allocating encoding method
        _length = (byte)Encoding.UTF8.GetBytes(value.AsSpan(0, Math.Min(value.Length, Capacity)), RawSpan);
    }

    public void SetLength(int length) => _length = (byte)Math.Min(length, Capacity);
    public void Clear() { _length = 0; _data[0] = 0; }
    public override readonly string ToString() => Encoding.UTF8.GetString(ReadOnlySpan);
    public readonly bool TryParseLong(out long res) => System.Buffers.Text.Utf8Parser.TryParse(ReadOnlySpan, out res, out _);
    public readonly bool TryParseDouble(out double res) => System.Buffers.Text.Utf8Parser.TryParse(ReadOnlySpan, out res, out _);
    public readonly bool Equals(FixedString8 other) => FixedStringHelpers.Equals(ReadOnlySpan, other.ReadOnlySpan);
    public override readonly bool Equals(object? obj) => obj is FixedString8 other && Equals(other);
    public override readonly int GetHashCode() => FixedStringHelpers.ComputeHashCode(ReadOnlySpan);
    public static bool operator ==(FixedString8 left, FixedString8 right) => left.Equals(right);
    public static bool operator !=(FixedString8 left, FixedString8 right) => !left.Equals(right);
    public static implicit operator FixedString8(string s) => new(s);
}

/// <summary>
/// Zero-allocation fixed-size string (16 bytes max).
/// For short identifiers.
/// </summary>
[StructLayout(LayoutKind.Auto)]
public struct FixedString16 : IFixedString, IEquatable<FixedString16>
{
    private FixedString16Buffer _data;
    private byte _length;

    public FixedString16(string value) => Set(value);
    public FixedString16(ReadOnlySpan<byte> utf8) => Set(utf8);

    public static int Capacity => FixedStringSizes.Size16;
    public readonly Span<byte> Span => MemoryMarshal.CreateSpan(ref Unsafe.AsRef(in _data[0]), _length);
    public readonly ReadOnlySpan<byte> ReadOnlySpan => MemoryMarshal.CreateReadOnlySpan(ref Unsafe.AsRef(in _data[0]), _length);
    public readonly Span<byte> RawSpan => MemoryMarshal.CreateSpan(ref Unsafe.AsRef(in _data[0]), Capacity);
    public readonly bool IsEmpty => _length == 0;
    public readonly int Length => _length;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Set(ReadOnlySpan<byte> utf8)
    {
        if (utf8.Length > Capacity) utf8 = utf8[..Capacity];
        _length = (byte)utf8.Length;
        utf8.CopyTo(RawSpan);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Set(string value)
    {
        if (string.IsNullOrEmpty(value)) { _length = 0; return; }
        _length = (byte)Encoding.UTF8.GetBytes(value.AsSpan(0, Math.Min(value.Length, Capacity)), RawSpan);
    }

    public void SetLength(int length) => _length = (byte)Math.Min(length, Capacity);
    public void Clear() { _length = 0; _data[0] = 0; }
    public override readonly string ToString() => Encoding.UTF8.GetString(ReadOnlySpan);
    public readonly bool TryParseLong(out long res) => System.Buffers.Text.Utf8Parser.TryParse(ReadOnlySpan, out res, out _);
    public readonly bool TryParseDouble(out double res) => System.Buffers.Text.Utf8Parser.TryParse(ReadOnlySpan, out res, out _);
    public readonly bool Equals(FixedString16 other) => FixedStringHelpers.Equals(ReadOnlySpan, other.ReadOnlySpan);
    public override readonly bool Equals(object? obj) => obj is FixedString16 other && Equals(other);
    public override readonly int GetHashCode() => FixedStringHelpers.ComputeHashCode(ReadOnlySpan);
    public static bool operator ==(FixedString16 left, FixedString16 right) => left.Equals(right);
    public static bool operator !=(FixedString16 left, FixedString16 right) => !left.Equals(right);
    public static implicit operator FixedString16(string s) => new(s);
}

[StructLayout(LayoutKind.Auto)]
public struct FixedString32 : IFixedString, IEquatable<FixedString32>
{
    private FixedString32Buffer _data;
    private byte _length;

    public FixedString32(string value) => Set(value);
    public FixedString32(ReadOnlySpan<byte> utf8) => Set(utf8);

    public static int Capacity => FixedStringSizes.Size32;
    public readonly Span<byte> Span => MemoryMarshal.CreateSpan(ref Unsafe.AsRef(in _data[0]), _length);
    public readonly ReadOnlySpan<byte> ReadOnlySpan => MemoryMarshal.CreateReadOnlySpan(ref Unsafe.AsRef(in _data[0]), _length);
    public readonly Span<byte> RawSpan => MemoryMarshal.CreateSpan(ref Unsafe.AsRef(in _data[0]), Capacity);
    public readonly bool IsEmpty => _length == 0;
    public readonly int Length => _length;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Set(ReadOnlySpan<byte> utf8)
    {
        if (utf8.Length > Capacity) utf8 = utf8[..Capacity];
        _length = (byte)utf8.Length;
        utf8.CopyTo(RawSpan);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Set(string value)
    {
        if (string.IsNullOrEmpty(value)) { _length = 0; return; }
        _length = (byte)Encoding.UTF8.GetBytes(value.AsSpan(0, Math.Min(value.Length, Capacity)), RawSpan);
    }

    public void SetLength(int length) => _length = (byte)Math.Min(length, Capacity);
    public void Clear() { _length = 0; _data[0] = 0; }
    public override readonly string ToString() => Encoding.UTF8.GetString(ReadOnlySpan);
    public readonly bool TryParseLong(out long res) => System.Buffers.Text.Utf8Parser.TryParse(ReadOnlySpan, out res, out _);
    public readonly bool TryParseDouble(out double res) => System.Buffers.Text.Utf8Parser.TryParse(ReadOnlySpan, out res, out _);
    public readonly bool Equals(FixedString32 other) => FixedStringHelpers.Equals(ReadOnlySpan, other.ReadOnlySpan);
    public override readonly bool Equals(object? obj) => obj is FixedString32 other && Equals(other);
    public override readonly int GetHashCode() => FixedStringHelpers.ComputeHashCode(ReadOnlySpan);
    public static bool operator ==(FixedString32 left, FixedString32 right) => left.Equals(right);
    public static bool operator !=(FixedString32 left, FixedString32 right) => !left.Equals(right);
    public static implicit operator FixedString32(string s) => new(s);
}

[StructLayout(LayoutKind.Auto)]
public struct FixedString64 : IFixedString, IEquatable<FixedString64>
{
    private FixedString64Buffer _data;
    private byte _length;

    public FixedString64(string value) => Set(value);
    public FixedString64(ReadOnlySpan<byte> utf8) => Set(utf8);

    public static int Capacity => FixedStringSizes.Size64;
    public readonly Span<byte> Span => MemoryMarshal.CreateSpan(ref Unsafe.AsRef(in _data[0]), _length);
    public readonly ReadOnlySpan<byte> ReadOnlySpan => MemoryMarshal.CreateReadOnlySpan(ref Unsafe.AsRef(in _data[0]), _length);
    public readonly Span<byte> RawSpan => MemoryMarshal.CreateSpan(ref Unsafe.AsRef(in _data[0]), Capacity);
    public readonly bool IsEmpty => _length == 0;
    public readonly int Length => _length;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Set(ReadOnlySpan<byte> utf8)
    {
        if (utf8.Length > Capacity) utf8 = utf8[..Capacity];
        _length = (byte)utf8.Length;
        utf8.CopyTo(RawSpan);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Set(string value)
    {
        if (string.IsNullOrEmpty(value)) { _length = 0; return; }
        _length = (byte)Encoding.UTF8.GetBytes(value.AsSpan(0, Math.Min(value.Length, Capacity)), RawSpan);
    }

    public void SetLength(int length) => _length = (byte)Math.Min(length, Capacity);
    public void Clear() { _length = 0; _data[0] = 0; }
    public override readonly string ToString() => Encoding.UTF8.GetString(ReadOnlySpan);
    public readonly bool TryParseLong(out long res) => System.Buffers.Text.Utf8Parser.TryParse(ReadOnlySpan, out res, out _);
    public readonly bool TryParseDouble(out double res) => System.Buffers.Text.Utf8Parser.TryParse(ReadOnlySpan, out res, out _);
    public readonly bool Equals(FixedString64 other) => FixedStringHelpers.Equals(ReadOnlySpan, other.ReadOnlySpan);
    public override readonly bool Equals(object? obj) => obj is FixedString64 other && Equals(other);
    public override readonly int GetHashCode() => FixedStringHelpers.ComputeHashCode(ReadOnlySpan);
    public static bool operator ==(FixedString64 left, FixedString64 right) => left.Equals(right);
    public static bool operator !=(FixedString64 left, FixedString64 right) => !left.Equals(right);
    public static implicit operator FixedString64(string s) => new(s);
}

[StructLayout(LayoutKind.Auto)]
public struct FixedString128 : IFixedString, IEquatable<FixedString128>
{
    private FixedString128Buffer _data;
    private byte _length;

    public FixedString128(string value) => Set(value);
    public FixedString128(ReadOnlySpan<byte> utf8) => Set(utf8);

    public static int Capacity => FixedStringSizes.Size128;
    public readonly Span<byte> Span => MemoryMarshal.CreateSpan(ref Unsafe.AsRef(in _data[0]), _length);
    public readonly ReadOnlySpan<byte> ReadOnlySpan => MemoryMarshal.CreateReadOnlySpan(ref Unsafe.AsRef(in _data[0]), _length);
    public readonly Span<byte> RawSpan => MemoryMarshal.CreateSpan(ref Unsafe.AsRef(in _data[0]), Capacity);
    public readonly bool IsEmpty => _length == 0;
    public readonly int Length => _length;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Set(ReadOnlySpan<byte> utf8)
    {
        if (utf8.Length > Capacity) utf8 = utf8[..Capacity];
        _length = (byte)utf8.Length;
        utf8.CopyTo(RawSpan);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Set(string value)
    {
        if (string.IsNullOrEmpty(value)) { _length = 0; return; }
        _length = (byte)Encoding.UTF8.GetBytes(value.AsSpan(0, Math.Min(value.Length, Capacity)), RawSpan);
    }

    public void SetLength(int length) => _length = (byte)Math.Min(length, Capacity);
    public void Clear() { _length = 0; _data[0] = 0; }
    public override readonly string ToString() => Encoding.UTF8.GetString(ReadOnlySpan);
    public readonly bool TryParseLong(out long res) => System.Buffers.Text.Utf8Parser.TryParse(ReadOnlySpan, out res, out _);
    public readonly bool TryParseDouble(out double res) => System.Buffers.Text.Utf8Parser.TryParse(ReadOnlySpan, out res, out _);
    public readonly bool Equals(FixedString128 other) => FixedStringHelpers.Equals(ReadOnlySpan, other.ReadOnlySpan);
    public override readonly bool Equals(object? obj) => obj is FixedString128 other && Equals(other);
    public override readonly int GetHashCode() => FixedStringHelpers.ComputeHashCode(ReadOnlySpan);
    public static bool operator ==(FixedString128 left, FixedString128 right) => left.Equals(right);
    public static bool operator !=(FixedString128 left, FixedString128 right) => !left.Equals(right);
    public static implicit operator FixedString128(string s) => new(s);
}

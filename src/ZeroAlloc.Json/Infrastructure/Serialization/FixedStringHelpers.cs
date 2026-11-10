#pragma warning disable MA0048

using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace GapSonar.Core.Shared.Infrastructure.Serialization;

/// <summary>
/// Helper methods for fixed-size string operations.
/// Provides common functionality for all FixedString types.
/// </summary>
public static class FixedStringHelpers
{
    /// <summary>
    /// Computes FNV-1a hash for a span of bytes.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int ComputeHashCode(ReadOnlySpan<byte> span)
    {
        uint hash = 2166136261;
        foreach (var b in span)
            hash = (hash ^ b) * 16777619;
        return (int)hash;
    }

    /// <summary>
    /// Compares two spans for equality.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool Equals(ReadOnlySpan<byte> left, ReadOnlySpan<byte> right)
    {
        if (left.Length != right.Length)
            return false;
        return left.SequenceEqual(right);
    }

    /// <summary>
    /// Copies UTF-8 bytes to a buffer with truncation if needed.
    /// Returns the actual length copied.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int CopyWithTruncation(ReadOnlySpan<byte> source, Span<byte> destination)
    {
        int length = source.Length > destination.Length ? destination.Length : source.Length;
        source[..length].CopyTo(destination);
        return length;
    }

    /// <summary>
    /// Clears a buffer by setting first byte to zero.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Clear(Span<byte> buffer)
    {
        if (buffer.Length > 0)
            buffer[0] = 0;
    }

    /// <summary>
    /// Creates a span from inline array buffer.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Span<byte> CreateSpan<TBuffer>(ref TBuffer buffer, int length)
        where TBuffer : struct
    {
        return MemoryMarshal.CreateSpan(ref Unsafe.As<TBuffer, byte>(ref buffer), length);
    }

    /// <summary>
    /// Creates a read-only span from inline array buffer.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ReadOnlySpan<byte> CreateReadOnlySpan<TBuffer>(ref TBuffer buffer, int length)
        where TBuffer : struct
    {
        return MemoryMarshal.CreateReadOnlySpan(ref Unsafe.As<TBuffer, byte>(ref buffer), length);
    }
}

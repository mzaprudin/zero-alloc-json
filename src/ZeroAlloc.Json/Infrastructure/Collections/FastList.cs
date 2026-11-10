#pragma warning disable MA0048
#pragma warning disable MA0012

using System.Collections;
using System.Runtime.CompilerServices;

namespace GapSonar.Core.Shared.Infrastructure.Collections;

/// <summary>
/// Interface for objects that need to be reset when returned to the pool (FastList).
/// </summary>
public interface IFastResettable
{
    void Reset();
}

/// <summary>
/// A high-performance, zero-allocation list with pre-allocated storage.
/// Designed for HFT scenarios where garbage collection overhead must be avoided.
/// </summary>
/// <typeparam name="T">The type of elements in the list.</typeparam>
public sealed class FastList<T> : IList<T>, IReadOnlyList<T>
{
    private T[] _items;
    private T[]? _initialRefs; // Tracking for integrity checks
    private int _count;
    private readonly Func<T>? _factory;

    /// <summary>
    /// Gets the fixed capacity of the list.
    /// </summary>
    public int Capacity => _items.Length;

    public int Count => _count;

    public bool IsReadOnly => false;

    /// <summary>
    /// Initializes a new instance of the <see cref="FastList{T}"/> class.
    /// </summary>
    /// <param name="capacity">Fixed capacity.</param>
    /// <param name="factory">Optional factory to pre-populate items (for classes). If null, uses new T() or default.</param>
    public FastList(int capacity, Func<T>? factory = null)
    {
        if (capacity < 0) throw new ArgumentOutOfRangeException(nameof(capacity));
        _items = new T[capacity];
        _count = 0;
        _factory = factory;

        bool isClass = !typeof(T).IsValueType;
        bool isString = typeof(T) == typeof(string);

        // We track initial references for classes, except strings (which are immutable but often replaced)
        if (isClass && !isString) _initialRefs = new T[capacity];

        if (isClass)
        {
            Preallocate(0, capacity);
        }
    }

    private void Preallocate(int start, int end)
    {
        bool isString = typeof(T) == typeof(string);
        for (int i = start; i < end; i++)
        {
            T item = isString
                ? (T)(object)string.Empty
                : (_factory is not null ? _factory() : Activator.CreateInstance<T>()!);

            _items[i] = item;
            _initialRefs?[i] = item;
        }
    }

    /// <summary>
    /// Verifies that no object references have been replaced in the list storage.
    /// Only applicable for class types (excluding strings).
    /// </summary>
    public void VerifyIntegrity()
    {
        if (_initialRefs is null) return;

        for (int i = 0; i < _items.Length; i++)
        {
            if (!ReferenceEquals(_items[i], _initialRefs[i]))
            {
                throw new InvalidOperationException($"FastList integrity violation: object at index {i} was replaced! This indicates an accidental allocation or assignment.");
            }
        }
    }

    /// <summary>
    /// Gets or sets the element at the specified index.
    /// </summary>
    public T this[int index]
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get
        {
            if ((uint)index >= (uint)_count) ThrowIndexOutOfRange();
            return _items[index];
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        set
        {
            if ((uint)index >= (uint)_count) ThrowIndexOutOfRange();
            _items[index] = value;
        }
    }

    /// <summary>
    /// Returns a reference to the item at the specified index.
    /// Use this for high-performance mutation of structs or avoiding copy overhead.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ref T GetRef(int index)
    {
        if ((uint)index >= (uint)_count) ThrowIndexOutOfRange();
        return ref _items[index];
    }

    /// <summary>
    /// Advances the count and returns a reference to the next slot in the pre-allocated array.
    /// This is the primary method for zero-allocation parsing.
    /// If capacity is exceeded, it performs an emergency expansion (breaking zero-allocation).
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ref T AddAndGetRef()
    {
        int index = _count;
        if ((uint)index >= (uint)_items.Length)
        {
            Expand();
        }
        _count = index + 1;
        return ref _items[index];
    }

    /// <summary>
    /// Adds an item to the list. For reference types, consider using AddAndGetRef() to avoid creating new instances externally.
    /// If capacity is exceeded, it performs an emergency expansion (breaking zero-allocation).
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Add(T item)
    {
        int index = _count;
        if ((uint)index >= (uint)_items.Length)
        {
            Expand();
        }
        _items[index] = item;
        _count = index + 1;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private void Expand()
    {
        int oldCapacity = _items.Length;
        int newCapacity = oldCapacity == 0 ? 4 : oldCapacity * 2;

        Console.Error.WriteLine($"[CRITICAL] FastList<{typeof(T).Name}> capacity exceeded! Emergency expansion from {oldCapacity} to {newCapacity}. This breaks zero-allocation guarantees.");

        T[] newItems = new T[newCapacity];
        Array.Copy(_items, newItems, oldCapacity);
        _items = newItems;

        if (_initialRefs is not null)
        {
            T[] newInitialRefs = new T[newCapacity];
            Array.Copy(_initialRefs, newInitialRefs, oldCapacity);
            _initialRefs = newInitialRefs;
        }

        if (!typeof(T).IsValueType)
        {
            Preallocate(oldCapacity, newCapacity);
        }
    }

    /// <summary>
    /// Clears the list by setting Count to 0. Does not release references to objects.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Clear()
    {
        // For HFT, we often don't want to clear the array references (Array.Clear),
        // because we want to reuse those objects in the next cycle.
        // We just reset the count.
        _count = 0;
    }

    /// <summary>
    /// Clears the list and optionally resets each item if it implements IFastResettable.
    /// Use this when starting a new population cycle to ensure clean state.
    /// </summary>
    public void Reset(bool deepReset = false)
    {
        if (deepReset && typeof(IFastResettable).IsAssignableFrom(typeof(T)))
        {
            // Reset state of all potentially used items (up to previous count or capacity?)
            // Usually enough to reset up to _count, but if we want total safety, maybe capacity.
            // Resetting only _count is faster.
            for (int i = 0; i < _count; i++)
            {
                ((IFastResettable?)_items[i])?.Reset();
            }
        }
        _count = 0;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Contains(T item)
    {
        return IndexOf(item) != -1;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void CopyTo(T[] array, int arrayIndex)
    {
        Array.Copy(_items, 0, array, arrayIndex, _count);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int IndexOf(T item)
    {
        return Array.IndexOf(_items, item, 0, _count);
    }

    public void Insert(int index, T item) => throw new NotSupportedException("Insert is not supported in FastList to maintain order/performance.");

    public bool Remove(T item) => throw new NotSupportedException("Remove is not supported in FastList.");

    public void RemoveAt(int index) => throw new NotSupportedException("RemoveAt is not supported in FastList.");

    public IEnumerator<T> GetEnumerator()
    {
        for (int i = 0; i < _count; i++)
        {
            yield return _items[i];
        }
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    private static void ThrowIndexOutOfRange() => throw new IndexOutOfRangeException();
    private static void ThrowCapacityExceeded() => throw new InvalidOperationException("FastList capacity exceeded.");
}

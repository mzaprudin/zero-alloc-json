using System.Buffers;
using GapSonar.Core.Shared;

namespace RouterBenchmark;

/// <summary>The original routing delegate shape, isolated from the exchange runtime.</summary>
public delegate bool SymbolLookup(ReadOnlySpan<byte> symbol, out SymbolId id);

/// <summary>The original routing contract, used locally by the benchmark implementations.</summary>
public interface IMessageRouter<TContext>
{
    /// <summary>Extracts the symbol identifier and application context from a message.</summary>
    bool Route(ReadOnlySequence<byte> message, out SymbolId symbolId, out TContext context);
}

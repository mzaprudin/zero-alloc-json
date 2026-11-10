namespace GapSonar.Core.Shared.Infrastructure.Serialization;

[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
public sealed class JsonPositionAttribute(int position) : Attribute
{
    public int Position { get; } = position;
}

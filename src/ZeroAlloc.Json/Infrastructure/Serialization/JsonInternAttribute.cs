using System;

namespace GapSonar.Core.Shared.Infrastructure.Serialization;

[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
public sealed class JsonInternAttribute : Attribute
{
}

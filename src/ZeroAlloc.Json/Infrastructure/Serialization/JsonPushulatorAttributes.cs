#pragma warning disable MA0048

using System;

namespace GapSonar.Core.Shared.Infrastructure.Serialization;

/// <summary>
/// Marks a property or field to be serialized as a string in JSON,
/// even if its underlying type is numeric or boolean.
/// </summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
public sealed class JsonToStringAttribute : Attribute { }

/// <summary>
/// Marks a DateTime property to be serialized as a long millisecond timestamp.
/// </summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
public sealed class JsonToMsAttribute : Attribute { }

/// <summary>
/// Marks a DateTime property to be serialized as a string containing a millisecond timestamp.
/// </summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
public sealed class JsonToStringMsAttribute : Attribute { }

/// <summary>
/// Marks a property or field to be included as an HTTP header instead of the JSON body.
/// </summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
public sealed class RequestHeaderAttribute(string name) : Attribute
{
    public string Name { get; } = name;
}

/// <summary>
/// Marks a struct field to be wrapped in a JSON array [ ... ] during serialization.
/// Used to avoid array allocations and pointer dereferences for single-element collections.
/// </summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
public sealed class JsonInlineArrayAttribute : Attribute { }

/// <summary>
/// Marks a struct to be serialized as a JSON array [ val1, val2, ... ]
/// instead of an object { "prop1": val1, ... }.
/// Order of elements is determined by field declaration order.
/// </summary>
[AttributeUsage(AttributeTargets.Struct | AttributeTargets.Class)]
public sealed class JsonAsArrayAttribute : Attribute { }

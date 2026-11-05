using System;
using System.Collections.Generic;

namespace http;

/// <summary>
/// Represents a Structured Field item or inner-list member.
/// </summary>
public class StructuredFieldMember
{
    /// <summary>
    /// The represented bare-item value kind.
    /// </summary>
    public HttpFieldValueKind ValueKind;

    /// <summary>
    /// The token, string, or display-string value, or <c>null</c> for another value kind.
    /// </summary>
    public string? TextValue;

    /// <summary>
    /// The byte-sequence value.
    /// </summary>
    public byte[] ByteSequenceValue = Array.Empty<byte>();

    /// <summary>
    /// The integer value.
    /// </summary>
    public long IntegerValue;

    /// <summary>
    /// The decimal value scaled by one thousand.
    /// </summary>
    public long DecimalThousandths;

    /// <summary>
    /// The Boolean value.
    /// </summary>
    public bool BooleanValue;

    /// <summary>
    /// The date value as seconds since the Unix epoch.
    /// </summary>
    public long DateSeconds;

    /// <summary>
    /// Indicates that this member is an inner list rather than a bare item.
    /// </summary>
    public bool IsInnerList;

    /// <summary>
    /// The ordered members of an inner list.
    /// </summary>
    public List<StructuredFieldMember> InnerList = new List<StructuredFieldMember>();

    /// <summary>
    /// The ordered parameters attached to this item or inner list.
    /// </summary>
    public List<StructuredFieldParameter> Parameters = new List<StructuredFieldParameter>();

    /// <summary>
    /// The dictionary key, or <c>null</c> when this is not a dictionary member.
    /// </summary>
    public string? DictionaryKey;

    /// <summary>
    /// Indicates that a dictionary member explicitly encoded its value.
    /// </summary>
    public bool HasExplicitDictionaryValue;
}

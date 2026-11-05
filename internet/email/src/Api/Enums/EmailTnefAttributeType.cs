using System;
using System.Collections.Generic;

namespace email;
/// <summary>
/// Specifies the supported TNEF attribute values.
/// </summary>
/// <remarks>
/// Use these values to make the corresponding parsing, formatting, storage, or policy choice explicit.
/// </remarks>
public enum EmailTnefAttributeType
{
    /// <summary>
    /// Represents the triples choice or state.
    /// </summary>
    Triples = 0,
    /// <summary>
    /// Represents the string choice or state.
    /// </summary>
    String = 1,
    /// <summary>
    /// Represents the text choice or state.
    /// </summary>
    Text = 2,
    /// <summary>
    /// Represents the date choice or state.
    /// </summary>
    Date = 3,
    /// <summary>
    /// Represents the short choice or state.
    /// </summary>
    Short = 4,
    /// <summary>
    /// Represents the long choice or state.
    /// </summary>
    Long = 5,
    /// <summary>
    /// Represents the byte choice or state.
    /// </summary>
    Byte = 6,
    /// <summary>
    /// Represents the word choice or state.
    /// </summary>
    Word = 7,
    /// <summary>
    /// Represents the dword choice or state.
    /// </summary>
    Dword = 8,
    /// <summary>
    /// The value is unknown or was not recognized.
    /// </summary>
    Unknown = 65535
}

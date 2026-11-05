using System;
using System.Globalization;
using System.Threading;

namespace html;
/// <summary>Selects which optional characters are represented as named or numeric character references.</summary>
public enum HtmlEntityEncoding
{
    /// <summary>Encodes only characters required by the selected HTML context.</summary>
    Minimal = 0,
    /// <summary>Prefers named character references when one is available.</summary>
    Named = 1,
    /// <summary>Uses numeric character references for encoded characters.</summary>
    Numeric = 2,
    /// <summary>Encodes every non-ASCII character in addition to required syntax characters.</summary>
    AsciiOnly = 3
}

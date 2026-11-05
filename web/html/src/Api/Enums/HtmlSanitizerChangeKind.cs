using System;
using System.Threading;
using System.Globalization;

namespace html;
/// <summary>Identifies whether a sanitizer change removed, replaced, added, or unwrapped content.</summary>
public enum HtmlSanitizerChangeKind
{
    /// <summary>Records that existing content was removed.</summary>
    Removed = 1,
    /// <summary>Records that existing content or a value was replaced.</summary>
    Replaced = 2,
    /// <summary>Records that policy-required content or a value was added.</summary>
    Added = 3,
    /// <summary>Records that an element was removed while its descendants were retained.</summary>
    Unwrapped = 4
}

using System;
using System.Threading;

namespace html;
/// <summary>Classifies a selector compilation error as invalid, unsupported, or too complex.</summary>
public enum HtmlSelectorCompileErrorKind
{
    /// <summary>The selector does not follow the supported CSS grammar.</summary>
    InvalidSyntax = 0,
    /// <summary>The selector uses valid CSS syntax that this library does not implement.</summary>
    UnsupportedFeature = 1,
    /// <summary>The selector exceeds the configured compilation complexity limit.</summary>
    ComplexityExceeded = 2
}

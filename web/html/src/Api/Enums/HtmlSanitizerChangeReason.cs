using System;
using System.Threading;
using System.Globalization;

namespace html;
/// <summary>Identifies why the sanitizer removed, rewrote, or added content.</summary>
public enum HtmlSanitizerChangeReason
{
    /// <summary>Indicates that no option or value applies.</summary>
    None = 0,
    /// <summary>The element was absent from the policy allowlist.</summary>
    ElementNotAllowed = 1001,
    /// <summary>A security-sensitive element and its descendants were removed.</summary>
    SensitiveElementDropped = 1002,
    /// <summary>An explicit blocked-element rule was applied.</summary>
    ElementBlocked = 1003,
    /// <summary>A comment was removed because comments are disabled.</summary>
    CommentNotAllowed = 1101,
    /// <summary>A document type was removed because document types are disabled.</summary>
    DoctypeNotAllowed = 1102,
    /// <summary>An attribute was absent from the applicable allowlist.</summary>
    AttributeNotAllowed = 2001,
    /// <summary>An attribute name or value was normalized or replaced.</summary>
    AttributeRewritten = 2002,
    /// <summary>A policy-required attribute was added.</summary>
    AttributeAdded = 2003,
    /// <summary>A customized built-in element was rejected by markup policy.</summary>
    CustomizedBuiltInNotAllowed = 2004,
    /// <summary>A URL-bearing value failed the applicable URL policy.</summary>
    UnsafeUrl = 2101,
    /// <summary>An accepted relative or obfuscated URL was rewritten.</summary>
    UrlRewritten = 2102,
    /// <summary>A link attribute was added or changed to reduce opener or navigation risk.</summary>
    LinkHardened = 2201,
    /// <summary>A name capable of shadowing a DOM property was rewritten.</summary>
    DomClobbering = 2301,
    /// <summary>A duplicate identifier was rewritten to remain unique.</summary>
    DuplicateIdentifier = 2302,
    /// <summary>Nested markup could not be sanitized completely and was rejected.</summary>
    NestedContentRejected = 2401,
    /// <summary>Nested markup was sanitized and replaced with safe serialized content.</summary>
    NestedContentRewritten = 2402,
    /// <summary>A recognized template-language marker was escaped or rewritten.</summary>
    TemplateSyntaxNeutralized = 2501,
    /// <summary>An inline style was removed because style processing was disabled or failed.</summary>
    InlineStyleRemoved = 3001,
    /// <summary>A CSS declaration used a property absent from the allowlist.</summary>
    CssPropertyRemoved = 3002,
    /// <summary>A CSS declaration was normalized or had a value rewritten.</summary>
    CssDeclarationRewritten = 3003,
    /// <summary>Inline CSS processing reached a configured resource limit.</summary>
    CssLimitExceeded = 3099,
    /// <summary>Sanitization reached a configured resource limit.</summary>
    LimitExceeded = 9001,
    /// <summary>The caller cancelled sanitization before it completed.</summary>
    Cancelled = 9002
}

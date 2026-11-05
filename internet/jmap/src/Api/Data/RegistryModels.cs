using System.Collections.Generic;

namespace jmap;

/// <summary>
/// Registers an application-defined JMAP method and its required capabilities.
/// </summary>
public class JmapVendorMethodRegistration
{
    /// <summary>The case-sensitive vendor method name.</summary>
    public string? MethodName;

    /// <summary>The extension capabilities required in addition to JMAP Core.</summary>
    public List<string> RequiredCapabilities = new List<string>();

    /// <summary>The handler that receives opaque vendor invocations.</summary>
    public IJmapVendorMethodHandler? Handler;
}

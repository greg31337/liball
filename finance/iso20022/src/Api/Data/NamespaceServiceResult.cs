using System.Collections.Generic;

namespace iso20022;

/// <summary>Contains the exception-less result of namespace parsing or construction.</summary>
public class NamespaceServiceResult
{
    /// <summary>Whether the operation succeeded.</summary>
    public bool Success;
    /// <summary>The resulting namespace descriptor when successful.</summary>
    public NamespaceDescriptor? Descriptor;
    /// <summary>The ordered repairs applied during lenient processing.</summary>
    public List<NamespaceRepair> Repairs = new List<NamespaceRepair>();
    /// <summary>A stable error code when the operation failed.</summary>
    public string ErrorCode = string.Empty;
    /// <summary>A human-readable error message when the operation failed.</summary>
    public string ErrorMessage = string.Empty;
}

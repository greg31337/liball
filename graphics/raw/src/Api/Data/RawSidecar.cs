namespace raw;

/// <summary>Contains a versioned generic raw descriptor sidecar and preserved extension properties.</summary>
public class RawSidecar
{
    /// <summary>The sidecar schema version.</summary>
    public int SchemaVersion;
    /// <summary>The exact generic raw payload descriptor.</summary>
    public RawDescriptor Descriptor;
    /// <summary>Unknown top-level JSON properties retained for forward-compatible rewriting.</summary>
    public System.Collections.Generic.Dictionary<string, System.Text.Json.JsonElement> UnknownProperties;

    /// <summary>Creates a sidecar document.</summary>
    /// <param name="schemaVersion">The schema version.</param>
    /// <param name="descriptor">The generic raw descriptor.</param>
    /// <param name="unknownProperties">Unknown top-level properties, or <c>null</c>.</param>
    public RawSidecar(int schemaVersion, RawDescriptor descriptor, System.Collections.Generic.Dictionary<string, System.Text.Json.JsonElement>? unknownProperties)
    {
        SchemaVersion = schemaVersion;
        Descriptor = descriptor;
        UnknownProperties = unknownProperties == null ? new System.Collections.Generic.Dictionary<string, System.Text.Json.JsonElement>(System.StringComparer.Ordinal) : unknownProperties;
    }
}

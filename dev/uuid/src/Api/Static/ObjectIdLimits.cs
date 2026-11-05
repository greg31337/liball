namespace uuid;

/// <summary>Defines the fixed ObjectId layout limits.</summary>
public static class ObjectIdLimits
{
    /// <summary>The ObjectId byte length.</summary>
    public const int ByteLength = 12;
    /// <summary>The process-unique field byte length.</summary>
    public const int ProcessUniqueByteLength = 5;
    /// <summary>The largest 24-bit counter.</summary>
    public const int MaxCounter = 16777215;
    /// <summary>The largest representable Unix second.</summary>
    public const long MaxUnixSeconds = 4294967295;
}

namespace json;

/// <summary>Contains exactly one caller-owned destination for UTF-8 JSON output.</summary>
public class JsonWriterTarget
{
    /// <summary>The caller-owned stream destination, or <c>null</c> when a buffer writer is used.</summary>
    public global::System.IO.Stream? Stream;

    /// <summary>The caller-owned buffer writer destination, or <c>null</c> when a stream is used.</summary>
    public global::System.Buffers.IBufferWriter<byte>? BufferWriter;
}

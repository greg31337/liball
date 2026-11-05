namespace http;

/// <summary>
/// Decodes HTTP fields and manages connection-scoped HPACK decoder state.
/// </summary>
public interface IHpackDecoder
{
    /// <summary>Decodes one complete HPACK header block.</summary>
    /// <param name="input">The compressed header-block bytes.</param>
    /// <param name="options">The HPACK limits and decoder policy.</param>
    /// <returns>The decoded fields, progress, and table state.</returns>
    HpackResult DecodeHeaderBlock(byte[] input, HpackOptions options);

    /// <summary>Updates the decoder table-size limit advertised to the peer.</summary>
    /// <param name="maximumSize">The new maximum size.</param>
    /// <returns>The updated table state.</returns>
    HpackResult UpdateDecoderTableLimit(int maximumSize);

    /// <summary>Inspects a copy of decoder dynamic-table state.</summary>
    /// <returns>The current table state.</returns>
    HpackResult InspectDecoderTable();
}

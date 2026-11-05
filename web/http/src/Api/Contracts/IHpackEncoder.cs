namespace http;

/// <summary>
/// Encodes HTTP fields using HPACK.
/// </summary>
public interface IHpackEncoder
{
    /// <summary>
    /// Encodes ordered HTTP fields using bounded HPACK options and returns table state.
    /// </summary>
    /// <param name="headers">The ordered fields to encode.</param>
    /// <param name="options">The HPACK limits and encoder policies.</param>
    /// <returns>The encoded bytes, progress counts, and dynamic-table summary.</returns>
    HpackResult EncodeHeaderBlock(HttpHeaders headers, HpackOptions options);

    /// <summary>Updates the encoder dynamic-table limit and returns resulting state.</summary>
    /// <param name="maximumSize">The peer-advertised maximum size.</param>
    /// <returns>The updated table state.</returns>
    HpackResult UpdateEncoderTableLimit(int maximumSize);

    /// <summary>Inspects a copy of encoder dynamic-table state.</summary>
    /// <returns>The current table state.</returns>
    HpackResult InspectEncoderTable();
}

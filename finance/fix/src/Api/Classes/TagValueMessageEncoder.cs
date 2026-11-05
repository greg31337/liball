using System;

namespace fix;

/// <summary>Encodes canonical FIX messages as complete TagValue frames.</summary>
public class TagValueMessageEncoder : IMessageEncoder
{
    /// <summary>Creates a TagValue message encoder.</summary>
    public TagValueMessageEncoder()
    {
    }

    /// <summary>Encodes one message into caller-owned storage without partial writes.</summary>
    /// <param name="message">The message to encode.</param>
    /// <param name="dictionary">The dictionary used to order fields and flatten groups.</param>
    /// <param name="destination">The destination storage.</param>
    /// <param name="options">The encode options and resource limits.</param>
    /// <returns>The explicit encode outcome and exact required size.</returns>
    public EncodeResult EncodeMessage(FixMessage message, FixDictionary dictionary, Span<byte> destination, EncodeOptions options)
    {
        MessageEncodingResult encoded = CanonicalMessageEncoder.EncodeCanonicalMessage(message, dictionary, options);
        EncodeResult result = new EncodeResult();
        result.RequiredSize = encoded.Required;
        if (!encoded.Success)
        {
            result.Status = encoded.Status;
            result.ErrorCode = encoded.ErrorCode;
            result.ErrorMessage = encoded.ErrorMessage;
            return result;
        }
        if (destination.Length < encoded.Required)
        {
            result.Status = EncodeStatus.DestinationTooSmall;
            result.ErrorCode = "destination-too-small";
            result.ErrorMessage = "The destination cannot hold the complete encoded FIX message.";
            return result;
        }

        encoded.Bytes.AsSpan().CopyTo(destination);
        result.Status = EncodeStatus.Success;
        result.BytesWritten = encoded.Required;
        return result;
    }
}

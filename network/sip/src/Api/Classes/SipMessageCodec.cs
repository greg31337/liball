namespace sip;

/// <summary>Parses, validates, measures, and serializes SIP messages.</summary>
public class SipMessageCodec : ISipMessageCodec
{
    /// <summary>Initializes a SIP message codec.</summary>
    public SipMessageCodec()
    {
    }

    /// <summary>Incrementally parses the first framed SIP stream message.</summary>
    /// <param name="source">The available source bytes.</param>
    /// <param name="options">The parse compatibility and limit options.</param>
    /// <returns>The parse outcome and parsed message.</returns>
    public SipParseResult ParseStreamMessage(byte[] source, SipParseOptions options)
    {
        return SipMessageParser.ParseStreamMessageBytes(source, options);
    }

    /// <summary>Parses one framed SIP datagram message.</summary>
    /// <param name="source">The available datagram bytes.</param>
    /// <param name="options">The parse compatibility and limit options.</param>
    /// <returns>The parse outcome and parsed message.</returns>
    public SipParseResult ParseDatagramMessage(byte[] source, SipParseOptions options)
    {
        return SipMessageParser.ParseDatagramMessageBytes(source, options);
    }

    /// <summary>Validates the semantic consistency of a SIP message.</summary>
    /// <param name="message">The message to validate.</param>
    /// <returns>The validation issues.</returns>
    public SipValidationIssue[] ValidateMessage(SipMessage message)
    {
        return SipMessageValidator.ValidateMessageModel(message, true);
    }

    /// <summary>Measures the serialized size of a SIP message.</summary>
    /// <param name="message">The message to measure.</param>
    /// <param name="options">The write options.</param>
    /// <returns>The required number of bytes.</returns>
    public int MeasureMessage(SipMessage message, SipWriteOptions options)
    {
        SipWriteResult result = SipMessageFormatter.FormatMessageModel(message, options);
        if (!result.Success)
        {
            string error = result.ErrorMessage == null ? "The SIP message cannot be serialized." : result.ErrorMessage;
            int statusCode = message == null ? 0 : message.StatusCode;
            string? methodToken = message == null ? null : message.MethodToken;
            throw new SipProtocolException(error, statusCode, methodToken);
        }
        return result.RequiredBytes;
    }

    /// <summary>Serializes one SIP message.</summary>
    /// <param name="message">The message to serialize.</param>
    /// <param name="options">The write options.</param>
    /// <returns>The write outcome and serialized bytes.</returns>
    public SipWriteResult SerializeMessage(SipMessage message, SipWriteOptions options)
    {
        return SipMessageFormatter.FormatMessageModel(message, options);
    }
}

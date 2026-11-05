using System;
namespace token;

/// <summary>
/// Converts ECDSA signatures between ASN.1 DER and fixed-width raw formats.
/// </summary>
internal static class EcdsaSignatureCodec
{
    /// <summary>
    /// Converts a strict ASN.1 DER ECDSA signature to fixed-width raw <c>r || s</c> bytes.
    /// </summary>
    /// <param name="derSignature">The DER-encoded ECDSA signature.</param>
    /// <param name="componentWidth">The exact byte width required for each unsigned component.</param>
    /// <returns>A result containing the raw signature, or failure details for malformed DER or oversized components.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <c>derSignature</c> is null.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <c>componentWidth</c> is not positive or is too large.</exception>
    internal static TokenBytesResult DerToRaw(byte[] derSignature, int componentWidth)
    {
        if (derSignature == null)
        {
            throw new ArgumentNullException("derSignature");
        }

        return DerToRaw(derSignature.AsSpan(), componentWidth);
    }

    /// <summary>
    /// Converts a strict ASN.1 DER ECDSA signature span to fixed-width raw <c>r || s</c> bytes.
    /// </summary>
    /// <param name="derSignature">The DER-encoded ECDSA signature.</param>
    /// <param name="componentWidth">The exact byte width required for each unsigned component.</param>
    /// <returns>A result containing the raw signature, or failure details for malformed DER or oversized components.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <c>componentWidth</c> is not positive or is too large.</exception>
    internal static TokenBytesResult DerToRaw(ReadOnlySpan<byte> derSignature, int componentWidth)
    {
        if (componentWidth <= 0 || componentWidth > int.MaxValue / 2)
        {
            throw new ArgumentOutOfRangeException("componentWidth");
        }

        if (derSignature.Length == 0 || derSignature[0] != 48)
        {
            return InvalidDer("An ECDSA signature must begin with a DER SEQUENCE.", 0);
        }

        DerLengthResult sequenceLength = ReadDerLength(derSignature, 1);
        if (!sequenceLength.Success)
        {
            return InvalidDer((sequenceLength.Error ?? throw new InvalidOperationException("A required value is null.")).Message, sequenceLength.Error.Position);
        }

        int sequenceStart = sequenceLength.NextPosition;
        if (sequenceLength.Length != derSignature.Length - sequenceStart)
        {
            int errorPosition = sequenceLength.Length > derSignature.Length - sequenceStart
                ? derSignature.Length
                : sequenceStart + sequenceLength.Length;
            return InvalidDer("The DER SEQUENCE length does not match the signature length.", errorPosition);
        }

        DerIntegerResult r = ReadInteger(derSignature, sequenceStart, componentWidth);
        if (!r.Success)
        {
            return InvalidDer((r.Error ?? throw new InvalidOperationException("A required value is null.")).Message, r.Error.Position);
        }

        DerIntegerResult s = ReadInteger(derSignature, r.NextPosition, componentWidth);
        if (!s.Success)
        {
            return InvalidDer((s.Error ?? throw new InvalidOperationException("A required value is null.")).Message, s.Error.Position);
        }

        if (s.NextPosition != derSignature.Length)
        {
            return InvalidDer("The DER SEQUENCE must contain exactly two INTEGER values.", s.NextPosition);
        }

        byte[] raw = new byte[componentWidth * 2];
        derSignature.Slice(r.ValuePosition, r.ValueLength)
            .CopyTo(raw.AsSpan(componentWidth - r.ValueLength, r.ValueLength));
        derSignature.Slice(s.ValuePosition, s.ValueLength)
            .CopyTo(raw.AsSpan(componentWidth * 2 - s.ValueLength, s.ValueLength));

        TokenBytesResult result = new TokenBytesResult();
        result.Bytes = raw;
        result.Success = true;
        return result;
    }

    /// <summary>
    /// Converts a fixed-width raw ECDSA <c>r || s</c> signature to canonical ASN.1 DER.
    /// </summary>
    /// <param name="rawSignature">The fixed-width raw ECDSA signature.</param>
    /// <param name="componentWidth">The exact byte width of each unsigned component.</param>
    /// <returns>A result containing the DER-encoded signature, or failure details when the raw length is invalid.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <c>rawSignature</c> is null.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <c>componentWidth</c> is not positive or is too large.</exception>
    internal static TokenBytesResult RawToDer(byte[] rawSignature, int componentWidth)
    {
        if (rawSignature == null)
        {
            throw new ArgumentNullException("rawSignature");
        }

        return RawToDer(rawSignature.AsSpan(), componentWidth);
    }

    /// <summary>
    /// Converts a fixed-width raw ECDSA <c>r || s</c> signature span to canonical ASN.1 DER.
    /// </summary>
    /// <param name="rawSignature">The fixed-width raw ECDSA signature.</param>
    /// <param name="componentWidth">The exact byte width of each unsigned component.</param>
    /// <returns>A result containing the DER-encoded signature, or failure details when the raw length is invalid.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <c>componentWidth</c> is not positive or is too large.</exception>
    internal static TokenBytesResult RawToDer(ReadOnlySpan<byte> rawSignature, int componentWidth)
    {
        if (componentWidth <= 0 || componentWidth > int.MaxValue / 2)
        {
            throw new ArgumentOutOfRangeException("componentWidth");
        }

        int expectedLength = componentWidth * 2;
        if (rawSignature.Length != expectedLength)
        {
            int errorPosition = rawSignature.Length < expectedLength ? rawSignature.Length : expectedLength;
            TokenBytesResult invalidResult = new TokenBytesResult();
            invalidResult.Error = new TokenError(
                "invalid_ecdsa_raw",
                "A raw ECDSA signature must contain exactly two fixed-width components.",
                errorPosition);
            return invalidResult;
        }

        ReadOnlySpan<byte> r = rawSignature.Slice(0, componentWidth);
        ReadOnlySpan<byte> s = rawSignature.Slice(componentWidth, componentWidth);
        int rValuePosition = FindIntegerValuePosition(r);
        int sValuePosition = FindIntegerValuePosition(s);
        int rValueLength = componentWidth - rValuePosition;
        int sValueLength = componentWidth - sValuePosition;
        bool rNeedsSignByte = (r[rValuePosition] & 128) != 0;
        bool sNeedsSignByte = (s[sValuePosition] & 128) != 0;
        int rContentLength = rValueLength + (rNeedsSignByte ? 1 : 0);
        int sContentLength = sValueLength + (sNeedsSignByte ? 1 : 0);
        long contentLength = 1L + GetDerLengthSize(rContentLength) + rContentLength;
        contentLength += 1L + GetDerLengthSize(sContentLength) + sContentLength;

        if (contentLength > int.MaxValue)
        {
            throw new ArgumentOutOfRangeException("rawSignature");
        }

        long outputLength = 1L + GetDerLengthSize((int)contentLength) + contentLength;
        if (outputLength > int.MaxValue)
        {
            throw new ArgumentOutOfRangeException("rawSignature");
        }

        byte[] der = new byte[(int)outputLength];
        int position = 0;
        der[position] = 48;
        position = WriteDerLength(der.AsSpan(), position + 1, (int)contentLength);
        position = WriteInteger(r, rValuePosition, rValueLength, rNeedsSignByte, der.AsSpan(), position);
        WriteInteger(s, sValuePosition, sValueLength, sNeedsSignByte, der.AsSpan(), position);

        TokenBytesResult result = new TokenBytesResult();
        result.Bytes = der;
        result.Success = true;
        return result;
    }

    static int FindIntegerValuePosition(ReadOnlySpan<byte> component)
    {
        int position = 0;
        while (position + 1 < component.Length && component[position] == 0)
        {
            position += 1;
        }

        return position;
    }

    static int GetDerLengthSize(int length)
    {
        if (length < 128)
        {
            return 1;
        }

        int byteCount = 0;
        int remaining = length;
        while (remaining > 0)
        {
            byteCount += 1;
            remaining >>= 8;
        }

        return byteCount + 1;
    }

    static int WriteDerLength(Span<byte> destination, int position, int length)
    {
        if (length < 128)
        {
            destination[position] = (byte)length;
            return position + 1;
        }

        int byteCount = GetDerLengthSize(length) - 1;
        destination[position] = (byte)(128 | byteCount);
        int index = byteCount;
        while (index > 0)
        {
            destination[position + index] = (byte)(length & 255);
            length >>= 8;
            index -= 1;
        }

        return position + byteCount + 1;
    }

    static int WriteInteger(
        ReadOnlySpan<byte> component,
        int valuePosition,
        int valueLength,
        bool needsSignByte,
        Span<byte> destination,
        int position)
    {
        int contentLength = valueLength + (needsSignByte ? 1 : 0);
        destination[position] = 2;
        position = WriteDerLength(destination, position + 1, contentLength);

        if (needsSignByte)
        {
            destination[position] = 0;
            position += 1;
        }

        component.Slice(valuePosition, valueLength).CopyTo(destination.Slice(position));
        return position + valueLength;
    }

    static DerIntegerResult ReadInteger(ReadOnlySpan<byte> source, int position, int componentWidth)
    {
        DerIntegerResult result = new DerIntegerResult();

        if (position >= source.Length || source[position] != 2)
        {
            result.Error = new TokenError(
                "invalid_ecdsa_der",
                "An ECDSA signature component must be a DER INTEGER.",
                position);
            return result;
        }

        DerLengthResult length = ReadDerLength(source, position + 1);
        if (!length.Success)
        {
            result.Error = length.Error;
            return result;
        }

        if (length.Length == 0)
        {
            result.Error = new TokenError(
                "invalid_ecdsa_der",
                "A DER INTEGER must not be empty.",
                length.NextPosition);
            return result;
        }

        if (length.Length > source.Length - length.NextPosition)
        {
            result.Error = new TokenError(
                "invalid_ecdsa_der",
                "A DER INTEGER extends beyond the signature.",
                source.Length);
            return result;
        }

        int valuePosition = length.NextPosition;
        int valueLength = length.Length;
        byte first = source[valuePosition];

        if ((first & 128) != 0)
        {
            result.Error = new TokenError(
                "invalid_ecdsa_der",
                "An ECDSA signature component must be non-negative.",
                valuePosition);
            return result;
        }

        if (valueLength > 1 && first == 0)
        {
            if ((source[valuePosition + 1] & 128) == 0)
            {
                result.Error = new TokenError(
                    "invalid_ecdsa_der",
                    "A DER INTEGER must use its minimal encoding.",
                    valuePosition);
                return result;
            }

            valuePosition += 1;
            valueLength -= 1;
        }

        if (valueLength > componentWidth)
        {
            result.Error = new TokenError(
                "invalid_ecdsa_der",
                "An ECDSA signature component exceeds the requested width.",
                valuePosition);
            return result;
        }

        result.ValuePosition = valuePosition;
        result.ValueLength = valueLength;
        result.NextPosition = length.NextPosition + length.Length;
        result.Success = true;
        return result;
    }

    static DerLengthResult ReadDerLength(ReadOnlySpan<byte> source, int position)
    {
        DerLengthResult result = new DerLengthResult();

        if (position >= source.Length)
        {
            result.Error = new TokenError(
                "invalid_ecdsa_der",
                "A DER length is missing.",
                position);
            return result;
        }

        byte first = source[position];
        if (first < 128)
        {
            result.Length = first;
            result.NextPosition = position + 1;
            result.Success = true;
            return result;
        }

        int lengthByteCount = first & 127;
        if (lengthByteCount == 0)
        {
            result.Error = new TokenError(
                "invalid_ecdsa_der",
                "DER indefinite lengths are not allowed.",
                position);
            return result;
        }

        if (lengthByteCount > 4 || lengthByteCount > source.Length - position - 1)
        {
            result.Error = new TokenError(
                "invalid_ecdsa_der",
                "The DER length is invalid or truncated.",
                position);
            return result;
        }

        if (source[position + 1] == 0)
        {
            result.Error = new TokenError(
                "invalid_ecdsa_der",
                "A DER length must use its minimal encoding.",
                position + 1);
            return result;
        }

        long length = 0;
        int index = 0;
        while (index < lengthByteCount)
        {
            length = (length << 8) | source[position + 1 + index];
            index += 1;
        }

        if (length < 128)
        {
            result.Error = new TokenError(
                "invalid_ecdsa_der",
                "A DER length must use short form when possible.",
                position);
            return result;
        }

        if (length > int.MaxValue)
        {
            result.Error = new TokenError(
                "invalid_ecdsa_der",
                "The DER length exceeds the supported range.",
                position);
            return result;
        }

        result.Length = (int)length;
        result.NextPosition = position + 1 + lengthByteCount;
        result.Success = true;
        return result;
    }

    static TokenBytesResult InvalidDer(string message, int position)
    {
        TokenBytesResult result = new TokenBytesResult();
        result.Error = new TokenError("invalid_ecdsa_der", message, position);
        return result;
    }
}

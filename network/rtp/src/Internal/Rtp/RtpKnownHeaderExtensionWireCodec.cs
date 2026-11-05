using System;
using System.Text;

namespace rtp;

internal static class RtpKnownHeaderExtensionWireCodec
{
    internal static int GetAudioLevelEncodedSize(RtpAudioLevelExtension value)
    {
        string? validationError = GetAudioLevelValidationError(value);
        if (validationError != null)
        {
            throw new RtpPacketFormatException(validationError);
        }

        return 1;
    }

    internal static int EncodeAudioLevel(
        RtpAudioLevelExtension value,
        Span<byte> destination)
    {
        int encodedSize = GetAudioLevelEncodedSize(value);
        if (destination.Length < encodedSize)
        {
            throw new RtpBufferCapacityException(
                "The destination is too small for the client audio-level payload.");
        }

        WriteValidatedAudioLevel(value, destination);
        return encodedSize;
    }

    internal static RtpCodecResult TryEncodeAudioLevel(
        RtpAudioLevelExtension value,
        Span<byte> destination)
    {
        string? validationError = GetAudioLevelValidationError(value);
        if (validationError != null)
        {
            return CreateFailure(validationError);
        }

        if (destination.Length < 1)
        {
            return CreateFailure(
                "The destination is too small for the client audio-level payload.");
        }

        WriteValidatedAudioLevel(value, destination);
        return CreateEncodeSuccess(1);
    }

    internal static RtpAudioLevelExtension DecodeAudioLevel(ReadOnlySpan<byte> source)
    {
        RtpCodecResult result = TryDecodeAudioLevel(source);
        if (!result.Success)
        {
            throw new RtpPacketFormatException(GetFailureMessage(
                result,
                "The client audio-level payload could not be decoded."));
        }

        RtpAudioLevelExtension? value = result.AudioLevelExtension;
        if (value == null)
        {
            throw new RtpPacketFormatException(
                "The client audio-level decoder returned no value.");
        }

        return value;
    }

    internal static RtpCodecResult TryDecodeAudioLevel(ReadOnlySpan<byte> source)
    {
        if (source.Length != 1)
        {
            return CreateFailure(
                "The client audio-level payload must contain exactly one byte.");
        }

        byte encoded = source[0];
        RtpAudioLevelExtension value = new RtpAudioLevelExtension();
        value.VoiceActivity = (encoded & 128) != 0;
        value.Level = (byte)(encoded & 127);

        RtpCodecResult result = CreateDecodeSuccess(1);
        result.AudioLevelExtension = value;
        return result;
    }

    internal static int GetMixerAudioLevelEncodedSize(RtpMixerAudioLevelExtension value)
    {
        string? validationError = GetMixerAudioLevelValidationError(value);
        if (validationError != null)
        {
            throw new RtpPacketFormatException(validationError);
        }

        return value.Levels.Length;
    }

    internal static int EncodeMixerAudioLevel(
        RtpMixerAudioLevelExtension value,
        Span<byte> destination)
    {
        int encodedSize = GetMixerAudioLevelEncodedSize(value);
        if (destination.Length < encodedSize)
        {
            throw new RtpBufferCapacityException(
                "The destination is too small for the mixer audio-level payload.");
        }

        WriteValidatedMixerAudioLevel(value, destination);
        return encodedSize;
    }

    internal static RtpCodecResult TryEncodeMixerAudioLevel(
        RtpMixerAudioLevelExtension value,
        Span<byte> destination)
    {
        string? validationError = GetMixerAudioLevelValidationError(value);
        if (validationError != null)
        {
            return CreateFailure(validationError);
        }

        int encodedSize = value.Levels.Length;
        if (destination.Length < encodedSize)
        {
            return CreateFailure(
                "The destination is too small for the mixer audio-level payload.");
        }

        WriteValidatedMixerAudioLevel(value, destination);
        return CreateEncodeSuccess(encodedSize);
    }

    internal static RtpMixerAudioLevelExtension DecodeMixerAudioLevel(
        ReadOnlySpan<byte> source)
    {
        RtpCodecResult result = TryDecodeMixerAudioLevel(source);
        if (!result.Success)
        {
            throw new RtpPacketFormatException(GetFailureMessage(
                result,
                "The mixer audio-level payload could not be decoded."));
        }

        RtpMixerAudioLevelExtension? value = result.MixerAudioLevelExtension;
        if (value == null)
        {
            throw new RtpPacketFormatException(
                "The mixer audio-level decoder returned no value.");
        }

        return value;
    }

    internal static RtpCodecResult TryDecodeMixerAudioLevel(ReadOnlySpan<byte> source)
    {
        if (source.Length < 1 || source.Length > 15)
        {
            return CreateFailure(
                "The mixer audio-level payload must contain between 1 and 15 bytes.");
        }

        int levelIndex = 0;
        while (levelIndex < source.Length)
        {
            if ((source[levelIndex] & 128) != 0)
            {
                return CreateFailure(
                    "A mixer audio-level payload has its reserved bit set.");
            }

            levelIndex = levelIndex + 1;
        }

        RtpMixerAudioLevelExtension value = new RtpMixerAudioLevelExtension();
        value.Levels = source.ToArray();
        RtpCodecResult result = CreateDecodeSuccess(source.Length);
        result.MixerAudioLevelExtension = value;
        return result;
    }

    internal static int GetNtp64EncodedSize(RtpNtp64Extension value)
    {
        if (value == null)
        {
            throw new RtpPacketFormatException("The NTP-64 value is required.");
        }

        return 8;
    }

    internal static int EncodeNtp64(RtpNtp64Extension value, Span<byte> destination)
    {
        int encodedSize = GetNtp64EncodedSize(value);
        if (destination.Length < encodedSize)
        {
            throw new RtpBufferCapacityException(
                "The destination is too small for the NTP-64 payload.");
        }

        NetworkBinaryWriter.WriteUInt64(destination, 0, value.Timestamp);
        return encodedSize;
    }

    internal static RtpCodecResult TryEncodeNtp64(
        RtpNtp64Extension value,
        Span<byte> destination)
    {
        if (value == null)
        {
            return CreateFailure("The NTP-64 value is required.");
        }

        if (destination.Length < 8)
        {
            return CreateFailure("The destination is too small for the NTP-64 payload.");
        }

        NetworkBinaryWriter.WriteUInt64(destination, 0, value.Timestamp);
        return CreateEncodeSuccess(8);
    }

    internal static RtpNtp64Extension DecodeNtp64(ReadOnlySpan<byte> source)
    {
        RtpCodecResult result = TryDecodeNtp64(source);
        if (!result.Success)
        {
            throw new RtpPacketFormatException(GetFailureMessage(
                result,
                "The NTP-64 payload could not be decoded."));
        }

        RtpNtp64Extension? value = result.Ntp64Extension;
        if (value == null)
        {
            throw new RtpPacketFormatException("The NTP-64 decoder returned no value.");
        }

        return value;
    }

    internal static RtpCodecResult TryDecodeNtp64(ReadOnlySpan<byte> source)
    {
        if (source.Length != 8)
        {
            return CreateFailure("The NTP-64 payload must contain exactly eight bytes.");
        }

        RtpNtp64Extension value = new RtpNtp64Extension();
        value.Timestamp = NetworkBinaryReader.ReadUInt64(source, 0);
        RtpCodecResult result = CreateDecodeSuccess(8);
        result.Ntp64Extension = value;
        return result;
    }

    internal static int GetStreamIdentifierEncodedSize(
        RtpStreamIdentifierExtension value)
    {
        string? validationError = GetStreamIdentifierValidationError(value);
        if (validationError != null)
        {
            throw new RtpPacketFormatException(validationError);
        }

        return Encoding.UTF8.GetByteCount(value.Value);
    }

    internal static int EncodeStreamIdentifier(
        RtpStreamIdentifierExtension value,
        Span<byte> destination)
    {
        int encodedSize = GetStreamIdentifierEncodedSize(value);
        if (destination.Length < encodedSize)
        {
            throw new RtpBufferCapacityException(
                "The destination is too small for the stream-identifier payload.");
        }

        Encoding.UTF8.GetBytes(value.Value.AsSpan(), destination);
        return encodedSize;
    }

    internal static RtpCodecResult TryEncodeStreamIdentifier(
        RtpStreamIdentifierExtension value,
        Span<byte> destination)
    {
        string? validationError = GetStreamIdentifierValidationError(value);
        if (validationError != null)
        {
            return CreateFailure(validationError);
        }

        int encodedSize = Encoding.UTF8.GetByteCount(value.Value);
        if (destination.Length < encodedSize)
        {
            return CreateFailure(
                "The destination is too small for the stream-identifier payload.");
        }

        Encoding.UTF8.GetBytes(value.Value.AsSpan(), destination);
        return CreateEncodeSuccess(encodedSize);
    }

    internal static RtpStreamIdentifierExtension DecodeStreamIdentifier(
        ReadOnlySpan<byte> source,
        string uri)
    {
        RtpCodecResult result = TryDecodeStreamIdentifier(source, uri);
        if (!result.Success)
        {
            throw new RtpPacketFormatException(GetFailureMessage(
                result,
                "The stream-identifier payload could not be decoded."));
        }

        RtpStreamIdentifierExtension? value = result.StreamIdentifierExtension;
        if (value == null)
        {
            throw new RtpPacketFormatException(
                "The URI does not identify a supported typed header extension.");
        }

        return value;
    }

    internal static RtpCodecResult TryDecodeStreamIdentifier(
        ReadOnlySpan<byte> source,
        string uri)
    {
        if (uri == null || uri.Length == 0)
        {
            return CreateFailure("The header-extension URI is required.");
        }

        if (!IsStreamIdentifierUri(uri))
        {
            RtpCodecResult rawResult = CreateDecodeSuccess(source.Length);
            rawResult.Data = source.ToArray();
            return rawResult;
        }

        if (source.Length < 1 || source.Length > 255)
        {
            return CreateFailure(
                "A stream-identifier payload must contain between 1 and 255 bytes.");
        }

        if (string.Equals(uri, RtpHeaderExtensionUris.MediaIdentifier,
            StringComparison.Ordinal))
        {
            if (!IsValidUtf8(source))
            {
                return CreateFailure("The MID payload is not valid UTF-8.");
            }
        }
        else if (!ContainsOnlyAsciiAlphaNumeric(source))
        {
            return CreateFailure(
                "RID and repaired-RID payloads may contain only ASCII alphanumeric bytes.");
        }

        RtpStreamIdentifierExtension value = new RtpStreamIdentifierExtension();
        value.Uri = uri;
        value.Value = Encoding.UTF8.GetString(source);
        RtpCodecResult result = CreateDecodeSuccess(source.Length);
        result.StreamIdentifierExtension = value;
        return result;
    }

    internal static string? GetAudioLevelValidationError(RtpAudioLevelExtension value)
    {
        if (value == null)
        {
            return "The client audio-level value is required.";
        }

        if (value.Level > 127)
        {
            return "The client audio level must be between 0 and 127.";
        }

        return null;
    }

    internal static string? GetMixerAudioLevelValidationError(
        RtpMixerAudioLevelExtension value)
    {
        if (value == null)
        {
            return "The mixer audio-level value is required.";
        }

        if (value.Levels == null)
        {
            return "The mixer audio-level collection is required.";
        }

        if (value.Levels.Length < 1 || value.Levels.Length > 15)
        {
            return "The mixer audio-level collection must contain between 1 and 15 values.";
        }

        int levelIndex = 0;
        while (levelIndex < value.Levels.Length)
        {
            if (value.Levels[levelIndex] > 127)
            {
                return "Every mixer audio level must be between 0 and 127.";
            }

            levelIndex = levelIndex + 1;
        }

        return null;
    }

    internal static string? GetStreamIdentifierValidationError(
        RtpStreamIdentifierExtension value)
    {
        if (value == null)
        {
            return "The stream-identifier value is required.";
        }

        if (!IsStreamIdentifierUri(value.Uri))
        {
            return "The URI does not identify a supported stream-identifier extension.";
        }

        if (value.Value == null || value.Value.Length == 0)
        {
            return "The stream identifier is required.";
        }

        if (!HasValidUtf16(value.Value))
        {
            return "The stream identifier contains an invalid Unicode sequence.";
        }

        int encodedSize = Encoding.UTF8.GetByteCount(value.Value);
        if (encodedSize > 255)
        {
            return "The stream identifier may contain at most 255 encoded bytes.";
        }

        if (!string.Equals(value.Uri, RtpHeaderExtensionUris.MediaIdentifier,
            StringComparison.Ordinal) && !ContainsOnlyAsciiAlphaNumericText(value.Value))
        {
            return "RID and repaired-RID values may contain only ASCII alphanumeric characters.";
        }

        return null;
    }

    internal static bool IsStreamIdentifierUri(string uri)
    {
        return string.Equals(uri, RtpHeaderExtensionUris.MediaIdentifier,
            StringComparison.Ordinal) ||
            string.Equals(uri, RtpHeaderExtensionUris.RtpStreamIdentifier,
                StringComparison.Ordinal) ||
            string.Equals(uri, RtpHeaderExtensionUris.RepairedRtpStreamIdentifier,
                StringComparison.Ordinal);
    }

    internal static bool HasValidUtf16(string value)
    {
        int valueIndex = 0;
        while (valueIndex < value.Length)
        {
            int codeUnit = value[valueIndex];
            if (codeUnit >= 55296 && codeUnit <= 56319)
            {
                if (valueIndex + 1 >= value.Length)
                {
                    return false;
                }

                int nextCodeUnit = value[valueIndex + 1];
                if (nextCodeUnit < 56320 || nextCodeUnit > 57343)
                {
                    return false;
                }

                valueIndex = valueIndex + 2;
            }
            else
            {
                if (codeUnit >= 56320 && codeUnit <= 57343)
                {
                    return false;
                }

                valueIndex = valueIndex + 1;
            }
        }

        return true;
    }

    internal static bool IsValidUtf8(ReadOnlySpan<byte> source)
    {
        int sourceIndex = 0;
        while (sourceIndex < source.Length)
        {
            byte first = source[sourceIndex];
            if (first <= 127)
            {
                sourceIndex = sourceIndex + 1;
            }
            else if (first >= 194 && first <= 223)
            {
                if (!HasContinuationBytes(source, sourceIndex, 1))
                {
                    return false;
                }

                sourceIndex = sourceIndex + 2;
            }
            else if (first >= 224 && first <= 239)
            {
                if (!HasContinuationBytes(source, sourceIndex, 2))
                {
                    return false;
                }

                byte second = source[sourceIndex + 1];
                if ((first == 224 && second < 160) ||
                    (first == 237 && second > 159))
                {
                    return false;
                }

                sourceIndex = sourceIndex + 3;
            }
            else if (first >= 240 && first <= 244)
            {
                if (!HasContinuationBytes(source, sourceIndex, 3))
                {
                    return false;
                }

                byte second = source[sourceIndex + 1];
                if ((first == 240 && second < 144) ||
                    (first == 244 && second > 143))
                {
                    return false;
                }

                sourceIndex = sourceIndex + 4;
            }
            else
            {
                return false;
            }
        }

        return true;
    }

    internal static bool HasContinuationBytes(
        ReadOnlySpan<byte> source,
        int sourceIndex,
        int continuationCount)
    {
        if (continuationCount > source.Length - sourceIndex - 1)
        {
            return false;
        }

        int continuationIndex = 1;
        while (continuationIndex <= continuationCount)
        {
            byte value = source[sourceIndex + continuationIndex];
            if (value < 128 || value > 191)
            {
                return false;
            }

            continuationIndex = continuationIndex + 1;
        }

        return true;
    }

    internal static bool ContainsOnlyAsciiAlphaNumeric(ReadOnlySpan<byte> source)
    {
        int sourceIndex = 0;
        while (sourceIndex < source.Length)
        {
            byte value = source[sourceIndex];
            if (!IsAsciiAlphaNumeric(value))
            {
                return false;
            }

            sourceIndex = sourceIndex + 1;
        }

        return true;
    }

    internal static bool ContainsOnlyAsciiAlphaNumericText(string value)
    {
        int valueIndex = 0;
        while (valueIndex < value.Length)
        {
            int codeUnit = value[valueIndex];
            if (codeUnit > 127 || !IsAsciiAlphaNumeric((byte)codeUnit))
            {
                return false;
            }

            valueIndex = valueIndex + 1;
        }

        return true;
    }

    internal static bool IsAsciiAlphaNumeric(byte value)
    {
        return (value >= 48 && value <= 57) ||
            (value >= 65 && value <= 90) ||
            (value >= 97 && value <= 122);
    }

    internal static void WriteValidatedAudioLevel(
        RtpAudioLevelExtension value,
        Span<byte> destination)
    {
        byte encoded = value.Level;
        if (value.VoiceActivity)
        {
            encoded = (byte)(encoded | 128);
        }

        destination[0] = encoded;
    }

    internal static void WriteValidatedMixerAudioLevel(
        RtpMixerAudioLevelExtension value,
        Span<byte> destination)
    {
        int levelIndex = 0;
        while (levelIndex < value.Levels.Length)
        {
            destination[levelIndex] = value.Levels[levelIndex];
            levelIndex = levelIndex + 1;
        }
    }

    internal static RtpCodecResult CreateFailure(string errorMessage)
    {
        RtpCodecResult result = new RtpCodecResult();
        result.ErrorMessage = errorMessage;
        return result;
    }

    internal static RtpCodecResult CreateEncodeSuccess(int writtenCount)
    {
        RtpCodecResult result = new RtpCodecResult();
        result.Success = true;
        result.WrittenCount = writtenCount;
        return result;
    }

    internal static RtpCodecResult CreateDecodeSuccess(int consumedCount)
    {
        RtpCodecResult result = new RtpCodecResult();
        result.Success = true;
        result.ConsumedCount = consumedCount;
        return result;
    }

    internal static string GetFailureMessage(RtpCodecResult result, string fallback)
    {
        if (result.ErrorMessage != null)
        {
            return result.ErrorMessage;
        }

        return fallback;
    }
}

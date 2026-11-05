using System;

namespace uuid;

internal class UuidTimestamp
{
    internal const long GregorianUnixOffsetTicks = 122192928000000000;

    internal static IdentifierTimestampResult Extract(UuidValue? value)
    {
        if (value == null)
        {
            return IdentifierTimestampResult.Failure(IdentifierErrorCode.NullInput, "UUID value is null");
        }

        if (!value.HasRfc9562Variant())
        {
            return IdentifierTimestampResult.Failure(IdentifierErrorCode.InvalidUuidVariant, "UUID does not use RFC 9562 variant bits");
        }

        UuidVersion version = value.Version();
        if (version != UuidVersion.Version1 && version != UuidVersion.Version6 && version != UuidVersion.Version7)
        {
            return IdentifierTimestampResult.Failure(IdentifierErrorCode.InvalidUuidVersion, "UUID version does not carry a supported timestamp");
        }

        long unixMilliseconds = ExtractUnixMilliseconds(value);
        return Identifier.TimestampFromUnixMilliseconds(IdentifierKind.Uuid, unixMilliseconds);
    }

    internal static IdentifierTimestampResult Extract(string? text)
    {
        UuidParseResult parse = UuidParser.Parse(text);
        if (!parse.Ok || parse.Value == null)
        {
            return IdentifierTimestampResult.Failure(parse.Code, parse.Error);
        }

        return Extract(parse.Value);
    }

    internal static long ExtractUnixMilliseconds(UuidValue? value)
    {
        if (value == null)
        {
            throw new ArgumentNullException("value");
        }

        if (!value.HasRfc9562Variant())
        {
            throw new IdentifierException(IdentifierErrorCode.InvalidUuidVariant, "UUID does not use RFC 9562 variant bits");
        }

        UuidVersion version = value.Version();
        if (version == UuidVersion.Version1)
        {
            return GregorianTimestampToUnixMilliseconds(ExtractV1GregorianTimestamp(value));
        }

        if (version == UuidVersion.Version6)
        {
            return GregorianTimestampToUnixMilliseconds(ExtractV6GregorianTimestamp(value));
        }

        if (version == UuidVersion.Version7)
        {
            return ExtractV7UnixMilliseconds(value);
        }

        throw new IdentifierException(IdentifierErrorCode.InvalidUuidVersion, "UUID version does not carry a supported timestamp");
    }

    internal static long ExtractV1GregorianTimestamp(UuidValue? value)
    {
        if (value == null)
        {
            throw new ArgumentNullException("value");
        }

        if (value.Version() != UuidVersion.Version1)
        {
            throw new InvalidOperationException("UUID value is not version 1");
        }

        return UuidConverter.V1Timestamp(value.ToBytes());
    }

    internal static long ExtractV6GregorianTimestamp(UuidValue? value)
    {
        if (value == null)
        {
            throw new ArgumentNullException("value");
        }

        if (value.Version() != UuidVersion.Version6)
        {
            throw new InvalidOperationException("UUID value is not version 6");
        }

        return UuidConverter.V6Timestamp(value.ToBytes());
    }

    internal static long ExtractV7UnixMilliseconds(UuidValue? value)
    {
        if (value == null)
        {
            throw new ArgumentNullException("value");
        }

        if (value.Version() != UuidVersion.Version7)
        {
            throw new InvalidOperationException("UUID value is not version 7");
        }

        return BigEndianTools.ReadUInt48(value.ToBytes(), 0);
    }

    internal static long GregorianTimestampToUnixMilliseconds(long gregorianTimestamp)
    {
        long ticksSinceUnixEpoch = gregorianTimestamp - GregorianUnixOffsetTicks;
        if (ticksSinceUnixEpoch >= 0)
        {
            return ticksSinceUnixEpoch / 10000;
        }

        return (ticksSinceUnixEpoch - 9999) / 10000;
    }
}

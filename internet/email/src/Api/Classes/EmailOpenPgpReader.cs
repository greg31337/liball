using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;

namespace email;
/// <summary>
/// Reads and interprets openPGP.
/// </summary>
public class EmailOpenPgpReader : IEmailOpenPgpReaderContract
{
    internal static EmailOpenPgpReadResult ParseImplementationWithInput(ReadOnlySpan<byte> input)
    {
        return ParseImplementationWithInputAndOptions(input, new EmailOpenPgpReaderOptions());
    }

    /// <summary>
    /// Parses the supplied email data.
    /// </summary>
    /// <param name = "input">The input stream or data to process.</param>
    /// <returns>A result object containing the produced value, status, and any diagnostics defined by the operation.</returns>
    public EmailOpenPgpReadResult ParseWithInput(ReadOnlySpan<byte> input)
    {
        return EmailOpenPgpReader.ParseImplementationWithInput(input);
    }

    internal static EmailOpenPgpReadResult ParseImplementationWithInputAndOptions(ReadOnlySpan<byte> input, EmailOpenPgpReaderOptions options)
    {
        if (options == null)
            throw new ArgumentNullException("options");
        EmailOpenPgpReaderOptions snapshot = options.Clone();
        EmailCryptographicLimits limits = snapshot.GetLimits();
        if (input.Length > limits.GetMaxInputBytes())
            throw new EmailLimitException("MaxOpenPgpInputBytes", limits.GetMaxInputBytes(), input.Length);
        byte[] original = input.ToArray();
        byte[] packetBytes = original;
        EmailIssueList issues = new EmailIssueList();
        EmailOpenPgpArmorResult? armor = null;
        if (LooksArmored(original))
        {
            armor = new EmailOpenPgpArmor().DecodeWithInputAndOptions(original, snapshot);
            issues.AddRange(armor.GetIssues());
            if (!armor.IsSuccess())
                return new EmailOpenPgpReadResult(new List<EmailOpenPgpPacket>(), armor, original, issues, false);
            packetBytes = armor.GetData();
        }

        if (packetBytes.Length == 0)
        {
            issues.Add(Issue(EmailIssueSeverity.Error, EmailIssueCode.OpenPgpPacketMalformed, "The OpenPGP packet sequence is empty.", 0, "No packets were parsed."));
            return new EmailOpenPgpReadResult(new List<EmailOpenPgpPacket>(), armor, original, issues, false);
        }

        List<EmailOpenPgpPacket> packets = new List<EmailOpenPgpPacket>();
        int offset = 0;
        bool complete = true;
        while (offset < packetBytes.Length)
        {
            if (packets.Count >= limits.GetMaxOpenPgpPacketCount())
                throw new EmailLimitException("MaxOpenPgpPacketCount", limits.GetMaxOpenPgpPacketCount(), packets.Count + 1);
            EmailOpenPgpFrameRead frameRead = ReadFrame(packetBytes, offset, snapshot.GetAllowLegacyPacketHeaders(), limits.GetMaxInputBytes());
            if (!frameRead.IsSuccess())
            {
                issues.Add(Issue(EmailIssueSeverity.Error, EmailIssueCode.OpenPgpPacketMalformed, frameRead.GetError(), offset, "Parsing stopped at the malformed packet boundary."));
                complete = false;
                break;
            }

            EmailOpenPgpFrame? frame = frameRead.GetFrame();
            EmailOpenPgpTypedRead typed = ParseTyped((frame ?? throw new InvalidOperationException()).GetTypeIdentifier(), frame.GetBody(), frame.HasLegacyHeader());
            if (!typed.IsSuccess())
            {
                issues.Add(Issue(EmailIssueSeverity.Error, EmailIssueCode.OpenPgpPacketMalformed, "The recognized OpenPGP packet body is malformed.", offset, "The packet bytes were retained without exposing typed fields."));
            }

            EmailOpenPgpPacketKind kind = Kind(frame.GetTypeIdentifier());
            if (kind == EmailOpenPgpPacketKind.Unknown)
            {
                EmailIssueSeverity severity = frame.GetTypeIdentifier() < 40 ? EmailIssueSeverity.Error : EmailIssueSeverity.Warning;
                issues.Add(Issue(severity, EmailIssueCode.OpenPgpPacketUnsupported, "The OpenPGP packet type is unknown.", offset, "The complete packet body and encoded bytes were retained."));
            }

            packets.Add(new EmailOpenPgpPacket(frame.GetTypeIdentifier(), frame.GetBody(), frame.GetEncodedBytes(), frame.HasLegacyHeader(), frame.UsesPartialLengths(), offset, typed.GetValue()));
            offset += frame.GetEncodedBytes().Length;
        }

        return new EmailOpenPgpReadResult(packets, armor, original, issues, complete && offset == packetBytes.Length);
    }

    /// <summary>
    /// Parses the supplied email data.
    /// </summary>
    /// <param name = "input">The input stream or data to process.</param>
    /// <param name = "options">The policy settings that control this operation.</param>
    /// <returns>A result object containing the produced value, status, and any diagnostics defined by the operation.</returns>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    /// <exception cref = "EmailLimitException">Thrown when processing would exceed a configured hard resource limit.</exception>
    public EmailOpenPgpReadResult ParseWithInputAndOptions(ReadOnlySpan<byte> input, EmailOpenPgpReaderOptions options)
    {
        return EmailOpenPgpReader.ParseImplementationWithInputAndOptions(input, options);
    }

    static EmailOpenPgpFrameRead ReadFrame(byte[] input, int start, bool allowLegacy, long maximumBodyBytes)
    {
        int position = start;
        int header = input[position++];
        if ((header & 0x80) == 0)
            return EmailOpenPgpFrameRead.Failure("An OpenPGP packet header must have its high bit set.");
        bool current = (header & 0x40) != 0;
        int typeIdentifier = current ? header & 0x3F : (header >> 2) & 0x0F;
        if (!current)
        {
            if (!allowLegacy)
                return EmailOpenPgpFrameRead.Failure("Legacy OpenPGP packet headers are disabled.");
            int lengthType = header & 3;
            ulong bodyLength;
            if (lengthType == 3)
            {
                bodyLength = (ulong)(input.Length - position);
            }
            else
            {
                int lengthBytes = lengthType == 0 ? 1 : lengthType == 1 ? 2 : 4;
                if (position > input.Length - lengthBytes)
                    return EmailOpenPgpFrameRead.Failure("The legacy OpenPGP packet length is truncated.");
                bodyLength = ReadUnsigned(input, position, lengthBytes);
                position += lengthBytes;
            }

            if (bodyLength > (ulong)maximumBodyBytes)
                throw new EmailLimitException("MaxOpenPgpPacketBodyBytes", maximumBodyBytes, ToLimitValue(bodyLength));
            if (bodyLength > (ulong)(input.Length - position))
                return EmailOpenPgpFrameRead.Failure("The legacy OpenPGP packet body is truncated.");
            int count = (int)bodyLength;
            byte[] body = Slice(input, position, count);
            position += count;
            return EmailOpenPgpFrameRead.Success(new EmailOpenPgpFrame(typeIdentifier, body, Slice(input, start, position - start), true, false));
        }

        EmailOpenPgpLengthRead first = ReadCurrentLength(input, position);
        if (!first.IsSuccess())
            return EmailOpenPgpFrameRead.Failure("The OpenPGP packet length is truncated or invalid.");
        ulong firstLength = first.GetLength();
        bool partial = first.IsPartial();
        position = first.GetPosition();
        if (!partial)
        {
            if (firstLength > (ulong)maximumBodyBytes)
                throw new EmailLimitException("MaxOpenPgpPacketBodyBytes", maximumBodyBytes, ToLimitValue(firstLength));
            if (firstLength > (ulong)(input.Length - position))
                return EmailOpenPgpFrameRead.Failure("The OpenPGP packet body is truncated.");
            int count = (int)firstLength;
            byte[] body = Slice(input, position, count);
            position += count;
            return EmailOpenPgpFrameRead.Success(new EmailOpenPgpFrame(typeIdentifier, body, Slice(input, start, position - start), false, false));
        }

        if (!AllowsPartialLengths(typeIdentifier))
            return EmailOpenPgpFrameRead.Failure("Partial body lengths are not permitted for this OpenPGP packet type.");
        if (firstLength < 512)
            return EmailOpenPgpFrameRead.Failure("The first OpenPGP partial body length must be at least 512 octets.");
        using (MemoryStream bodyStream = new MemoryStream())
        {
            ulong partLength = firstLength;
            bool partIsPartial = true;
            while (true)
            {
                if (partLength > (ulong)(input.Length - position))
                    return EmailOpenPgpFrameRead.Failure("An OpenPGP partial body segment is truncated.");
                long observed = bodyStream.Length + ToLimitValue(partLength);
                if (observed > maximumBodyBytes)
                    throw new EmailLimitException("MaxOpenPgpPacketBodyBytes", maximumBodyBytes, observed);
                int count = (int)partLength;
                bodyStream.Write(input, position, count);
                position += count;
                if (!partIsPartial)
                    break;
                EmailOpenPgpLengthRead next = ReadCurrentLength(input, position);
                if (!next.IsSuccess())
                    return EmailOpenPgpFrameRead.Failure("The final OpenPGP partial body length is missing or truncated.");
                partLength = next.GetLength();
                partIsPartial = next.IsPartial();
                position = next.GetPosition();
            }

            return EmailOpenPgpFrameRead.Success(new EmailOpenPgpFrame(typeIdentifier, bodyStream.ToArray(), Slice(input, start, position - start), false, true));
        }
    }

    static EmailOpenPgpLengthRead ReadCurrentLength(byte[] input, int position)
    {
        if (position >= input.Length)
            return EmailOpenPgpLengthRead.Failure(position);
        int first = input[position++];
        if (first < 192)
            return EmailOpenPgpLengthRead.Success(position, (ulong)first, false);
        if (first <= 223)
        {
            if (position >= input.Length)
                return EmailOpenPgpLengthRead.Failure(position);
            ulong length = (ulong)(((first - 192) << 8) + input[position++] + 192);
            return EmailOpenPgpLengthRead.Success(position, length, false);
        }

        if (first < 255)
        {
            return EmailOpenPgpLengthRead.Success(position, 1UL << (first & 0x1F), true);
        }

        if (position > input.Length - 4)
            return EmailOpenPgpLengthRead.Failure(position);
        ulong finalLength = ReadUnsigned(input, position, 4);
        position += 4;
        return EmailOpenPgpLengthRead.Success(position, finalLength, false);
    }

    static EmailOpenPgpTypedRead ParseTyped(int typeIdentifier, byte[] body, bool legacyHeader)
    {
        if (typeIdentifier == 11)
            return ParseLiteral(body);
        if (typeIdentifier == 8)
            return ParseCompressed(body);
        if (typeIdentifier == 2)
            return ParseSignature(body);
        if (typeIdentifier == 5 || typeIdentifier == 6 || typeIdentifier == 7 || typeIdentifier == 14)
            return ParseKey(typeIdentifier, body);
        if (typeIdentifier == 1 || typeIdentifier == 3)
            return ParseSessionKey(typeIdentifier, body);
        return EmailOpenPgpTypedRead.Success(null);
    }

    static EmailOpenPgpTypedRead ParseLiteral(byte[] body)
    {
        if (body.Length < 6)
            return EmailOpenPgpTypedRead.Failure();
        int nameLength = body[1];
        if (body.Length < 6 + nameLength)
            return EmailOpenPgpTypedRead.Failure();
        byte[] name = Slice(body, 2, nameLength);
        uint timestamp = (uint)ReadUnsigned(body, 2 + nameLength, 4);
        byte[] data = Slice(body, 6 + nameLength, body.Length - 6 - nameLength);
        return EmailOpenPgpTypedRead.Success(new EmailOpenPgpLiteralData(body[0], name, timestamp, data));
    }

    static EmailOpenPgpTypedRead ParseCompressed(byte[] body)
    {
        if (body.Length < 1)
            return EmailOpenPgpTypedRead.Failure();
        return EmailOpenPgpTypedRead.Success(new EmailOpenPgpCompressedData(body[0], Slice(body, 1, body.Length - 1)));
    }

    static EmailOpenPgpTypedRead ParseSignature(byte[] body)
    {
        if (body.Length < 1)
            return EmailOpenPgpTypedRead.Failure();
        int version = body[0];
        if (version == 3)
        {
            if (body.Length < 19 || body[1] != 5)
                return EmailOpenPgpTypedRead.Failure();
            return EmailOpenPgpTypedRead.Success(new EmailOpenPgpSignaturePacket(3, body[2], body[15], body[16], Slice(body, 2, 5), new byte[0], Slice(body, 17, 2), new byte[0], Slice(body, 19, body.Length - 19), Slice(body, 7, 8), new List<EmailOpenPgpSignatureSubpacket>(), new List<EmailOpenPgpSignatureSubpacket>()));
        }

        if (version != 4 && version != 6)
            return EmailOpenPgpTypedRead.Failure();
        int lengthBytes = version == 4 ? 2 : 4;
        int position = 4;
        EmailOpenPgpScalarRead hashedLengthRead = ReadScalar(body, position, lengthBytes);
        if (!hashedLengthRead.IsSuccess())
            return EmailOpenPgpTypedRead.Failure();
        ulong hashedLength = hashedLengthRead.GetValue();
        position = hashedLengthRead.GetPosition();
        if (hashedLength > (ulong)(body.Length - position))
            return EmailOpenPgpTypedRead.Failure();
        byte[] hashed = Slice(body, position, (int)hashedLength);
        EmailOpenPgpSubpacketRead parsedHashedRead = ParseSignatureSubpackets(hashed);
        if (!parsedHashedRead.IsSuccess())
            return EmailOpenPgpTypedRead.Failure();
        List<EmailOpenPgpSignatureSubpacket>? parsedHashed = parsedHashedRead.GetSubpackets();
        position += (int)hashedLength;
        EmailOpenPgpScalarRead unhashedLengthRead = ReadScalar(body, position, lengthBytes);
        if (!unhashedLengthRead.IsSuccess())
            return EmailOpenPgpTypedRead.Failure();
        ulong unhashedLength = unhashedLengthRead.GetValue();
        position = unhashedLengthRead.GetPosition();
        if (unhashedLength > (ulong)(body.Length - position))
            return EmailOpenPgpTypedRead.Failure();
        byte[] unhashed = Slice(body, position, (int)unhashedLength);
        EmailOpenPgpSubpacketRead parsedUnhashedRead = ParseSignatureSubpackets(unhashed);
        if (!parsedUnhashedRead.IsSuccess())
            return EmailOpenPgpTypedRead.Failure();
        List<EmailOpenPgpSignatureSubpacket>? parsedUnhashed = parsedUnhashedRead.GetSubpackets();
        position += (int)unhashedLength;
        if (position > body.Length - 2)
            return EmailOpenPgpTypedRead.Failure();
        byte[] prefix = Slice(body, position, 2);
        position += 2;
        byte[] salt = new byte[0];
        if (version == 6)
        {
            if (position >= body.Length)
                return EmailOpenPgpTypedRead.Failure();
            int saltLength = body[position++];
            if (saltLength > body.Length - position)
                return EmailOpenPgpTypedRead.Failure();
            salt = Slice(body, position, saltLength);
            position += saltLength;
        }

        return EmailOpenPgpTypedRead.Success(new EmailOpenPgpSignaturePacket(version, body[1], body[2], body[3], hashed, unhashed, prefix, salt, Slice(body, position, body.Length - position), new byte[0], parsedHashed, parsedUnhashed));
    }

    static EmailOpenPgpSubpacketRead ParseSignatureSubpackets(byte[] input)
    {
        List<EmailOpenPgpSignatureSubpacket> subpackets = new List<EmailOpenPgpSignatureSubpacket>();
        int position = 0;
        while (position < input.Length)
        {
            int start = position;
            EmailOpenPgpLengthRead lengthRead = ReadCurrentLength(input, position);
            if (!lengthRead.IsSuccess())
                return EmailOpenPgpSubpacketRead.Failure();
            ulong length = lengthRead.GetLength();
            position = lengthRead.GetPosition();
            if (lengthRead.IsPartial() || length < 1 || length > (ulong)(input.Length - position))
                return EmailOpenPgpSubpacketRead.Failure();
            int count = (int)length;
            int type = input[position];
            subpackets.Add(new EmailOpenPgpSignatureSubpacket(type & 0x7F, (type & 0x80) != 0, Slice(input, position + 1, count - 1), Slice(input, start, position - start + count)));
            position += count;
        }

        return EmailOpenPgpSubpacketRead.Success(subpackets);
    }

    static EmailOpenPgpTypedRead ParseKey(int typeIdentifier, byte[] body)
    {
        if (body.Length < 1)
            return EmailOpenPgpTypedRead.Failure();
        bool secret = typeIdentifier == 5 || typeIdentifier == 7;
        bool subkey = typeIdentifier == 7 || typeIdentifier == 14;
        int version = body[0];
        uint creation;
        int algorithm;
        int publicBodyLength;
        byte[] publicMaterial;
        byte[] fingerprint;
        byte[] keyIdentifier;
        if (version == 2 || version == 3)
        {
            if (body.Length < 8)
                return EmailOpenPgpTypedRead.Failure();
            creation = (uint)ReadUnsigned(body, 1, 4);
            algorithm = body[7];
            if (algorithm != 1 && algorithm != 2 && algorithm != 3)
                return EmailOpenPgpTypedRead.Failure();
            int position = 8;
            int firstStart = position;
            EmailOpenPgpMpiRead modulusRead = ReadMpi(body, position);
            if (!modulusRead.IsSuccess())
                return EmailOpenPgpTypedRead.Failure();
            byte[]? modulus = modulusRead.GetValue();
            position = modulusRead.GetPosition();
            EmailOpenPgpMpiRead exponentRead = ReadMpi(body, position);
            if (!exponentRead.IsSuccess())
                return EmailOpenPgpTypedRead.Failure();
            byte[]? exponent = exponentRead.GetValue();
            position = exponentRead.GetPosition();
            publicBodyLength = position;
            if (!secret && position != body.Length)
                return EmailOpenPgpTypedRead.Failure();
            publicMaterial = Slice(body, firstStart, position - firstStart);
            byte[] md5Material = new byte[(modulus ?? throw new InvalidOperationException()).Length + (exponent ?? throw new InvalidOperationException()).Length];
            modulus.CopyTo(md5Material, 0);
            exponent.CopyTo(md5Material, modulus.Length);
            fingerprint = MD5.HashData(md5Material);
            keyIdentifier = LastBytes(modulus, 8);
        }
        else if (version == 4)
        {
            if (body.Length < 6)
                return EmailOpenPgpTypedRead.Failure();
            creation = (uint)ReadUnsigned(body, 1, 4);
            algorithm = body[5];
            if (secret)
            {
                int position = 6;
                int skippedPosition = SkipV4PublicMaterial(body, algorithm, position);
                if (skippedPosition < 0)
                    return EmailOpenPgpTypedRead.Failure();
                publicBodyLength = skippedPosition;
            }
            else
                publicBodyLength = body.Length;
            if (publicBodyLength > 65535)
                return EmailOpenPgpTypedRead.Failure();
            publicMaterial = Slice(body, 6, publicBodyLength - 6);
            fingerprint = Fingerprint(body, publicBodyLength, 4);
            keyIdentifier = Slice(fingerprint, fingerprint.Length - 8, 8);
        }
        else if (version == 6)
        {
            if (body.Length < 10)
                return EmailOpenPgpTypedRead.Failure();
            creation = (uint)ReadUnsigned(body, 1, 4);
            algorithm = body[5];
            ulong materialLength = ReadUnsigned(body, 6, 4);
            if (materialLength > (ulong)(body.Length - 10))
                return EmailOpenPgpTypedRead.Failure();
            publicBodyLength = 10 + (int)materialLength;
            if (!secret && publicBodyLength != body.Length)
                return EmailOpenPgpTypedRead.Failure();
            publicMaterial = Slice(body, 10, (int)materialLength);
            fingerprint = Fingerprint(body, publicBodyLength, 6);
            keyIdentifier = Slice(fingerprint, 0, 8);
        }
        else
            return EmailOpenPgpTypedRead.Failure();
        return EmailOpenPgpTypedRead.Success(new EmailOpenPgpKeyPacket(version, creation, algorithm, publicMaterial, fingerprint, keyIdentifier, secret, subkey));
    }

    static EmailOpenPgpTypedRead ParseSessionKey(int typeIdentifier, byte[] body)
    {
        if (body.Length < 1)
            return EmailOpenPgpTypedRead.Failure();
        if (typeIdentifier == 1)
        {
            int version = body[0];
            if (version == 3)
            {
                if (body.Length < 10)
                    return EmailOpenPgpTypedRead.Failure();
                return EmailOpenPgpTypedRead.Success(new EmailOpenPgpSessionKeyPacket(3, true, body[9], -1, -1, Slice(body, 1, 8), new byte[0], Slice(body, 10, body.Length - 10)));
            }

            if (version != 6 || body.Length < 3)
                return EmailOpenPgpTypedRead.Failure();
            int identifierLength = body[1];
            if (identifierLength > body.Length - 3)
                return EmailOpenPgpTypedRead.Failure();
            byte[] identifier = new byte[0];
            if (identifierLength != 0)
            {
                if (identifierLength != 21 && identifierLength != 33)
                    return EmailOpenPgpTypedRead.Failure();
                int keyVersion = body[2];
                if (keyVersion == 4 && identifierLength != 21 || keyVersion == 6 && identifierLength != 33 || keyVersion != 4 && keyVersion != 6)
                    return EmailOpenPgpTypedRead.Failure();
                identifier = Slice(body, 3, identifierLength - 1);
            }

            int algorithmOffset = 2 + identifierLength;
            if (algorithmOffset >= body.Length)
                return EmailOpenPgpTypedRead.Failure();
            return EmailOpenPgpTypedRead.Success(new EmailOpenPgpSessionKeyPacket(6, true, body[algorithmOffset], -1, -1, identifier, new byte[0], Slice(body, algorithmOffset + 1, body.Length - algorithmOffset - 1)));
        }

        int symmetricVersion = body[0];
        if (symmetricVersion == 4)
        {
            if (body.Length < 3)
                return EmailOpenPgpTypedRead.Failure();
            int version4S2kLength = GetStringToKeyLength(body, 2);
            if (version4S2kLength < 0)
                return EmailOpenPgpTypedRead.Failure();
            return EmailOpenPgpTypedRead.Success(new EmailOpenPgpSessionKeyPacket(4, false, -1, body[1], -1, new byte[0], Slice(body, 2, version4S2kLength), Slice(body, 2 + version4S2kLength, body.Length - 2 - version4S2kLength)));
        }

        if (symmetricVersion != 6 || body.Length < 5)
            return EmailOpenPgpTypedRead.Failure();
        int fieldsLength = body[1];
        if (fieldsLength < 3 || fieldsLength > body.Length - 2)
            return EmailOpenPgpTypedRead.Failure();
        int symmetricAlgorithm = body[2];
        int aeadAlgorithm = body[3];
        int s2kLength = body[4];
        if (s2kLength > fieldsLength - 3)
            return EmailOpenPgpTypedRead.Failure();
        if (5 + s2kLength > 2 + fieldsLength)
            return EmailOpenPgpTypedRead.Failure();
        return EmailOpenPgpTypedRead.Success(new EmailOpenPgpSessionKeyPacket(6, false, -1, symmetricAlgorithm, aeadAlgorithm, new byte[0], Slice(body, 5, s2kLength), Slice(body, 2 + fieldsLength, body.Length - 2 - fieldsLength)));
    }

    static int GetStringToKeyLength(byte[] input, int offset)
    {
        if (offset >= input.Length)
            return -1;
        int type = input[offset];
        int length;
        if (type == 0)
            length = 2;
        else if (type == 1)
            length = 10;
        else if (type == 3)
            length = 11;
        else if (type == 4)
            length = 20;
        else
            return -1;
        return length <= input.Length - offset ? length : -1;
    }

    static int SkipV4PublicMaterial(byte[] body, int algorithm, int position)
    {
        int count = algorithm == 1 || algorithm == 2 || algorithm == 3 ? 2 : algorithm == 16 ? 3 : algorithm == 17 ? 4 : 0;
        int index = 0;
        while (index < count)
        {
            EmailOpenPgpMpiRead ignored = ReadMpi(body, position);
            if (!ignored.IsSuccess())
                return -1;
            position = ignored.GetPosition();
            index++;
        }

        if (count > 0)
            return position;
        if (algorithm == 18 || algorithm == 19 || algorithm == 22)
        {
            if (position >= body.Length)
                return -1;
            int oidLength = body[position++];
            if (oidLength > body.Length - position)
                return -1;
            position += oidLength;
            EmailOpenPgpMpiRead ignored = ReadMpi(body, position);
            if (!ignored.IsSuccess())
                return -1;
            position = ignored.GetPosition();
            if (algorithm == 18)
            {
                if (position >= body.Length)
                    return -1;
                int kdfLength = body[position++];
                if (kdfLength > body.Length - position)
                    return -1;
                position += kdfLength;
            }

            return position;
        }

        return -1;
    }

    static EmailOpenPgpMpiRead ReadMpi(byte[] input, int position)
    {
        int initialPosition = position;
        if (position > input.Length - 2)
            return EmailOpenPgpMpiRead.Failure(initialPosition);
        int bits = (input[position] << 8) | input[position + 1];
        position += 2;
        int bytes = (bits + 7) / 8;
        if (bytes > input.Length - position)
            return EmailOpenPgpMpiRead.Failure(initialPosition);
        byte[] value = Slice(input, position, bytes);
        position += bytes;
        return EmailOpenPgpMpiRead.Success(position, value);
    }

    static byte[] Fingerprint(byte[] body, int publicBodyLength, int version)
    {
        int prefixLength = version == 4 ? 3 : 5;
        byte[] material = new byte[prefixLength + publicBodyLength];
        material[0] = version == 4 ? (byte)0x99 : (byte)0x9B;
        WriteUnsigned(material, 1, publicBodyLength, prefixLength - 1);
        new ReadOnlySpan<byte>(body, 0, publicBodyLength).CopyTo(new Span<byte>(material, prefixLength, publicBodyLength));
        return version == 4 ? SHA1.HashData(material) : SHA256.HashData(material);
    }

    static EmailOpenPgpScalarRead ReadScalar(byte[] input, int position, int bytes)
    {
        if (position > input.Length - bytes)
            return EmailOpenPgpScalarRead.Failure(position);
        ulong value = ReadUnsigned(input, position, bytes);
        position += bytes;
        return EmailOpenPgpScalarRead.Success(position, value);
    }

    static ulong ReadUnsigned(byte[] input, int offset, int count)
    {
        ulong value = 0;
        int index = 0;
        while (index < count)
        {
            value = (value << 8) | input[offset + index];
            index++;
        }

        return value;
    }

    static void WriteUnsigned(byte[] output, int offset, long value, int count)
    {
        int index = count - 1;
        while (index >= 0)
        {
            output[offset + index] = (byte)(value & 255);
            value >>= 8;
            index--;
        }
    }

    static long ToLimitValue(ulong value)
    {
        return value > long.MaxValue ? long.MaxValue : (long)value;
    }

    static bool AllowsPartialLengths(int typeIdentifier)
    {
        return typeIdentifier == 8 || typeIdentifier == 9 || typeIdentifier == 11 || typeIdentifier == 18 || typeIdentifier == 20;
    }

    static EmailOpenPgpPacketKind Kind(int value)
    {
        return value >= 1 && value <= 14 || value >= 17 && value <= 21 ? (EmailOpenPgpPacketKind)value : EmailOpenPgpPacketKind.Unknown;
    }

    static bool LooksArmored(byte[] input)
    {
        return input.Length >= 14 && input.AsSpan(0, 14).SequenceEqual("-----BEGIN PGP"u8);
    }

    static byte[] LastBytes(byte[] value, int count)
    {
        if (value.Length >= count)
            return Slice(value, value.Length - count, count);
        byte[] result = new byte[count];
        value.CopyTo(result, count - value.Length);
        return result;
    }

    static byte[] Slice(byte[] value, int offset, int length)
    {
        byte[] copy = new byte[length];
        new ReadOnlySpan<byte>(value, offset, length).CopyTo(copy);
        return copy;
    }

    static EmailIssue Issue(EmailIssueSeverity severity, EmailIssueCode code, string? description, long offset, string recovery)
    {
        return new EmailIssueOperations().CreateWithSeverityAndCodeAndDescriptionAndSourcePositionAndEntityPathAndFieldNameAndRecoveryDescription(severity, code, description, new EmailSourcePosition(offset, 1, 1), null, null, recovery);
    }
}

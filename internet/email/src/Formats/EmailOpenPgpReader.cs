using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;

namespace email;
internal class EmailOpenPgpFrame
{
    int typeIdentifier;
    byte[] body;
    byte[] encodedBytes;
    bool legacyHeader;
    bool partialLengths;

    internal EmailOpenPgpFrame(int typeIdentifier, byte[] body, byte[] encodedBytes, bool legacyHeader, bool partialLengths)
    {
        this.typeIdentifier = typeIdentifier;
        this.body = body;
        this.encodedBytes = encodedBytes;
        this.legacyHeader = legacyHeader;
        this.partialLengths = partialLengths;
    }

    internal int GetTypeIdentifier() { return typeIdentifier; }
    internal byte[] GetBody() { return body; }
    internal byte[] GetEncodedBytes() { return encodedBytes; }
    internal bool HasLegacyHeader() { return legacyHeader; }
    internal bool UsesPartialLengths() { return partialLengths; }
}
internal class EmailOpenPgpFrameRead
{
    EmailOpenPgpFrame? frame;
    string? error;

    EmailOpenPgpFrameRead(EmailOpenPgpFrame? frame, string? error)
    {
        this.frame = frame;
        this.error = error;
    }

    internal static EmailOpenPgpFrameRead Success(EmailOpenPgpFrame frame) { return new EmailOpenPgpFrameRead(frame, null); }
    internal static EmailOpenPgpFrameRead Failure(string error) { return new EmailOpenPgpFrameRead(null, error); }
    internal bool IsSuccess() { return frame != null; }
    internal EmailOpenPgpFrame? GetFrame() { return frame; }
    internal string? GetError() { return error; }
}
internal class EmailOpenPgpLengthRead
{
    bool success;
    int position;
    ulong length;
    bool partial;

    EmailOpenPgpLengthRead(bool success, int position, ulong length, bool partial)
    {
        this.success = success;
        this.position = position;
        this.length = length;
        this.partial = partial;
    }

    internal static EmailOpenPgpLengthRead Success(int position, ulong length, bool partial) { return new EmailOpenPgpLengthRead(true, position, length, partial); }
    internal static EmailOpenPgpLengthRead Failure(int position) { return new EmailOpenPgpLengthRead(false, position, 0, false); }
    internal bool IsSuccess() { return success; }
    internal int GetPosition() { return position; }
    internal ulong GetLength() { return length; }
    internal bool IsPartial() { return partial; }
}
internal class EmailOpenPgpTypedRead
{
    bool success;
    object? value;

    EmailOpenPgpTypedRead(bool success, object? value)
    {
        this.success = success;
        this.value = value;
    }

    internal static EmailOpenPgpTypedRead Success(object? value) { return new EmailOpenPgpTypedRead(true, value); }
    internal static EmailOpenPgpTypedRead Failure() { return new EmailOpenPgpTypedRead(false, null); }
    internal bool IsSuccess() { return success; }
    internal object? GetValue() { return value; }
}
internal class EmailOpenPgpSubpacketRead
{
    bool success;
    List<EmailOpenPgpSignatureSubpacket>? subpackets;

    EmailOpenPgpSubpacketRead(bool success, List<EmailOpenPgpSignatureSubpacket>? subpackets)
    {
        this.success = success;
        this.subpackets = subpackets;
    }

    internal static EmailOpenPgpSubpacketRead Success(List<EmailOpenPgpSignatureSubpacket> subpackets) { return new EmailOpenPgpSubpacketRead(true, subpackets); }
    internal static EmailOpenPgpSubpacketRead Failure() { return new EmailOpenPgpSubpacketRead(false, null); }
    internal bool IsSuccess() { return success; }
    internal List<EmailOpenPgpSignatureSubpacket>? GetSubpackets() { return subpackets; }
}
internal class EmailOpenPgpScalarRead
{
    bool success;
    int position;
    ulong value;

    EmailOpenPgpScalarRead(bool success, int position, ulong value)
    {
        this.success = success;
        this.position = position;
        this.value = value;
    }

    internal static EmailOpenPgpScalarRead Success(int position, ulong value) { return new EmailOpenPgpScalarRead(true, position, value); }
    internal static EmailOpenPgpScalarRead Failure(int position) { return new EmailOpenPgpScalarRead(false, position, 0); }
    internal bool IsSuccess() { return success; }
    internal int GetPosition() { return position; }
    internal ulong GetValue() { return value; }
}

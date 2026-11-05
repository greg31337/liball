using System;
using System.Buffers.Binary;
using System.Collections.Generic;

namespace avif;

internal class IsoBoxWriteFrame
{
    internal int Offset;
    internal int HeaderSize;
    internal bool UsesLargeSize;
    internal bool ExtendsToEnd;
    internal bool ContentSealed;

    internal IsoBoxWriteFrame(
        int offset,
        int headerSize,
        bool usesLargeSize,
        bool extendsToEnd)
    {
        Offset = offset;
        HeaderSize = headerSize;
        UsesLargeSize = usesLargeSize;
        ExtendsToEnd = extendsToEnd;
        ContentSealed = false;
    }
}

internal class IsoBoxWriteResult
{
    internal AvifResultCode Code;
    internal byte[] Data;

    internal IsoBoxWriteResult(AvifResultCode code, byte[] data)
    {
        Code = code;
        Data = data;
    }
}

internal class IsoBoxWriter
{
    internal MemoryOutput Output;
    internal List<IsoBoxWriteFrame> Frames;
    internal bool RootSealed;
    internal bool Closed;
    internal AvifResultCode Code;

    internal IsoBoxWriter(int initialCapacity, long maxBytes)
    {
        Output = MemoryOutput.Create(initialCapacity, maxBytes);
        Frames = new List<IsoBoxWriteFrame>();
        RootSealed = false;
        Closed = false;
        Code = Output.Code;
    }

    internal AvifResultCode BeginBox(string type)
    {
        return Begin(type, System.Array.Empty<byte>(), false, false);
    }

    internal AvifResultCode BeginLargeBox(string type)
    {
        return Begin(type, System.Array.Empty<byte>(), true, false);
    }

    internal AvifResultCode BeginToEndBox(string type)
    {
        return Begin(type, System.Array.Empty<byte>(), false, true);
    }

    internal AvifResultCode BeginUuidBox(ReadOnlySpan<byte> userType)
    {
        return Begin("uuid", userType, false, false);
    }

    internal AvifResultCode BeginLargeUuidBox(ReadOnlySpan<byte> userType)
    {
        return Begin("uuid", userType, true, false);
    }

    internal AvifResultCode BeginToEndUuidBox(ReadOnlySpan<byte> userType)
    {
        return Begin("uuid", userType, false, true);
    }

    internal AvifResultCode BeginFullBox(string type, int version, int flags)
    {
        return BeginFull(type, version, flags, false);
    }

    internal AvifResultCode BeginLargeFullBox(string type, int version, int flags)
    {
        return BeginFull(type, version, flags, true);
    }

    internal AvifResultCode BeginFull(
        string type,
        int version,
        int flags,
        bool largeSize)
    {
        if (version < 0 || version > byte.MaxValue || flags < 0 || flags > 16777215)
        {
            return SetError(AvifResultCode.InvalidArgument);
        }

        if (!IsoBoxType.IsValid(type) || type == "uuid")
        {
            return SetError(AvifResultCode.InvalidArgument);
        }

        AvifResultCode state = CheckCanAppend();
        if (state != AvifResultCode.Success)
        {
            return state;
        }

        int required = largeSize ? 20 : 12;
        if (!Output.EnsureCapacity(required))
        {
            Code = Output.Code;
            return Code;
        }

        AvifResultCode beginCode = Begin(
            type,
            System.Array.Empty<byte>(),
            largeSize,
            false);
        if (beginCode != AvifResultCode.Success)
        {
            return beginCode;
        }

        byte[] fields = new byte[4];
        fields[0] = IntegerConversion.ToByte(version);
        fields[1] = IntegerConversion.ToByte(flags >> 16);
        fields[2] = IntegerConversion.ToByte(flags >> 8);
        fields[3] = IntegerConversion.ToByte(flags);
        return Write(fields);
    }

    internal AvifResultCode Write(ReadOnlySpan<byte> source)
    {
        AvifResultCode state = CheckCanAppend();
        if (state != AvifResultCode.Success)
        {
            return state;
        }

        AvifResultCode writeCode = Output.Write(source);
        if (writeCode != AvifResultCode.Success)
        {
            Code = writeCode;
        }

        return Code;
    }

    internal AvifResultCode WritePreservedBox(IsoBox box)
    {
        AvifResultCode state = CheckCanAppend();
        if (state != AvifResultCode.Success)
        {
            return state;
        }

        if (box == null || box.RawData.Length == 0 || box.RawData.Length != box.Size)
        {
            return SetError(AvifResultCode.InvalidArgument);
        }

        AvifResultCode writeCode = Output.Write(box.RawData);
        if (writeCode != AvifResultCode.Success)
        {
            Code = writeCode;
            return Code;
        }

        if (box.ExtendsToEnd)
        {
            SealCurrentContent();
        }

        return Code;
    }

    internal AvifResultCode EndBox()
    {
        if (Closed)
        {
            return AvifResultCode.Closed;
        }

        if (Code != AvifResultCode.Success)
        {
            return Code;
        }

        if (Frames.Count == 0)
        {
            return SetError(AvifResultCode.InvalidState);
        }

        int frameIndex = Frames.Count - 1;
        IsoBoxWriteFrame frame = Frames[frameIndex];
        long size = (long)Output.Length - frame.Offset;
        if (size < frame.HeaderSize)
        {
            return SetError(AvifResultCode.InvalidState);
        }

        if (!frame.ExtendsToEnd)
        {
            if (frame.UsesLargeSize)
            {
                BinaryPrimitives.WriteUInt64BigEndian(
                    Output.Buffer.AsSpan(frame.Offset + 8, 8),
                    (ulong)size);
            }
            else
            {
                if (size > uint.MaxValue)
                {
                    return SetError(AvifResultCode.LimitExceeded);
                }

                BinaryPrimitives.WriteUInt32BigEndian(
                    Output.Buffer.AsSpan(frame.Offset, 4),
                    (uint)size);
            }
        }

        Frames.RemoveAt(frameIndex);
        if (frame.ExtendsToEnd)
        {
            SealCurrentContent();
        }

        return Code;
    }

    internal IsoBoxWriteResult Finish()
    {
        if (Closed)
        {
            return new IsoBoxWriteResult(AvifResultCode.Closed, System.Array.Empty<byte>());
        }

        if (Code != AvifResultCode.Success)
        {
            return new IsoBoxWriteResult(Code, System.Array.Empty<byte>());
        }

        if (Frames.Count != 0)
        {
            return new IsoBoxWriteResult(AvifResultCode.InvalidState, System.Array.Empty<byte>());
        }

        return new IsoBoxWriteResult(AvifResultCode.Success, Output.ToArray());
    }

    internal AvifResultCode Close()
    {
        if (!Closed)
        {
            Closed = true;
            Frames.Clear();
            Output.Close();
        }

        return AvifResultCode.Success;
    }

    internal AvifResultCode Begin(
        string type,
        ReadOnlySpan<byte> userType,
        bool usesLargeSize,
        bool extendsToEnd)
    {
        AvifResultCode state = CheckCanAppend();
        if (state != AvifResultCode.Success)
        {
            return state;
        }

        if (!IsoBoxType.IsValid(type))
        {
            return SetError(AvifResultCode.InvalidArgument);
        }

        bool isUuid = type == "uuid";
        if ((isUuid && userType.Length != 16) || (!isUuid && userType.Length != 0))
        {
            return SetError(AvifResultCode.InvalidArgument);
        }

        if (usesLargeSize && extendsToEnd)
        {
            return SetError(AvifResultCode.InvalidArgument);
        }

        int headerSize = usesLargeSize ? 16 : 8;
        if (isUuid)
        {
            headerSize += 16;
        }

        byte[] header = new byte[headerSize];
        ByteWriter writer = new ByteWriter(header.Length);
        if (usesLargeSize)
        {
            writer.WriteUInt32BigEndian(header, 1);
        }
        else
        {
            writer.WriteUInt32BigEndian(header, 0);
        }

        writer.WriteFourCc(header, type);
        if (usesLargeSize)
        {
            writer.WriteUInt64BigEndian(header, 0);
        }

        if (isUuid)
        {
            writer.WriteUuid(header, userType);
        }

        if (writer.Code != AvifResultCode.Success)
        {
            return SetError(writer.Code);
        }

        int offset = Output.Length;
        AvifResultCode writeCode = Output.Write(header);
        if (writeCode != AvifResultCode.Success)
        {
            Code = writeCode;
            return Code;
        }

        Frames.Add(new IsoBoxWriteFrame(offset, headerSize, usesLargeSize, extendsToEnd));
        return Code;
    }

    internal AvifResultCode CheckCanAppend()
    {
        if (Closed)
        {
            return AvifResultCode.Closed;
        }

        if (Code != AvifResultCode.Success)
        {
            return Code;
        }

        if (RootSealed)
        {
            return SetError(AvifResultCode.InvalidState);
        }

        if (Frames.Count != 0 && Frames[Frames.Count - 1].ContentSealed)
        {
            return SetError(AvifResultCode.InvalidState);
        }

        return AvifResultCode.Success;
    }

    internal void SealCurrentContent()
    {
        if (Frames.Count == 0)
        {
            RootSealed = true;
        }
        else
        {
            Frames[Frames.Count - 1].ContentSealed = true;
        }
    }

    internal AvifResultCode SetError(AvifResultCode code)
    {
        if (Code == AvifResultCode.Success)
        {
            Code = code;
        }

        return Code;
    }
}

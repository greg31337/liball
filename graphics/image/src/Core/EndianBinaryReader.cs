using System.Collections.Generic;

namespace image;

internal class EndianBinaryReader
{
    internal BoundedStreamReader? Reader;
    internal bool LittleEndian;
    internal bool LastReadSucceeded;
    internal List<MetadataDiagnostic> Diagnostics;

    internal EndianBinaryReader(BoundedStreamReader? reader, bool littleEndian)
    {
        Reader = reader;
        LittleEndian = littleEndian;
        LastReadSucceeded = true;

        if (Reader != null)
        {
            Diagnostics = Reader.Diagnostics;
        }
        else
        {
            Diagnostics = new List<MetadataDiagnostic>();
        }
    }

    internal static EndianBinaryReader CreateLittleEndian(BoundedStreamReader? reader)
    {
        return new EndianBinaryReader(reader, true);
    }

    internal static EndianBinaryReader CreateBigEndian(BoundedStreamReader? reader)
    {
        return new EndianBinaryReader(reader, false);
    }

    internal void SetLittleEndian()
    {
        LittleEndian = true;
    }

    internal void SetBigEndian()
    {
        LittleEndian = false;
    }

    internal byte ReadUnsigned8()
    {
        byte[] bytes = ReadExact(1, "endian.u8");
        return bytes[0];
    }

    internal sbyte ReadSigned8()
    {
        byte[] bytes = ReadExact(1, "endian.i8");
        return IntegerBitPattern.ToSigned8(bytes[0]);
    }

    internal ushort ReadUnsigned16()
    {
        byte[] bytes = ReadExact(2, "endian.u16");
        uint value = 0;

        if (LittleEndian)
        {
            value = (uint)bytes[0] | ((uint)bytes[1] << 8);
        }
        else
        {
            value = ((uint)bytes[0] << 8) | (uint)bytes[1];
        }

        return (ushort)value;
    }

    internal short ReadSigned16()
    {
        ushort value = ReadUnsigned16();
        return IntegerBitPattern.ToSigned16(value);
    }

    internal uint ReadUnsigned32()
    {
        byte[] bytes = ReadExact(4, "endian.u32");
        uint value = 0;

        if (LittleEndian)
        {
            value = (uint)bytes[0]
                | ((uint)bytes[1] << 8)
                | ((uint)bytes[2] << 16)
                | ((uint)bytes[3] << 24);
        }
        else
        {
            value = ((uint)bytes[0] << 24)
                | ((uint)bytes[1] << 16)
                | ((uint)bytes[2] << 8)
                | (uint)bytes[3];
        }

        return value;
    }

    internal int ReadSigned32()
    {
        uint value = ReadUnsigned32();
        return IntegerBitPattern.ToSigned32(value);
    }

    internal ulong ReadUnsigned64()
    {
        byte[] bytes = ReadExact(8, "endian.u64");
        ulong value = 0;

        if (LittleEndian)
        {
            value = (ulong)bytes[0]
                | ((ulong)bytes[1] << 8)
                | ((ulong)bytes[2] << 16)
                | ((ulong)bytes[3] << 24)
                | ((ulong)bytes[4] << 32)
                | ((ulong)bytes[5] << 40)
                | ((ulong)bytes[6] << 48)
                | ((ulong)bytes[7] << 56);
        }
        else
        {
            value = ((ulong)bytes[0] << 56)
                | ((ulong)bytes[1] << 48)
                | ((ulong)bytes[2] << 40)
                | ((ulong)bytes[3] << 32)
                | ((ulong)bytes[4] << 24)
                | ((ulong)bytes[5] << 16)
                | ((ulong)bytes[6] << 8)
                | (ulong)bytes[7];
        }

        return value;
    }

    internal long ReadSigned64()
    {
        ulong value = ReadUnsigned64();
        return IntegerBitPattern.ToSigned64(value);
    }

    internal MetadataValue ReadUnsignedRational32()
    {
        uint numerator = ReadUnsigned32();
        bool numeratorSucceeded = LastReadSucceeded;
        uint denominator = ReadUnsigned32();

        LastReadSucceeded = numeratorSucceeded && LastReadSucceeded;
        return MetadataValue.FromRational(numerator, denominator);
    }

    internal MetadataValue ReadSignedRational32()
    {
        int numerator = ReadSigned32();
        bool numeratorSucceeded = LastReadSucceeded;
        int denominator = ReadSigned32();

        LastReadSucceeded = numeratorSucceeded && LastReadSucceeded;
        return MetadataValue.FromSignedRational(numerator, denominator);
    }

    internal double ReadUnsignedFixed16Dot16()
    {
        uint value = ReadUnsigned32();
        return (double)value / 65536.0;
    }

    internal double ReadSignedFixed16Dot16()
    {
        int value = ReadSigned32();
        return (double)value / 65536.0;
    }

    internal double ReadUnsignedFixed8Dot8()
    {
        ushort value = ReadUnsigned16();
        return (double)value / 256.0;
    }

    internal double ReadSignedFixed8Dot8()
    {
        short value = ReadSigned16();
        return (double)value / 256.0;
    }

    internal byte[] ReadFixedBytes(int count)
    {
        return ReadExact(count, "endian.fixed.bytes");
    }

    internal byte[] ReadExact(int count, string operation)
    {
        LastReadSucceeded = false;

        if (count < 0)
        {
            MetadataDiagnostic.AddRecoverableError(Diagnostics, operation + ".negative", "Primitive read count was negative.");
            return new byte[0];
        }

        byte[] result = new byte[count];

        if (count == 0)
        {
            LastReadSucceeded = true;
            return result;
        }

        if (Reader == null)
        {
            MetadataDiagnostic.AddRecoverableError(Diagnostics, operation + ".reader.null", "Bounded stream reader was null.");
            return result;
        }

        byte[] bytes = Reader.ReadBytes(count);
        int index = 0;

        while (index < bytes.Length && index < count)
        {
            result[index] = bytes[index];
            index = index + 1;
        }

        LastReadSucceeded = bytes.Length == count;
        return result;
    }
}

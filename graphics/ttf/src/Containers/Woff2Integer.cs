namespace ttf;

internal static class Woff2Integer
{
    internal static uint ReadUIntBase128(BigEndianReader reader)
    {
        if (reader == null)
        {
            throw new System.ArgumentNullException("reader");
        }

        uint value = 0;
        int index = 0;

        while (index < 5)
        {
            byte encodedByte = reader.ReadUInt8();

            if (index == 0 && encodedByte == 0x80)
            {
                throw FontFormatException.Create(
                    "A WOFF 2 UIntBase128 value has a prohibited leading zero.");
            }

            if ((value & 0xFE000000U) != 0)
            {
                throw FontFormatException.Create(
                    "A WOFF 2 UIntBase128 value exceeds the UInt32 range.");
            }

            value = (value << 7) | ((uint)encodedByte & 0x7FU);

            if ((encodedByte & 0x80) == 0)
            {
                return value;
            }

            index += 1;
        }

        throw FontFormatException.Create(
            "A WOFF 2 UIntBase128 value exceeds the five-byte encoding limit.");
    }

    internal static int Read255UInt16(BigEndianReader reader)
    {
        if (reader == null)
        {
            throw new System.ArgumentNullException("reader");
        }

        int code = reader.ReadUInt8();

        if (code == 253)
        {
            int highByte = reader.ReadUInt8();
            int lowByte = reader.ReadUInt8();
            return (highByte << 8) | lowByte;
        }

        if (code == 254)
        {
            return reader.ReadUInt8() + 506;
        }

        if (code == 255)
        {
            return reader.ReadUInt8() + 253;
        }

        return code;
    }
}

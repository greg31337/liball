namespace image;

internal class JpegXlBitReader
{
    internal byte[] Data;
    internal int BitPosition;
    internal bool Valid;

    internal JpegXlBitReader(byte[]? data, int bitPosition)
    {
        Data = data == null ? new byte[0] : data;
        BitPosition = bitPosition;
        Valid = bitPosition >= 0 && bitPosition <= Data.Length * 8;
    }

    internal uint ReadBits(int count)
    {
        if (!Valid || count < 0 || count > 32 || count > Data.Length * 8 - BitPosition)
        {
            Valid = false;
            return 0;
        }

        uint value = 0;
        int index = 0;
        while (index < count)
        {
            int source = BitPosition + index;
            int bit = (Data[source / 8] >> (source % 8)) & 1;
            value = value | ((uint)bit << index);
            index = index + 1;
        }

        BitPosition = BitPosition + count;
        return value;
    }
}

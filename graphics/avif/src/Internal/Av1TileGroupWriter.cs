using System;

namespace avif;

// AV1 specification sections 5.11.1 and 6.10.1.
internal static class Av1TileGroupWriter
{
    internal static Av1WriteResult Write(
        Av1TileInfo info,
        int startTile,
        byte[][] tiles)
    {
        if (info == null || tiles == null || tiles.Length == 0
            || !HasValidTileInfo(info))
        {
            return Fail(AvifResultCode.InvalidArgument);
        }

        int tileCount = info.TileColumns * info.TileRows;
        int endTile = startTile + tiles.Length - 1;
        if (startTile < 0 || endTile < startTile || endTile >= tileCount)
        {
            return Fail(AvifResultCode.InvalidArgument);
        }

        bool rangePresent = startTile != 0 || endTile != tileCount - 1;
        int headerBits = tileCount > 1 ? 1 : 0;
        if (rangePresent)
        {
            headerBits += 2 * (info.TileColumnsLog2 + info.TileRowsLog2);
        }

        long dataLength = (headerBits + 7) / 8;
        int index = 0;
        while (index < tiles.Length)
        {
            if (tiles[index] == null || tiles[index].Length == 0)
            {
                return Fail(AvifResultCode.InvalidArgument);
            }

            dataLength += tiles[index].Length;
            if (index < tiles.Length - 1)
            {
                if (!FitsTileSize(tiles[index].Length, info.TileSizeBytes))
                {
                    return Fail(AvifResultCode.InvalidArgument);
                }

                dataLength += info.TileSizeBytes;
            }

            if (dataLength > int.MaxValue)
            {
                return Fail(AvifResultCode.LimitExceeded);
            }

            index++;
        }

        byte[] payload = new byte[(int)dataLength];
        BitWriter writer = BitWriter.Create(payload.Length);
        if (tileCount > 1)
        {
            writer.WriteBit(payload, rangePresent ? 1 : 0);
        }

        if (rangePresent)
        {
            int tileNumberBits = info.TileColumnsLog2 + info.TileRowsLog2;
            writer.WriteBits(payload, (ulong)startTile, tileNumberBits);
            writer.WriteBits(payload, (ulong)endTile, tileNumberBits);
        }

        writer.AlignToByte(payload, 0);
        if (writer.Code != AvifResultCode.Success)
        {
            return Fail(writer.Code);
        }

        int cursor = (int)(writer.BitPosition / 8);
        index = 0;
        while (index < tiles.Length)
        {
            if (index < tiles.Length - 1)
            {
                ulong encodedSize = (ulong)tiles[index].Length - 1;
                int sizeByte = 0;
                while (sizeByte < info.TileSizeBytes)
                {
                    payload[cursor] = IntegerConversion.ToByte(
                        encodedSize >> (sizeByte * 8));
                    cursor++;
                    sizeByte++;
                }
            }

            tiles[index].CopyTo(payload, cursor);
            cursor += tiles[index].Length;
            index++;
        }

        if (cursor == payload.Length)
        {
            return new Av1WriteResult(AvifResultCode.Success, payload);
        }

        byte[] compact = new byte[cursor];
        payload.AsSpan(0, cursor).CopyTo(compact);
        return new Av1WriteResult(AvifResultCode.Success, compact);
    }

    static bool FitsTileSize(int size, int sizeBytes)
    {
        if (size <= 0 || sizeBytes < 1 || sizeBytes > 4)
        {
            return false;
        }

        ulong maximum = sizeBytes == 4
            ? uint.MaxValue
            : (1UL << (sizeBytes * 8)) - 1;
        return (ulong)(size - 1) <= maximum;
    }

    static bool HasValidTileInfo(Av1TileInfo info)
    {
        if (info.TileColumns < 1
            || info.TileColumns > Av1SyntaxConstants.MaxTileColumns
            || info.TileRows < 1
            || info.TileRows > Av1SyntaxConstants.MaxTileRows
            || info.TileColumnsLog2 < 0 || info.TileColumnsLog2 > 6
            || info.TileRowsLog2 < 0 || info.TileRowsLog2 > 6
            || !HasMatchingLog2(info.TileColumns, info.TileColumnsLog2)
            || !HasMatchingLog2(info.TileRows, info.TileRowsLog2))
        {
            return false;
        }

        int count = info.TileColumns * info.TileRows;
        if (count == 1)
        {
            return info.TileSizeBytes == 0;
        }

        return info.TileSizeBytes >= 1 && info.TileSizeBytes <= 4
            && info.TileColumnsLog2 + info.TileRowsLog2 > 0;
    }

    static bool HasMatchingLog2(int count, int countLog2)
    {
        int upper = 1 << countLog2;
        int lower = countLog2 == 0 ? 0 : 1 << (countLog2 - 1);
        return count > lower && count <= upper;
    }

    static Av1WriteResult Fail(AvifResultCode code)
    {
        return new Av1WriteResult(code, System.Array.Empty<byte>());
    }
}

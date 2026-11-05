using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace pdf;

internal class PdfPngParseState
{
    internal int Width;
    internal int Height;
    internal int BitDepth;
    internal int ColorTypeValue;
    internal int ChannelCount;
    internal bool IsInterlaced;
    internal bool HasHeader;
    internal bool HasPalette;
    internal bool HasTransparency;
    internal bool HasPhysicalDimensions;
    internal bool HasPhysicalChunk;
    internal bool HasImageData;
    internal bool ImageDataEnded;
    internal bool HasEnd;
    internal byte[] Palette;
    internal byte[] Transparency;
    internal int TransparentGray;
    internal int TransparentRed;
    internal int TransparentGreen;
    internal int TransparentBlue;
    internal double HorizontalDpi;
    internal double VerticalDpi;
    internal List<byte> CompressedData;

    internal PdfPngParseState()
    {
        Palette = new byte[0];
        Transparency = new byte[0];
        TransparentGray = -1;
        TransparentRed = -1;
        TransparentGreen = -1;
        TransparentBlue = -1;
        CompressedData = new List<byte>();
    }
}

internal static class PdfPngParser
{
    internal static readonly byte[] Signature = new byte[] { 137, 80, 78, 71, 13, 10, 26, 10 };

    internal static PdfPngData Parse(byte[] data)
    {
        ValidateSignature(data);
        PdfPngParseState state = new PdfPngParseState();
        int offset = Signature.Length;
        while (offset < data.Length)
        {
            if (offset + 12 > data.Length)
            {
                throw new InvalidDataException("PNG chunk header is truncated.");
            }

            long chunkLengthValue = ReadUInt32(data, offset);
            if (chunkLengthValue > int.MaxValue)
            {
                throw new InvalidDataException("PNG chunk length exceeds the supported range.");
            }

            int chunkLength = (int)chunkLengthValue;
            int typeOffset = offset + 4;
            ValidateChunkType(data, typeOffset);
            long chunkEndValue = (long)offset + 12 + chunkLength;
            if (chunkEndValue > data.Length)
            {
                throw new InvalidDataException("PNG chunk extends beyond the available data.");
            }

            int chunkDataOffset = typeOffset + 4;
            int crcOffset = chunkDataOffset + chunkLength;
            uint expectedCrc = (uint)ReadUInt32(data, crcOffset);
            uint actualCrc = CalculateCrc(data, typeOffset, chunkLength + 4);
            if (actualCrc != expectedCrc)
            {
                throw new InvalidDataException("PNG chunk CRC does not match its contents.");
            }

            string chunkType = Encoding.ASCII.GetString(data, typeOffset, 4);
            if (!state.HasHeader && chunkType != "IHDR")
            {
                throw new InvalidDataException("PNG data must begin with an IHDR chunk.");
            }

            if (state.HasImageData && chunkType != "IDAT")
            {
                state.ImageDataEnded = true;
            }

            if (chunkType == "IHDR")
            {
                ParseHeader(data, chunkDataOffset, chunkLength, state);
            }
            else if (chunkType == "PLTE")
            {
                ParsePalette(data, chunkDataOffset, chunkLength, state);
            }
            else if (chunkType == "tRNS")
            {
                ParseTransparency(data, chunkDataOffset, chunkLength, state);
            }
            else if (chunkType == "pHYs")
            {
                ParsePhysicalDimensions(data, chunkDataOffset, chunkLength, state);
            }
            else if (chunkType == "IDAT")
            {
                ParseImageData(data, chunkDataOffset, chunkLength, state);
            }
            else if (chunkType == "IEND")
            {
                ParseEnd(chunkLength, state);
            }
            else if (chunkType == "acTL" || chunkType == "fcTL" || chunkType == "fdAT")
            {
                throw new InvalidDataException("Animated PNG data is not supported.");
            }
            else if ((data[typeOffset] & 32) == 0)
            {
                throw new InvalidDataException("PNG data contains an unsupported critical chunk.");
            }

            offset = (int)chunkEndValue;
            if (state.HasEnd)
            {
                if (offset != data.Length)
                {
                    throw new InvalidDataException("PNG data contains bytes after the IEND chunk.");
                }

                break;
            }
        }

        ValidateRequiredChunks(state);
        return PdfPngDecoder.Decode(state, data);
    }

    internal static void ValidateSignature(byte[] data)
    {
        if (data.Length < Signature.Length)
        {
            throw new InvalidDataException("PNG signature is truncated.");
        }

        for (int index = 0; index < Signature.Length; index++)
        {
            if (data[index] != Signature[index])
            {
                throw new InvalidDataException("PNG signature is invalid.");
            }
        }
    }

    internal static void ValidateChunkType(byte[] data, int offset)
    {
        for (int index = 0; index < 4; index++)
        {
            int value = data[offset + index];
            bool upper = value >= 65 && value <= 90;
            bool lower = value >= 97 && value <= 122;
            if (!upper && !lower)
            {
                throw new InvalidDataException("PNG chunk type contains an invalid character.");
            }
        }

        if (data[offset + 2] >= 97 && data[offset + 2] <= 122)
        {
            throw new InvalidDataException("PNG chunk type uses an invalid reserved bit.");
        }
    }

    internal static void ParseHeader(byte[] data, int offset, int length, PdfPngParseState state)
    {
        if (state.HasHeader)
        {
            throw new InvalidDataException("PNG data contains more than one IHDR chunk.");
        }

        if (length != 13)
        {
            throw new InvalidDataException("PNG IHDR chunk must contain 13 bytes.");
        }

        long widthValue = ReadUInt32(data, offset);
        long heightValue = ReadUInt32(data, offset + 4);
        if (widthValue <= 0 || heightValue <= 0 || widthValue > int.MaxValue || heightValue > int.MaxValue)
        {
            throw new InvalidDataException("PNG dimensions must be positive supported integers.");
        }

        int bitDepth = data[offset + 8];
        int colorType = data[offset + 9];
        ValidateColorFormat(colorType, bitDepth);
        if (data[offset + 10] != 0 || data[offset + 11] != 0)
        {
            throw new InvalidDataException("PNG compression and filter methods must use the baseline values.");
        }

        int interlace = data[offset + 12];
        if (interlace != 0 && interlace != 1)
        {
            throw new InvalidDataException("PNG interlace method is invalid.");
        }

        int channels = GetChannelCount(colorType);
        long pixelCount = widthValue * heightValue;
        if (pixelCount > int.MaxValue / channels)
        {
            throw new InvalidDataException("PNG dimensions require more decoded samples than supported.");
        }

        state.Width = (int)widthValue;
        state.Height = (int)heightValue;
        state.BitDepth = bitDepth;
        state.ColorTypeValue = colorType;
        state.ChannelCount = channels;
        state.IsInterlaced = interlace == 1;
        state.HasHeader = true;
        ValidateDecodedSizes(state);
    }

    internal static void ValidateColorFormat(int colorType, int bitDepth)
    {
        bool valid = false;
        if (colorType == 0)
        {
            valid = bitDepth == 1 || bitDepth == 2 || bitDepth == 4 || bitDepth == 8 || bitDepth == 16;
        }
        else if (colorType == 2)
        {
            valid = bitDepth == 8 || bitDepth == 16;
        }
        else if (colorType == 3)
        {
            valid = bitDepth == 1 || bitDepth == 2 || bitDepth == 4 || bitDepth == 8;
        }
        else if (colorType == 4 || colorType == 6)
        {
            valid = bitDepth == 8 || bitDepth == 16;
        }

        if (!valid)
        {
            throw new InvalidDataException("PNG color type and bit depth combination is invalid or unsupported.");
        }
    }

    internal static int GetChannelCount(int colorType)
    {
        if (colorType == 0 || colorType == 3)
        {
            return 1;
        }

        if (colorType == 2)
        {
            return 3;
        }

        if (colorType == 4)
        {
            return 2;
        }

        return 4;
    }

    internal static void ValidateDecodedSizes(PdfPngParseState state)
    {
        long mainRowBytes;
        if (state.ColorTypeValue == 3)
        {
            mainRowBytes = (long)state.Width * 3;
        }
        else
        {
            int outputChannels = state.ColorTypeValue == 2 || state.ColorTypeValue == 6 ? 3 : 1;
            mainRowBytes = ((long)state.Width * outputChannels * state.BitDepth + 7) / 8;
        }

        long alphaRowBytes = ((long)state.Width * state.BitDepth + 7) / 8;
        if (mainRowBytes * state.Height > int.MaxValue || alphaRowBytes * state.Height > int.MaxValue)
        {
            throw new InvalidDataException("PNG decoded image streams exceed the supported size.");
        }
    }

    internal static void ParsePalette(byte[] data, int offset, int length, PdfPngParseState state)
    {
        if (state.HasPalette || state.HasTransparency || state.HasImageData)
        {
            throw new InvalidDataException("PNG PLTE chunk is duplicated or out of order.");
        }

        if (state.ColorTypeValue == 0 || state.ColorTypeValue == 4)
        {
            throw new InvalidDataException("Grayscale PNG data cannot contain a PLTE chunk.");
        }

        if (length == 0 || length > 768 || length % 3 != 0)
        {
            throw new InvalidDataException("PNG palette length is invalid.");
        }

        int entryCount = length / 3;
        if (state.ColorTypeValue == 3 && entryCount > 1 << state.BitDepth)
        {
            throw new InvalidDataException("PNG palette has more entries than its bit depth permits.");
        }

        state.Palette = CopyRange(data, offset, length);
        state.HasPalette = true;
    }

    internal static void ParseTransparency(byte[] data, int offset, int length, PdfPngParseState state)
    {
        if (state.HasTransparency || state.HasImageData)
        {
            throw new InvalidDataException("PNG tRNS chunk is duplicated or out of order.");
        }

        if (state.ColorTypeValue == 4 || state.ColorTypeValue == 6)
        {
            throw new InvalidDataException("PNG data with an alpha channel cannot contain a tRNS chunk.");
        }

        if (state.ColorTypeValue == 0)
        {
            if (length != 2)
            {
                throw new InvalidDataException("Grayscale PNG transparency data must contain one sample.");
            }

            int sample = ReadUInt16(data, offset);
            int maximum = (1 << state.BitDepth) - 1;
            if (state.BitDepth < 16 && sample > maximum)
            {
                throw new InvalidDataException("PNG transparent grayscale sample exceeds the declared bit depth.");
            }

            state.TransparentGray = sample;
        }
        else if (state.ColorTypeValue == 2)
        {
            if (length != 6)
            {
                throw new InvalidDataException("Truecolor PNG transparency data must contain three samples.");
            }

            state.TransparentRed = ReadUInt16(data, offset);
            state.TransparentGreen = ReadUInt16(data, offset + 2);
            state.TransparentBlue = ReadUInt16(data, offset + 4);
            if (state.BitDepth == 8 && (state.TransparentRed > 255 || state.TransparentGreen > 255 || state.TransparentBlue > 255))
            {
                throw new InvalidDataException("PNG transparent truecolor samples exceed the declared bit depth.");
            }
        }
        else
        {
            if (!state.HasPalette)
            {
                throw new InvalidDataException("Indexed PNG transparency data must follow its palette.");
            }

            int entryCount = state.Palette.Length / 3;
            if (length == 0 || length > entryCount)
            {
                throw new InvalidDataException("Indexed PNG transparency data exceeds its palette.");
            }

            state.Transparency = CopyRange(data, offset, length);
        }

        state.HasTransparency = true;
    }

    internal static void ParsePhysicalDimensions(byte[] data, int offset, int length, PdfPngParseState state)
    {
        if (state.HasPhysicalChunk || state.HasImageData)
        {
            throw new InvalidDataException("PNG pHYs chunk is duplicated or out of order.");
        }

        if (length != 9)
        {
            throw new InvalidDataException("PNG pHYs chunk must contain nine bytes.");
        }

        long horizontal = ReadUInt32(data, offset);
        long vertical = ReadUInt32(data, offset + 4);
        int unit = data[offset + 8];
        if (unit != 0 && unit != 1)
        {
            throw new InvalidDataException("PNG physical-dimension unit is invalid.");
        }

        if (unit == 1 && horizontal > 0 && vertical > 0)
        {
            state.HasPhysicalDimensions = true;
            state.HorizontalDpi = horizontal * 0.0254;
            state.VerticalDpi = vertical * 0.0254;
        }

        state.HasPhysicalChunk = true;
    }

    internal static void ParseImageData(byte[] data, int offset, int length, PdfPngParseState state)
    {
        if (state.ImageDataEnded)
        {
            throw new InvalidDataException("PNG IDAT chunks must be consecutive.");
        }

        if (state.ColorTypeValue == 3 && !state.HasPalette)
        {
            throw new InvalidDataException("Indexed PNG data requires a palette before image data.");
        }

        for (int index = 0; index < length; index++)
        {
            state.CompressedData.Add(data[offset + index]);
        }

        state.HasImageData = true;
    }

    internal static void ParseEnd(int length, PdfPngParseState state)
    {
        if (state.HasEnd || length != 0)
        {
            throw new InvalidDataException("PNG IEND chunk is duplicated or has an invalid length.");
        }

        if (!state.HasImageData)
        {
            throw new InvalidDataException("PNG IEND chunk appears before image data.");
        }

        state.HasEnd = true;
    }

    internal static void ValidateRequiredChunks(PdfPngParseState state)
    {
        if (!state.HasHeader || !state.HasImageData || !state.HasEnd)
        {
            throw new InvalidDataException("PNG data is missing a required chunk.");
        }

        if (state.ColorTypeValue == 3 && !state.HasPalette)
        {
            throw new InvalidDataException("Indexed PNG data is missing its palette.");
        }

        if (state.CompressedData.Count == 0)
        {
            throw new InvalidDataException("PNG image data is empty.");
        }
    }

    internal static long ReadUInt32(byte[] data, int offset)
    {
        return (long)data[offset] * 16777216 + (long)data[offset + 1] * 65536 + (long)data[offset + 2] * 256 + data[offset + 3];
    }

    internal static int ReadUInt16(byte[] data, int offset)
    {
        return data[offset] * 256 + data[offset + 1];
    }

    internal static uint CalculateCrc(byte[] data, int offset, int length)
    {
        uint crc = 4294967295U;
        for (int index = 0; index < length; index++)
        {
            crc = crc ^ data[offset + index];
            for (int bit = 0; bit < 8; bit++)
            {
                if ((crc & 1U) != 0)
                {
                    crc = 3988292384U ^ crc >> 1;
                }
                else
                {
                    crc = crc >> 1;
                }
            }
        }

        return crc ^ 4294967295U;
    }

    internal static byte[] CopyRange(byte[] data, int offset, int length)
    {
        byte[] copied = new byte[length];
        Array.Copy(data, offset, copied, 0, length);
        return copied;
    }
}
